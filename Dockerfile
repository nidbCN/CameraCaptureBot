FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim AS base
USER root
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

FROM base AS final
WORKDIR /app
ARG FFMPEG_URL=https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2024-04-30-12-51/ffmpeg-n7.0-21-gfb8f0ea7b3-linux64-gpl-7.0.tar.xz

ADD $FFMPEG_URL /opt/ffmpeg
COPY --from=publish /app/publish .

ENV LD_LIBRARY_PATH /opt/ffmpeg/lib
ENTRYPOINT ["dotnet", "CameraCaptureBot.Core.dll"]
