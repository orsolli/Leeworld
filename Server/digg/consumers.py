import json

from asgiref.sync import async_to_sync
from channels.generic.websocket import JsonWebsocketConsumer
from boolean_geometry import TIME

def authenticate(user, player_id):
    from . import models
    return user.is_active and user.is_authenticated and models.Player.objects.filter(user__id=user.pk, id=player_id).exists()

class BlockConsumer(JsonWebsocketConsumer):
    def connect(self):
        self.block_group_name = None
        from . import signals
        self.signals = signals
        self.player_id = self.scope["url_route"]["kwargs"]["player_id"]
        self.user = self.scope["user"]
        if not authenticate(self.user, self.player_id):
            self.user = None

        self.block_group_name = "blocks"
        self.duration = None

        async_to_sync(self.channel_layer.group_add)(
            self.block_group_name, self.channel_name
        )

        self.accept()

    def disconnect(self, close_code):
        async_to_sync(self.channel_layer.group_discard)(
            self.block_group_name, self.channel_name
        )
        if self.user is None:
            return

        self.duration = None
        self.signals.terraformer_signal.send(
            sender=None,
            action=self.signals.STOP,
            player_id=self.player_id,
        )

    def receive(self, bytes_data):
        if self.user is None:
            return self.close(3000)

        text_data_json = json.loads(bytes_data)
        action = text_data_json["action"]
        self.player_id=text_data_json['player']
        block = None
        if action == 'stop':
            self.duration = None
            self.signals.terraformer_signal.send(
                sender=None,
                action=self.signals.STOP,
                player_id=self.player_id,
            )
        elif action == 'digg':
            block = '_'.join([str(int(p)) for p in text_data_json['block'].split('_')])
            position = '_'.join([str(int(p, base=10)) for p in text_data_json['position'][::-1].split('_')])[::-1]
            results = self.signals.terraformer_signal.send(
                sender=None,
                action=self.signals.DIGG if self.duration is None else self.signals.PING,
                player_id=self.player_id,
                block_position=block.replace('_', ','),
                position=position.replace('_', ','),
            )
            for receiver, response in results:
                if receiver == self.signals.terraformer_signal_handler:
                    done, finishers, time = response
                    break
            if self.duration is None:
                self.duration = TIME if int(self.player_id) in finishers else max(0, TIME + (TIME - time))
                async_to_sync(self.channel_layer.group_send)(
                    "players", {"type": "cursor_position", "player": self.player_id, "block": block, "position": position}
                )
            if done:
                async_to_sync(self.channel_layer.group_send)(
                    self.block_group_name, {"type": "block_message", "finishers": finishers, "block": block}
                )
            elif int(self.player_id) in finishers:
                self.send(bytes_data=bytes(f'digg:{block}:{min(99, int(100 - 100 * max(0, TIME - time) / self.duration))}:fin', 'utf8'))

            elif int(self.player_id) not in finishers:
                self.send(bytes_data=bytes(f'digg:{block}:{min(99, int(100 - 100 * max(0, TIME + (TIME - time)) / self.duration))}:queue', 'utf8'))

        async_to_sync(self.channel_layer.group_send)(
            "players", {"type": "cursor_action", "player": self.player_id, "action": action, "block": block}
        )

    # Receive message from block group
    def block_message(self, event):
        finishers: list[int] = event["finishers"]
        block = event["block"]
        if int(self.player_id) in finishers:
            self.send(bytes_data=bytes(f'digg:{block}:{100}', 'utf8'))
        self.send(bytes_data=bytes(f'block:{block}', 'utf8'))

class PlayerConsumer(JsonWebsocketConsumer):
    def connect(self):
        self.players_group = None
        self.player_id = self.scope["url_route"]["kwargs"]["player_id"]
        self.user = self.scope["user"]
        if not authenticate(self.user, self.player_id):
            self.user = None

        self.players_group = "players"
        self.block = '0_0_0'
        self.throttle = 0

        async_to_sync(self.channel_layer.group_add)(
            self.players_group, self.channel_name
        )

        self.accept()

    def disconnect(self, close_code):
        async_to_sync(self.channel_layer.group_send)(
            self.players_group, {"type": "player_disconnect", "player": self.player_id}
        )
        async_to_sync(self.channel_layer.group_discard)(
            self.players_group, self.channel_name
        )

    def receive(self, bytes_data):
        text_data_json = json.loads(bytes_data)
        part = text_data_json['part']
        block = '_'.join([str(int(p)) for p in text_data_json['block'].split('_')])
        if part == 'player':
            self.block = block
        elif self.user is None:
            return self.close(3000) # Unauthenticated users can not point
        position = '_'.join([str(int(p, base=10)) for p in text_data_json['position'][::-1].split('_')])[::-1]
        async_to_sync(self.channel_layer.group_send)(
            self.players_group, {"type": f"{part}_position", "player": self.player_id, "block": block, "position": position}
        )

    # Receive position from player group
    def player_position(self, event):
        player = event["player"]
        block = '_'.join([str(int(p)) for p in event['block'].split('_')])
        position = event['position']
        if self.player_id != player:
            self.throttle -= 1.1
            if self.throttle < 0:
                self.throttle = self.distance(block)
                self.send(bytes_data=bytes(f'pos:{player}:{block}:{position}', 'utf8'))

    # Receive position from cursors group
    def cursor_position(self, event):
        player = event["player"]
        block = '_'.join([str(int(p)) for p in event['block'].split('_')])
        position = event['position']
        if self.player_id != player:
            self.throttle -= 1.1
            if self.throttle < 0:
                self.throttle = self.distance(block)
                self.send(bytes_data=bytes(f'cur:{player}:{block}:{position}', 'utf8'))

    # Receive action from block group
    def cursor_action(self, event):
        player = event["player"]
        action = event["action"]
        block = event["block"]
        if self.player_id != player:
            self.throttle -= 1.1
            if self.throttle < 0 or block is None:
                self.send(bytes_data=bytes(f'act:{player}:{action}', 'utf8'))
                self.throttle = self.distance(block) if block else 0

    # Receive disconnect from block group
    def player_disconnect(self, event):
        player = event["player"]
        if self.player_id != player:
            self.send(bytes_data=bytes(f'off:{player}', 'utf8'))

    def distance(self, block: str):
        if self.block is None:
            return 0
        position_a = block.split('_')
        position_b = self.block.split('_')
        relative_position = [
            int(position_a[0]) - int(position_b[0]),
            int(position_a[1]) - int(position_b[1]),
            int(position_a[2]) - int(position_b[2]),
        ]
        distance_sqared = sum([
            relative_position[0]**2,
            relative_position[1]**2,
            relative_position[2]**2,
        ])
        return distance_sqared
