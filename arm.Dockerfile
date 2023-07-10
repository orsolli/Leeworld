ARG TAG=latest
FROM orjans/leeworld:${TAG}
FROM arm64v8/python:3.11-slim

WORKDIR /app

COPY --from=0 /app /app
RUN pip install -r requirements.txt -U

COPY --from=0 /Build/WebGL/ /Build/WebGL/

# Alias to start container with default params
RUN echo '#!/bin/bash \n\
    ./manage.py collectstatic \n\
    daphne LeeworldServer.asgi:application "$@"\n\
    ' > /usr/bin/leeworld \
    && chmod +x /usr/bin/leeworld
ENTRYPOINT [ "/usr/bin/leeworld" ]
