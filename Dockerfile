FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["./VideoStreamCaptureBot.Core/VideoStreamCaptureBot.Core.csproj", "./VideoStreamCaptureBot.Core/"]
RUN mkdir -p ./Lagrange.Core/Lagrange.Core
COPY ["./Lagrange.Core/Lagrange.Core/Lagrange.Core.csproj", "./Lagrange.Core/Lagrange.Core/"]
RUN dotnet restore "./VideoStreamCaptureBot.Core/VideoStreamCaptureBot.Core.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./VideoStreamCaptureBot.Core/VideoStreamCaptureBot.Core.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoStreamCaptureBot.Core/VideoStreamCaptureBot.Core.csproj" \
    -c $BUILD_CONFIGURATION \
    --self-contained true \
    --runtime linux-x64 \
    -o /app/publish

FROM registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot-base:n7.0.2-26 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./VideoStreamCaptureBot.Core"]
