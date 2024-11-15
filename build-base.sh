echo "Start make temp dir"
mkdir -p /tmp/bot_build_temp

echo "Start check download cache"
if [ ! -f "/tmp/bot_build_temp/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0.tar.xz" ]; then
    echo "Start download ffmpeg"
    wget -O /tmp/bot_build_temp/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0.tar.xz "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2024-08-31-12-50/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0.tar.xz"
fi

echo "Start make local temp dir"
mkdir -p ffmpeg

echo "Start decompress ffmpeg"
tar --strip-components=1 -xf /tmp/bot_build_temp/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-gpl-shared-7.0.tar.xz -C ./ffmpeg
echo "End decompress"

echo "Start process so files"
rm ./ffmpeg/lib/*.so
rm -r ./ffmpeg/lib/pkgconfig

for file in ./ffmpeg/lib/*.so.*; do
    if [ -L "$file" ]; then
        target=$(readlink "$file")
        rm "$file"
        mv "./ffmpeg/lib/$target" "$file"
    fi
done

ls -l ./ffmpeg/lib
echo "End process"

echo "Start build docker image"
sudo docker build . -f Dockerfile.base -t registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot-base:7.0.2-6
echo "End build"

echo "Start clean resources"
rm -r ffmpeg
