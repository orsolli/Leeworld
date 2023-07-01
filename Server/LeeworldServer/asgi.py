"""
ASGI config for LeeworldServer project.

It exposes the ASGI callable as a module-level variable named ``application``.

For more information on this file, see
https://docs.djangoproject.com/en/4.1/howto/deployment/asgi/
"""

import os

from channels.auth import AuthMiddlewareStack
from channels.routing import ProtocolTypeRouter, URLRouter
from channels.security.websocket import AllowedHostsOriginValidator
from django.core.asgi import get_asgi_application

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'LeeworldServer.settings')
asgi_app = get_asgi_application()

import digg.routing
import player.routing

application = ProtocolTypeRouter(
    {
        'http': asgi_app,
        'websocket': AllowedHostsOriginValidator(
            AuthMiddlewareStack(
                URLRouter([
                    *digg.routing.websocket_urlpatterns,
                    *player.routing.websocket_urlpatterns,
                ])
            )
        ),
    }
)
