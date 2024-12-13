using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.Options;
using VideoStreamCaptureBot.Core.Configs;

namespace VideoStreamCaptureBot.Core;

public class HeartBeatWorker(ILogger<HeartBeatWorker> logger,
    BotContext botCtx,
    IOptions<BotOption> botOptions) : BackgroundService
{
    private readonly HttpClient _httpClient = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested
               && (botOptions.Value.NotificationConfig.NotifyWebhookOnHeartbeat
               || botOptions.Value.NotificationConfig.NotifyAdminOnHeartbeat))
        {
            // delay before heartbeat. at the beginning of first loop bot maybe not logged in.
            await Task.Delay(TimeSpan.FromHours(botOptions.Value.NotificationConfig.HeartbeatIntervalHour), stoppingToken);

            if (botOptions.Value.NotificationConfig is
                { NotifyWebhookOnHeartbeat: true, WebhookUrl: not null })
            {
                var url = botOptions.Value.NotificationConfig.WebhookUrl!;

                var headers = botOptions.Value.NotificationConfig.WebhookHeaders;
                var resp = await _httpClient
                    .PostAsync(url, new StringContent($@"Time: `{DateTime.Now:s}`, {nameof(VideoStreamCaptureBot)} {GetType().Assembly.GetName().Version} alive\."), stoppingToken);
                if (!resp.IsSuccessStatusCode)
                {
                    logger.LogInformation("Webhook heartbeat invoked, {code} {msg}.", resp.StatusCode, resp.ReasonPhrase);
                }
                else
                {
                    logger.LogWarning("Webhook heartbeat invoked failed, {code} {msg}.", resp.StatusCode, resp.ReasonPhrase);
                }
            }

            if (botOptions.Value.NotificationConfig.NotifyAdminOnHeartbeat)
            {
                try
                {
                    await Parallel.ForEachAsync(botOptions.Value.AdminAccounts, stoppingToken,
                        async (account, _) =>
                    {
                        var message = MessageBuilder
                            .Friend(account)
                            .Text($"Time: {DateTime.Now:s}, {nameof(VideoStreamCaptureBot)} alive.")
                            .Build();
                        await botCtx.SendMessage(message);
                        logger.LogInformation("Bot heartbeat invoked.");
                    });
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Bot heartbeat invoked failed, {msg}.", e.Message);
                }
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        botCtx.Dispose();

        return base.StopAsync(cancellationToken);
    }
}
