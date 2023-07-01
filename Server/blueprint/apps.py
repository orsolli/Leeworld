from django.apps import AppConfig
from django.contrib import admin


class BlueprintConfig(AppConfig):
    default_auto_field = 'django.db.models.BigAutoField'
    name = 'blueprint'

    def ready(self):
        from . import models
        admin.site.register(models.Blueprint)