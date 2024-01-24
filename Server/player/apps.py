from django.apps import AppConfig
from django.contrib import admin


class PlayerConfig(AppConfig):
    default_auto_field = "django.db.models.BigAutoField"
    name = "player"

    def ready(self):
        from . import models

        admin.site.register(models.Player)
