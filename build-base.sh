wget  -O ffmpeg.tar.xz "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2024-08-31-12-50/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0.tar.xz"
mkdir -p ffmpeg
tar -xJf ffmpeg.tar.xz -C ffmpeg --strip-components=1 "ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0/lib"

sudo docker build . -f Dockerfile.base -t camera-capture-bot-base:7.0.2-6

rm -r ffmpeg
rm ffmpeg.tar.xz
