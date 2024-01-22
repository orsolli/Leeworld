from functools import lru_cache
from asgiref.sync import async_to_sync
from channels.generic.websocket import JsonWebsocketConsumer
from boolean_geometry import TIME


def authenticate(user, player_id):
    from . import models

    return (
        user.is_active
        and user.is_authenticated
        and models.Player.objects.filter(user__id=user.pk, id=player_id).exists()
    )


@lru_cache(1048510)
def distance(a: str, b: str):
    if a is None:
        return 0
    position_a = b.split("_")
    position_b = a.split("_")
    relative_position = [
        int(position_a[0]) - int(position_b[0]),
        int(position_a[1]) - int(position_b[1]),
        int(position_a[2]) - int(position_b[2]),
    ]
    distance_sqared = sum(
        [
            relative_position[0] ** 2,
            relative_position[1] ** 2,
            relative_position[2] ** 2,
        ]
    )
    return distance_sqared


class PlayerConsumer(JsonWebsocketConsumer):
    def connect(self):
        self.players_group = None
        self.player_id = self.scope["url_route"]["kwargs"]["player_id"]
        self.user = self.scope["user"]
        if not authenticate(self.user, self.player_id):
            self.user = None
            self.player_id = 0

        self.players_group = "players"
        self.block = "0_0_0"
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

    def receive_json(self, text_data_json):
        part = text_data_json["part"]
        block = "_".join([str(int(p)) for p in text_data_json["block"].split("_")])
        if part == "player":
            self.block = block
        elif self.user is None:
            return self.close(3000)  # Unauthenticated users can not point
        position = "_".join(
            [str(int(p, base=10)) for p in text_data_json["position"].split("_")]
        )
        async_to_sync(self.channel_layer.group_send)(
            self.players_group,
            {
                "type": f"{part}_position",
                "player": self.player_id,
                "block": block,
                "position": position,
            },
        )

    # Receive position from player group
    def player_position(self, event):
        player = event["player"]
        block = "_".join([str(int(p)) for p in event["block"].split("_")])
        position = event["position"]
        if self.player_id != player:
            self.throttle -= 1.1
            if self.throttle < 0:
                self.throttle = distance(*sorted([self.block, block]))
                self.send(bytes_data=bytes(f"pos:{player}:{block}:{position}", "utf8"))

    # Receive position from cursors group
    def cursor_position(self, event):
        player = event["player"]
        block = "_".join([str(int(p)) for p in event["block"].split("_")])
        position = event["position"]
        if self.player_id != player:
            self.throttle -= 1.1
            if self.throttle < 0:
                self.throttle = distance(*sorted([self.block, block]))
                self.send(bytes_data=bytes(f"cur:{player}:{block}:{position}", "utf8"))

    # Receive action from block group
    def cursor_action(self, event):
        player = event["player"]
        action = event["action"]
        block = event["block"]
        if self.player_id != player:
            self.throttle -= 1.1
            if self.throttle < 0 or block is None:
                self.send(bytes_data=bytes(f"act:{player}:{action}", "utf8"))
                self.throttle = distance(*sorted([self.block, block])) if block else 0

    # Receive disconnect from block group
    def player_disconnect(self, event):
        player = event["player"]
        if self.player_id != player:
            self.send(bytes_data=bytes(f"off:{player}", "utf8"))
