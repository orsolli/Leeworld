# Leeworld

## Build

    docker build -t orjans/leeworld:hub --target hub .
    docker run -it --rm orjans/leeworld:hub > Unity.alf

Upload the Unity.alf file to https://license.unity3d.com/manual and get the .ulf file and save it as Unity_lic.ulf

    docker build -t orjans/leeworld .

## Host

Create a file named `Server/LeeworldServer/production_settings.py`

```python
    # Server/LeeworldServer/production_settings.py
    from .settings import *
    SECRET_KEY = "with-a-random-secret-key-123*"
    DEBUG = False
    STATIC_ROOT = '/var/www/static/'
```

Create ssl certificate

    openssl req -newkey rsa:4096  -x509  -sha512  -days 365 -nodes -out crt.pem -keyout key.pem

Run the server

    docker run \
        -p 4430:443 \
        -v $(pwd)/Server/db.sqlite3:/app/db.sqlite3 \
        -v $(pwd)/Server/LeeworldServer/production_settings.py:/app/LeeworldServer/production_settings.py \
            -e DJANGO_SETTINGS_MODULE=LeeworldServer.production_settings \
        -v $(pwd)/Server/crt.pem:/app/crt.pem -v $(pwd)/Server/key.pem:/app/key.pem \
    orjans/leeworld -e ssl:443:privateKey=key.pem:certKey=crt.pem
