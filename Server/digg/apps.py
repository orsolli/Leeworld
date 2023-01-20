from django.apps import AppConfig


class DiggConfig(AppConfig):
    default_auto_field = 'django.db.models.BigAutoField'
    name = 'digg'

    def ready(self):
        from . import signals
        signals.terraformer_signal.connect(signals.terraformer_signal_handler, dispatch_uid='terraformer_signal_handler')
