mkdir -p /tmp/bot_build_temp

if [ ! -f "/tmp/bot_build_temp/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0.tar.xz" ]; then
    echo "File not found. Downloading..."
    wget -O /tmp/bot_build_temp/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0.tar.xz "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2024-08-31-12-50/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0.tar.xz"
fi

mkdir -p ffmpeg

tar --strip-components=1 -xf /tmp/bot_build_temp/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0.tar.xz -C ffmpeg

sudo docker build . -f Dockerfile.base -t registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot-base:7.0.2-6

rm -r ffmpeg
