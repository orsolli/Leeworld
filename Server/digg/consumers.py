import json

from asgiref.sync import async_to_sync
from channels.generic.websocket import JsonWebsocketConsumer

from . import signals


class BlockConsumer(JsonWebsocketConsumer):
    def connect(self):
        self.block_id = self.scope["url_route"]["kwargs"]["block_id"]
        self.block_group_name = "block_%s" % self.block_id
        self.player_id = None

        async_to_sync(self.channel_layer.group_add)(
            self.block_group_name, self.channel_name
        )

        self.accept()

    def disconnect(self, close_code):
        async_to_sync(self.channel_layer.group_discard)(
            self.block_group_name, self.channel_name
        )

    def receive(self, bytes_data):
        text_data_json = json.loads(bytes_data)
        action = text_data_json["action"]
        self.player_id=text_data_json['player']
        block = None
        if action == 'stop':
            signals.terraformer_signal.send(
                sender=None,
                action=signals.STOP,
                player_id=self.player_id,
            )
        elif action == 'digg':
            block = text_data_json['block']
            position = text_data_json['position']
            results = signals.terraformer_signal.send(
                sender=None,
                action=signals.DIGG,
                player_id=self.player_id,
                block_position=block.replace('_', ','),
                position=position.replace('_', ','),
            )
            async_to_sync(self.channel_layer.group_send)(
                "cursors", {"type": "cursor_position", "player": self.player_id, "block": block, "position": position}
            )
            for receiver, response in results:
                if receiver == signals.terraformer_signal_handler:
                    finishers = response
                    break
            if int(self.player_id) not in finishers:
                self.send(bytes_data=b'Waiting')
            if len(finishers):
                async_to_sync(self.channel_layer.group_send)(
                    self.block_group_name, {"type": "block_message", "finishers": finishers}
                )
        
        async_to_sync(self.channel_layer.group_send)(
            "cursors", {"type": "cursor_action", "player": self.player_id, "action": action, "block": block}
        )

    # Receive message from block group
    def block_message(self, event):
        finishers: list[int] = event["finishers"]
        if self.player_id and int(self.player_id) in finishers:
            self.send(bytes_data=bytes('Done', 'utf8'))
        else:
            self.send(bytes_data=bytes('Dirty', 'utf8'))

class PlayerConsumer(JsonWebsocketConsumer):
    def connect(self):
        self.player_id = self.scope["url_route"]["kwargs"]["player_id"]
        self.players_group = "players"
        self.block = None

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
        block = text_data_json['block']
        if part == 'player':
            self.block = block
        position = text_data_json['position']
        async_to_sync(self.channel_layer.group_send)(
            self.players_group, {"type": f"{part}_position", "player": self.player_id, "block": block, "position": position}
        )

    # Receive position from player group
    def player_position(self, event):
        player = event["player"]
        block = event["block"]
        position = event["position"]
        if self.player_id != player and self.nearby(block):
            self.send(bytes_data=bytes(f'pos:{player}:{block}:{position}', 'utf8'))

    # Receive position from cursors group
    def cursor_position(self, event):
        player = event["player"]
        block = event["block"]
        position = event["position"]
        if self.player_id != player and self.nearby(block):
            self.send(bytes_data=bytes(f'cur:{player}:{block}:{position}', 'utf8'))

    # Receive action from block group
    def cursor_action(self, event):
        player = event["player"]
        action = event["action"]
        block = event["action"]
        if self.player_id != player and (block is None or self.nearby(block)):
            self.send(bytes_data=bytes(f'act:{player}:{action}', 'utf8'))

    # Receive disconnect from block group
    def player_disconnect(self, event):
        player = event["player"]
        if self.player_id != player:
            self.send(bytes_data=bytes(f'off:{player}', 'utf8'))

    def nearby(self, block: str):
        if self.block is None:
            return False
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
        return distance_sqared < 36
