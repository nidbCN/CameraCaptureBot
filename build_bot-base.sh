#!/usr/bin/bash

FF_REPO="./VideoStreamCaptureBot.Base/ffmpeg"

if [ -d "$FF_REPO" ]; then
    echo "ffmpeg has been cloned."
else
    git clone -b release/7.0 https://git.ffmpeg.org/ffmpeg.git $FF_REPO
fi

VERSION=$(cd $FF_REPO && git describe --tags | awk -F'-' '{print $1 "-" $2}')
if [[ -z "$VERSION" ]]; then
    echo "Error: Invaild tags, Have you clone all submodules?"
    exit 1
fi

IMAGE_NAME="registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot-base"
IMAGE_TAG="${IMAGE_NAME}:${VERSION}"

docker build ./VideoStreamCaptureBot.Base -t "$IMAGE_TAG"
