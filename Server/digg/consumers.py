import json

from asgiref.sync import async_to_sync
from channels.generic.websocket import JsonWebsocketConsumer

from .signals import terraformer_signal_handler, DIGG, STOP


class BlockConsumer(JsonWebsocketConsumer):
    def connect(self):
        self.block_id = self.scope["url_route"]["kwargs"]["block_id"]
        self.block_group_name = "block_%s" % self.block_id

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

        if action == 'stop':
            terraformer_signal_handler(STOP, player_id=text_data_json['player'])
        elif action == 'digg':
            status = terraformer_signal_handler(
                DIGG,
                player_id=text_data_json['player'],
                block_position=text_data_json['block'].replace('_', ','),
                position=text_data_json['position'].replace('_', ','),
            )
            if status == 'Waiting':
                self.send(bytes_data=b'Waiting')
                return
            elif status == 'Done':
                async_to_sync(self.channel_layer.group_send)(
                    self.block_group_name, {"type": "block_message", "message": 'Done'}
                )

    # Receive message from block group
    def block_message(self, event):
        message: str = event["message"]

        # Send message to WebSocket
        self.send(bytes_data=bytes(message, 'utf8'))
