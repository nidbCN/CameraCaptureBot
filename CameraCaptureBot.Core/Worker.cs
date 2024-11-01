using System.IO.IsolatedStorage;
using System.Text.Json;
using CameraCaptureBot.Core.Configs;
using CameraCaptureBot.Core.Services;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Options;
using BotLogLevel = Lagrange.Core.Event.EventArg.LogLevel;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CameraCaptureBot.Core;

public class Worker(ILogger<Worker> logger,
    CaptureService captureService,
    BotContext botCtx,
    IsolatedStorageFile isoStorage,
    IOptions<BotOption> botOptions) : BackgroundService
{
    private async Task SendCaptureMessage(MessageBuilder message, BotContext thisBot)
    {
        try
        {
            var (result, image) = await captureService.CaptureImageAsync();

            if (!result || image is null)
            {
                // 编解码失败
                logger.LogError("Decode failed, send error message.");
                message.Text("杰哥不要！（图像编解码失败）");
            }
            else
            {
                message.Text("开玩笑，我超勇的好不好");
                message.Image(image);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to decode and encode.");
            message.Text("杰哥不要！（图像编解码器崩溃）");
            message.Text("你最好不要说出去，我知道你的学校和班级：\n" + e.Message + e.StackTrace);
        }
        finally
        {
            var sendTask = thisBot.SendMessage(message.Build());
            var flushTask = captureService.FlushDecoderBufferAsync(CancellationToken.None);
            await Task.WhenAll(sendTask, flushTask);
        }
    }

    private async Task SendErrorToAccounts(IList<uint> accounts, Exception e)
        => await Parallel.ForEachAsync(accounts, async (account, token) =>
        {
            var msg = MessageBuilder
                 .Friend(account)
               .Text(e.Message + '\n' + e.StackTrace)
            .Build();
            await botCtx.SendMessage(msg);
        });

    private async Task ProcessMessage2(MessageChain message, BotContext thisBot)
    {
        var isGroup = message.GroupUin is not null;

        var replyUin = message.FriendUin;
        var allowedList = botOptions.Value.AllowedFriends;
        var groupInfo = string.Empty;
        var builderBuilder = MessageBuilder.Friend;

        if (isGroup)
        {
            replyUin = message.GroupUin!.Value;
            allowedList = botOptions.Value.AllowedGroups;
            groupInfo = $".Group({message.GroupUin})";
            builderBuilder = MessageBuilder.Group;
        }

        var scope = logger.BeginScope($"{nameof(BotContext)}{groupInfo}@{message.FriendUin}");

        try
        {
            var textMessages = message
                .Select(m => m as TextEntity)
                .Where(m => m != null);

            if (!textMessages.Any(m => m!.Text.StartsWith("让我看看")))
                return;

            logger.LogInformation("Received command `{msg}`.", message.ToPreviewString());

            var messageBuilder = builderBuilder.Invoke(replyUin);

            if (allowedList?.Contains(replyUin) ?? true)
            {
                logger.LogInformation("Allowed user, send captured image.");
                await SendCaptureMessage(messageBuilder, thisBot);
            }
            else
            {
                logger.LogWarning("UnAllowed user, reject.");
                await botCtx.SendMessage(messageBuilder.Text("杰哥，你...你干嘛啊（用户不在白名单）").Build());
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process message.");

            if (!botOptions.Value.NotifyAdminOnException)
                return;

            logger.LogInformation("{opt} enabled, send error message to admin accounts.",
                nameof(BotOption.NotifyAdminOnException));
            var admins = botOptions.Value.AdminAccounts;
            if (admins.Count == 0)
            {
                logger.LogWarning("No admin accounts has been configured, can not send message.");
            }
            else
            {
                try
                {
                    await SendErrorToAccounts(admins, e);
                }
                catch (Exception sendError)
                {
                    logger.LogError(sendError, "Unable to send message");
                }
            }
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private void ConfigureEvents()
    {
        botCtx.Invoker.OnBotLogEvent += (_, @event) =>
        {
            using (logger.BeginScope($"{nameof(Lagrange.Core.Event.EventArg.BotLogEvent)}"))
            {
                logger.Log(@event.Level switch
                {
                    BotLogLevel.Debug => LogLevel.Trace,
                    BotLogLevel.Verbose => LogLevel.Debug,
                    BotLogLevel.Information => LogLevel.Information,
                    BotLogLevel.Warning => LogLevel.Warning,
                    BotLogLevel.Exception => LogLevel.Error,
                    BotLogLevel.Fatal => LogLevel.Critical,
                    _ => throw new NotImplementedException(),
                }, "event time:{time}, msg:'{msg}'", @event.EventTime.ToLocalTime(), @event.EventMessage);
            }
        };

        botCtx.Invoker.OnBotCaptchaEvent += (bot, @event) =>
        {
            logger.LogWarning("Need captcha, url: {msg}", @event.Url);
            logger.LogInformation("Input response json string:");
            var json = Console.ReadLine();

            if (json is null || string.IsNullOrWhiteSpace(json))
            {
                logger.LogError("You input nothing! can't boot.");
                throw new ApplicationException("Can't boot without captcha.");
            }

            try
            {
                var jsonObj = JsonSerializer.Deserialize<IDictionary<string, string>>(json);

                if (jsonObj is null)
                {
                    logger.LogError("Deserialize `{json}` failed, result is null.", json);
                }
                else
                {
                    const string ticket = "ticket";
                    const string randStr = "randstr";

                    if (jsonObj.TryGetValue(ticket, out var ticketValue)
                        && jsonObj.TryGetValue(randStr, out var randStrValue))
                    {
                        logger.LogInformation("Receive captcha, ticket {t}, rand-str {s}", ticketValue, randStrValue);
                        bot.SubmitCaptcha(ticketValue, randStrValue);
                    }
                    else
                    {
                        throw new ApplicationException("Can't boot without captcha.");
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Deserialize failed! str: {s}", json);
                throw;
            }
        };

        botCtx.Invoker.OnBotOnlineEvent += (_, _) =>
        {
            logger.LogInformation("Login Success! Bot online.");
        };

        botCtx.Invoker.OnGroupMessageReceived += async (bot, @event) =>
        {
            await ProcessMessage2(@event.Chain, bot);
        };

        botCtx.Invoker.OnFriendMessageReceived += async (bot, @event) =>
        {
            await ProcessMessage2(@event.Chain, bot);
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConfigureEvents();
        await StartUp.LoginAsync(botCtx, isoStorage, logger, botOptions.Value, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        botCtx.Dispose();
        isoStorage.Close();
        isoStorage.Dispose();

        return base.StopAsync(cancellationToken);
    }
}
