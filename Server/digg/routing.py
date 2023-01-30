from django.urls import re_path

from . import consumers

websocket_urlpatterns = [
    re_path(r"ws/block/(?P<block_id>\w+)/$", consumers.BlockConsumer.as_asgi()),
    re_path(r"ws/player/(?P<player_id>\w+)/$", consumers.PlayerConsumer.as_asgi()),
    re_path(r"ws/cursor/(?P<player_id>\w+)/$", consumers.CursorConsumer.as_asgi()),
]
