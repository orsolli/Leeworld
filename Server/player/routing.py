from django.urls import re_path

from . import consumers

websocket_urlpatterns = [
    re_path(r"ws/player/(?P<player_id>\w+)/$", consumers.PlayerConsumer.as_asgi()),
]
