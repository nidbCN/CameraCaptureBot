﻿using StreamingCaptureBot.Core.Services;

namespace StreamingCaptureBot.Core.Controllers;

public class BotController(
    ILogger<BotController> logger,
    CaptureService captureService
    )
{
    public async Task<ActionResult> HandleCaptureImageCommand(BotRequest request)
    {
        var response = new ActionResult();

        try
        {
            var (result, image) = await captureService.CaptureImageAsync();

            if (!result || image is null)
            {
                // 编解码失败
                logger.LogError("Capture failed, send error message.");
                response.Message = "杰哥不要！（图像获取失败）";
            }
            else
            {
                response.Message = "开玩笑，我超勇的好不好";
                response.Image = image;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to decode or encode.");
            response.Message = "你最好不要说出去，我知道你的学校和班级（图像编解码崩溃）：\n" + e.Message + e.StackTrace;
        }
        finally
        {
            await captureService.FlushDecoderBufferAsync(CancellationToken.None);
        }

        return response;
    }

    public record ActionResult
    {
        public string? Message { get; set; }
        public byte[]? Image { get; set; }
    }

    public record BotRequest
    {
        public uint? GroupUin { get; set; }
        public uint FriendUin { get; set; }
        public string? MessageText { get; set; }
    }
}
