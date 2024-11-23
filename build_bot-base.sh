#!/usr/bin/bash

FF_REPO="./CameraCaptureBot.Base/ffmpeg"

VERSION=$(cd $FF_REPO && git describe --tags | awk -F'-' '{print $1 "-" $2}')
if [[ -z "$VERSION" ]]; then
    echo "Error: Invaild tags, Have you clone all submodules?"
    exit 1
fi

IMAGE_NAME="registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot-base"
IMAGE_TAG="${IMAGE_NAME}:${VERSION}"

docker build ./CameraCaptureBot.Base -t "$IMAGE_TAG"
