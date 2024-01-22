import json

from asgiref.sync import async_to_sync
from channels.generic.websocket import JsonWebsocketConsumer
from boolean_geometry import TIME
from . import signals


def authenticate(user, player_id):
    from . import models

    return (
        user.is_active
        and user.is_authenticated
        and models.Player.objects.filter(user__id=user.pk, id=player_id).exists()
    )


class BlockConsumer(JsonWebsocketConsumer):
    def connect(self):
        self.block_group_name = None
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

    def receive_json(self, text_data_json):
        if self.user is None:
            return self.close(3000)

        action = text_data_json["action"]
        self.player_id = text_data_json["player"]
        block = None
        if action == "stop":
            self.duration = None
            self.signals.terraformer_signal.send(
                sender=None,
                action=self.signals.STOP,
                player_id=self.player_id,
            )
        elif action == "digg":
            block = "_".join([str(int(p)) for p in text_data_json["block"].split("_")])
            position = "_".join(
                [str(int(p, base=10)) for p in text_data_json["position"].split("_")]
            )
            results = self.signals.terraformer_signal.send(
                sender=None,
                action=self.signals.DIGG
                if self.duration is None
                else self.signals.PING,
                player_id=self.player_id,
                block_position=block.replace("_", ","),
                position=position.replace("_", ","),
            )
            for receiver, response in results:
                if receiver == self.signals.terraformer_signal_handler:
                    done, finishers, time = response
                    break
            if self.duration is None:
                self.duration = (
                    TIME
                    if int(self.player_id) in finishers
                    else max(0, TIME + (TIME - time))
                )
                async_to_sync(self.channel_layer.group_send)(
                    "players",
                    {
                        "type": "cursor_position",
                        "player": self.player_id,
                        "block": block,
                        "position": position,
                    },
                )
            if done:
                async_to_sync(self.channel_layer.group_send)(
                    self.block_group_name,
                    {"type": "block_message", "finishers": finishers, "block": block},
                )
            elif int(self.player_id) in finishers:
                self.send(
                    bytes_data=bytes(
                        f"digg:{block}:{min(99, int(100 - 100 * max(0, TIME - time) / self.duration))}:fin",
                        "utf8",
                    )
                )

            elif int(self.player_id) not in finishers:
                self.send(
                    bytes_data=bytes(
                        f"digg:{block}:{min(99, int(100 - 100 * max(0, TIME + (TIME - time)) / self.duration))}:queue",
                        "utf8",
                    )
                )

        async_to_sync(self.channel_layer.group_send)(
            "players",
            {
                "type": "cursor_action",
                "player": self.player_id,
                "action": action,
                "block": block,
            },
        )

    # Receive message from block group
    def block_message(self, event):
        finishers: list[int] = event["finishers"]
        block = event["block"]
        if int(self.player_id) in finishers:
            self.send(bytes_data=bytes(f"digg:{block}:{100}", "utf8"))
        self.send(bytes_data=bytes(f"block:{block}", "utf8"))
