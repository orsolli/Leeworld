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
        if action == 'stop':
            signals.terraformer_signal.send(
                sender=None,
                action=signals.STOP,
                player_id=self.player_id,
            )
        elif action == 'digg':
            results = signals.terraformer_signal.send(
                sender=None,
                action=signals.DIGG,
                player_id=self.player_id,
                block_position=text_data_json['block'].replace('_', ','),
                position=text_data_json['position'].replace('_', ','),
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

    # Receive message from block group
    def block_message(self, event):
        finishers: list[int] = event["finishers"]
        if self.player_id and int(self.player_id) in finishers:
            self.send(bytes_data=bytes('Done', 'utf8'))
        else:
            self.send(bytes_data=bytes('Dirty', 'utf8'))
