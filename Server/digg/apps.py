from django.apps import AppConfig
import signal

class DiggConfig(AppConfig):
    default_auto_field = 'django.db.models.BigAutoField'
    name = 'digg'

    def ready(self):
        from . import signals
        signal.signal(signal.SIGINT, signals.shutdown)
        signals.terraformer_signal.connect(signals.terraformer_signal_handler, dispatch_uid='terraformer_signal_handler')
