# Leeworld

## Build

    docker build -t leeworld .

First will fail, but print out XML from a file named Unity_v${UNITY_VERSION}.alf
Upload that file to https://license.unity3d.com/manual and get the .ulf file and save it as Unity_lic.ulf

Retry build with the new Unity_lic.ulf file

    docker build -t leeworld .

## Host

Create a file named `Server/LeeworldServer/production_settings.py`

```python
    # Server/LeeworldServer/production_settings.py
    from .settings import *
    SECRET_KEY = "with-a-random-secret-key-123*"
    DEBUG = False
    STATIC_ROOT = '/var/www/static/'
```

Run the server

    docker run -p 8000:8000 -v $(pwd)/Server/db.sqlite3:/app/db.sqlite3 -v $(pwd)/Server/LeeworldServer/production_settings.py:/app/LeeworldServer/production_settings.py -e DJANGO_SETTINGS_MODULE=LeeworldServer.production_settings leeworld