#!/usr/bin/bash
git clone -b n7.0 https://git.ffmpeg.org/ffmpeg.git ./CameraCaptureBot.Base/ffm`peg
sudo docker build . -t camera-capture-bot-base:7.0.2-26
