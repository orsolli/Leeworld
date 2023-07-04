ARG UNITY_VERSION=2022.3.4f1
ARG UNITY_CHANGESET=35713cd46cd7
ARG UNITYCI_REPO_VERSION=1.1.2

###########################
#         Builder         #
###########################

FROM unityci/hub:${UNITYCI_REPO_VERSION} AS hub

# Install editor
ARG UNITY_VERSION
ARG UNITY_CHANGESET
RUN unity-hub install --version ${UNITY_VERSION} --changeset ${UNITY_CHANGESET} --module webgl | tee output.log
RUN cat output.log | grep 'Error' | exit $(wc -l)

RUN "/opt/unity/editors/${UNITY_VERSION}/Editor/Unity" -batchmode -nographics -createManualActivationFile || cat "Unity_v${UNITY_VERSION}.alf"
RUN cat "Unity_v${UNITY_VERSION}.alf" > Unity.alf
CMD [ "cat", "Unity.alf" ]

###########################
#          Editor         #
###########################

FROM unityci/base:${UNITYCI_REPO_VERSION} as editor

# Always put "Editor" and "modules.json" directly in $UNITY_PATH
ARG UNITY_VERSION
COPY --from=hub /opt/unity/editors/${UNITY_VERSION}/ "${UNITY_PATH}/"

# Add a file containing the version for this build
RUN echo $version > "${UNITY_PATH}/version"

# Alias to "unity-editor" with default params
RUN echo '#!/bin/bash\nxvfb-run -ae /dev/stdout "${UNITY_PATH}/Editor/Unity" -batchmode "$@"' > /usr/bin/unity-editor \
    && chmod +x /usr/bin/unity-editor

# First run will print the .alf file. Upload it to https://license.unity3d.com/manual to get the .ulf file and save it as Unity_lic.ulf for this next step
COPY "Unity_lic.ulf" /root/.local/share/unity3d/Unity/

WORKDIR /app
COPY ./Packages/ /app/Packages/
COPY ./ProjectSettings/ /app/ProjectSettings/
COPY ./Assets/ /app/Assets/

RUN /usr/bin/unity-editor -logfile - -quit -projectPath /app -executeMethod Leeworld.Builder.BuildProject

###########################
#          Python         #
###########################

FROM python:3.11

RUN pip install -U 'Twisted[tls,http2]'
COPY ./Server/requirements.txt /app/
WORKDIR /app
RUN pip install -r requirements.txt -U
COPY Server /app

COPY --from=editor /app/Build/WebGL/ /Build/WebGL/

# Alias to start container with default params
RUN echo '#!/bin/bash \n\
    ./manage.py collectstatic \n\
    daphne LeeworldServer.asgi:application "$@"\n\
    ' > /usr/bin/leeworld \
    && chmod +x /usr/bin/leeworld
ENTRYPOINT [ "/usr/bin/leeworld" ]
