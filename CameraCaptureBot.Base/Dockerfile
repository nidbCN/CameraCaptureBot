FROM ubuntu:24.04 AS base
RUN apt-get update && apt-get install -y ca-certificates
COPY ./tuna.sources /etc/apt/sources.list.d/ubuntu.sources
RUN apt-get update && \
    apt-get install -y unminimize locales && \
    rm -rf /var/lib/apt/lists/* && \
    localedef -i en_US -c -f UTF-8 -A /usr/share/locale/locale.alias en_US.UTF-8
ENV LANG=en_US.utf8
RUN apt-get update && apt-get upgrade -y && apt-get install -y nasm \
    libnuma-dev \
    libunistring-dev \
    libaom-dev \
    libsvtav1-dev \
    libsvtav1enc-dev \
    libsvtav1dec-dev \
    libx264-dev \
    libx265-dev \
    libwebp-dev \
    autoconf \
    automake \
    build-essential \
    cmake \
    git-core \
    libass-dev \
    libfreetype6-dev \
    libgnutls28-dev \
    libmp3lame-dev \
    libtool \
    libvorbis-dev \
    meson \
    ninja-build \
    pkg-config \
    texinfo \
    wget \
    yasm \
    zlib1g-dev

FROM base AS build
ENV WORK_DIR=/workspace
ENV BUILD_DIR=$WORK_DIR/build_output
WORKDIR $WORK_DIR
COPY ./ffmpeg $WORK_DIR
COPY ./scripts/*.sh $WORK_DIR
RUN ./pre-build.sh
RUN make -j16 && make install
RUN ./post-build.sh

FROM debian:bookworm-slim AS runtime
RUN export DEBIAN_FRONTEND=noninteractive && \
    cp /etc/apt/sources.list.d/debian.sources /tmp/debian.sources.bak && \
    sed -i 's|deb.debian.org/debian|ftp.cn.debian.org/debian|g' /etc/apt/sources.list.d/debian.sources && \
    apt-get update && \
    apt-get install -y --no-install-recommends \
    ca-certificates && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists && \
    mv /tmp/debian.sources.bak /etc/apt/sources.list.d/debian.sources && \
    export DEBIAN_FRONTEND=""

FROM runtime AS final
COPY --from=build /workspace/build_output/lib/* /usr/lib/x86_64-linux-gnu/
