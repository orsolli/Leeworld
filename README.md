# Leeworld

## Build

    docker build -t orjans/leeworld:hub --target hub .
    docker run -it --rm orjans/leeworld:hub > Unity.alf

Upload the Unity.alf file to https://license.unity3d.com/manual and get the .ulf file and save it as Unity_lic.ulf

    docker build -t orjans/leeworld:amd .

### ARM64

Download [arm.Dockerfile](arm.Dockerfile) to build an ARM version of the image. Replace `latest` with the desired version of leeworld

    docker build -f arm.Dockerfile --build-arg TAG=amd -t orjans/leeworld:arm .
    docker push orjans/leeworld:arm

Push the arm image to the same tag by using manifest

    docker manifest create orjans/leeworld:latest orjans/leeworld:amd orjans/leeworld:arm
    docker manifest push orjans/leeworld:latest

## Setup database

    touch db.sqlite3
    docker run \
        -it \
        --rm \
        -v $(pwd)/db.sqlite3:/app/db.sqlite3 \
        --entrypoint ./manage.py \
    orjans/leeworld migrate
    docker run \
        -it \
        --rm \
        -v $(pwd)/db.sqlite3:/app/db.sqlite3 \
        --entrypoint ./manage.py \
    orjans/leeworld createsuperuser


## Host

Create a file named `production_settings.py`

```python
    # production_settings.py
    from .settings import *
    SECRET_KEY = "with-a-random-secret-key-123*"
    DEBUG = False
    STATIC_ROOT = '/var/www/static/'
```

Create ssl certificate

    openssl req -newkey rsa:4096  -x509  -sha512  -days 365 -nodes -out crt.pem -keyout key.pem

Run the server

    docker run \
        -it \
        -p 4430:443 \
        -v $(pwd)/db.sqlite3:/app/db.sqlite3 \
        -v $(pwd)/production_settings.py:/app/LeeworldServer/production_settings.py \
            -e DJANGO_SETTINGS_MODULE=LeeworldServer.production_settings \
        -v $(pwd)/crt.pem:/app/crt.pem -v $(pwd)/key.pem:/app/key.pem \
    orjans/leeworld -e ssl:443:privateKey=key.pem:certKey=crt.pem

Visit https://localhost:4430/admin to register players