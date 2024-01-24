from django.apps import AppConfig
from django.contrib import admin
import signal


class DiggConfig(AppConfig):
    default_auto_field = "django.db.models.BigAutoField"
    name = "digg"

    def ready(self):
        from . import models
        from . import signals

        admin.site.register(models.Block)
        admin.site.register(models.Terraformer)
        admin.site.register(models.TerraformQueue)
        signal.signal(signal.SIGINT, signals.shutdown_handler)
        signals.terraformer_signal.connect(
            signals.terraformer_signal_handler,
            dispatch_uid="terraformer_signal_handler",
        )
