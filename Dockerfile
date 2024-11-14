#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["./CameraCaptureBot.Core/CameraCaptureBot.Core.csproj", "./CameraCaptureBot.Core/"]
RUN mkdir -p ./Lagrange.Core/Lagrange.Core
COPY ["./Lagrange.Core/Lagrange.Core/Lagrange.Core.csproj", "./Lagrange.Core/Lagrange.Core/"]
RUN dotnet restore "./CameraCaptureBot.Core/CameraCaptureBot.Core.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./CameraCaptureBot.Core/CameraCaptureBot.Core.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CameraCaptureBot.Core/CameraCaptureBot.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS ffmpeg
ENV DEBIAN_FRONTEND=noninteractive
USER root
RUN apt update && \
    sed -i '/^Suites:.*bookworm[^-]/ s/$/ testing/' /etc/apt/sources.list.d/debian.sources && \
    apt update && \
    apt install -y -t testing \ 
        libatomic1 \
        libavcodec60 \
        libavdevice60 \
        libavfilter10 \
        libavformat60 \
        libavutil58 \
        libpostproc57 \
        libswresample4 \
        libswscale7

FROM ffmpeg AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CameraCaptureBot.Core.dll"]
