using System.IO.IsolatedStorage;
using System.Text.Json;
using CameraCaptureBot.Core.Configs;
using CameraCaptureBot.Core.Services;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Options;
using BotLogLevel = Lagrange.Core.Event.EventArg.LogLevel;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CameraCaptureBot.Core;
internal class BotHost : IHostedLifecycleService
{
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly BotContext _botCtx;
    private readonly CaptureService _captureService;
    private readonly IsolatedStorageFile _isoStorage;
    private readonly IOptions<BotOption> _botOptions;

    public BotHost(
        ILogger<BotHost> logger,
        IHostApplicationLifetime appLifetime,
        IOptions<BotOption> botOptions,
        BotContext botCtx,
        CaptureService captureService,
        IsolatedStorageFile isoStorage)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _botOptions = botOptions;
        _botCtx = botCtx;
        _captureService = captureService;
        _isoStorage = isoStorage;

        _appLifetime.ApplicationStarted.Register(OnStarted);
        _appLifetime.ApplicationStopping.Register(OnStopping);
        _appLifetime.ApplicationStopped.Register(OnStopped);
    }

    private async Task LoginAsync(CancellationToken stoppingToken)
    {
        var loggedIn = false;

        var pwdLoginTimeoutTokenSrc = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            _logger.LogInformation("Try login use password, timeout value: 5 sec.");
            loggedIn = await _botCtx.LoginByPassword(pwdLoginTimeoutTokenSrc.Token);
        }
        catch (TaskCanceledException e)
        {
            _logger.LogError(e, "Password login timeout, try QRCode.");
        }
        finally
        {
            pwdLoginTimeoutTokenSrc.Dispose();
        }

        if (!loggedIn)
        {
            _logger.LogWarning("Password login failed, try QRCode.");

            var (url, _) = await _botCtx.FetchQrCode()
                           ?? throw new ApplicationException(message: "Fetch QRCode failed.\n");

            // The QrCode will be expired in 2 minutes.
            var qrLoginTimeoutTokenSrc = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var link = new UriBuilder("https://util-functions.azurewebsites.net/api/QrCode")
            {
                Query = await new FormUrlEncodedContent(
                    new Dictionary<string, string> {
                        {"content", url}
                    }).ReadAsStringAsync(stoppingToken)
            };

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Open link `{link}` and scan the QRCode to login.", link.Uri.ToString());
            }
            else
            {
                Console.WriteLine("Open link `{0}` and scan the QRCode to login.", link.Uri.ToString());
            }

            // Use both external stopping token and login timeout token.
            using var qrLoginStoppingTokenSrc = CancellationTokenSource
                .CreateLinkedTokenSource(stoppingToken, qrLoginTimeoutTokenSrc.Token);

            try
            {
                await _botCtx.LoginByQrCode(qrLoginStoppingTokenSrc.Token);
            }
            catch (TaskCanceledException e)
            {
                _logger.LogError(e, "QRCode login timeout, can't boot.");
                throw;
            }
        }

        // save device info and keystore
        try
        {
            await using var deviceInfoFileStream = _isoStorage.OpenFile(_botOptions.Value.DeviceInfoFile, FileMode.OpenOrCreate, FileAccess.Write);
            await JsonSerializer.SerializeAsync(deviceInfoFileStream, _botCtx.UpdateDeviceInfo(), cancellationToken: stoppingToken);

            await using var keyFileStream = _isoStorage.OpenFile(_botOptions.Value.KeyStoreFile, FileMode.OpenOrCreate, FileAccess.Write);
            await JsonSerializer.SerializeAsync(keyFileStream, _botCtx.UpdateKeystore(), cancellationToken: stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Save device info and key files failed.");
        }
        finally
        {
            _isoStorage.Close();
        }
    }

    private async Task SendCaptureMessage(MessageBuilder message, BotContext thisBot)
    {
        try
        {
            var (result, image) = await _captureService.CaptureImageAsync();

            if (!result || image is null)
            {
                // 编解码失败
                _logger.LogError("Decode failed, send error message.");
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
            _logger.LogError(e, "Failed to decode and encode.");
            message.Text("杰哥不要！（图像编解码器崩溃）");
            message.Text("你最好不要说出去，我知道你的学校和班级：\n" + e.Message + e.StackTrace);
        }
        finally
        {
            var sendTask = thisBot.SendMessage(message.Build());
            var flushTask = _captureService.FlushDecoderBufferAsync(CancellationToken.None);
            await Task.WhenAll(sendTask, flushTask);
        }
    }

    private async Task SendErrorToAccounts(IList<uint> accounts, Exception e)
        => await Parallel.ForEachAsync(accounts, async (account, _) =>
        {
            var msg = MessageBuilder
                .Friend(account)
                .Text(e.Message + '\n' + e.StackTrace)
                .Build();
            await _botCtx.SendMessage(msg);
        });

    private void ProcessLog(BotContext bot, BotLogEvent @event)
    {
        using (_logger.BeginScope($"{nameof(Lagrange.Core.Event.EventArg.BotLogEvent)}"))
        {
            _logger.Log(@event.Level switch
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
    }

    private void ProcessCaptcha(BotContext bot, BotCaptchaEvent @event)
    {
        _logger.LogWarning("Need captcha, url: {msg}", @event.Url);
        _logger.LogInformation("Input response json string:");
        var json = Console.ReadLine();

        if (json is null || string.IsNullOrWhiteSpace(json))
        {
            _logger.LogError("You input nothing! can't boot.");
            throw new ApplicationException("Can't boot without captcha.");
        }

        try
        {
            var jsonObj = JsonSerializer.Deserialize<IDictionary<string, string>>(json);

            if (jsonObj is null)
            {
                _logger.LogError("Deserialize `{json}` failed, result is null.", json);
            }
            else
            {
                const string ticket = "ticket";
                // ReSharper disable once StringLiteralTypo
                const string randStr = "randstr";

                if (jsonObj.TryGetValue(ticket, out var ticketValue)
                    && jsonObj.TryGetValue(randStr, out var randStrValue))
                {
                    _logger.LogInformation("Receive captcha, ticket {t}, rand-str {s}", ticketValue, randStrValue);
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
            _logger.LogError(e, "Deserialize failed! str: {s}", json);
            throw;
        }
    }

    private void ProcessBotOnline(BotContext bot, BotOnlineEvent _)
    {
        _logger.LogInformation("Login Success! Bot {id} online.", bot.BotUin);

        //if (_botOptions.Value.NotificationConfig.NotifyWebhookOnHeartbeat)
        //{
        //    await _httpClient.PostAsync(_botOptions.Value.NotificationConfig.WebhookUrl,
        //        new StringContent(@$"Time: `{@event.EventTime}`, Bot `{bot.BotUin}` online\."));
        //}
    }

    private void ProcessBotOffline(BotContext bot, BotOfflineEvent _)
    {
        _logger.LogError("Bot {id} offline.", bot.BotUin);

        //    if (!_botOptions.Value.NotificationConfig.NotifyWebhookOnHeartbeat) return;

        //    _logger.LogWarning("{option} set true, send HTTP POST to webhook.",
        //        nameof(_botOptions.Value.NotificationConfig.NotifyWebhookOnHeartbeat));
        //    await _httpClient.PostAsync(botOptions.Value.NotificationConfig.WebhookUrl,
        //        new StringContent(@$"Time: `{@event.EventTime}`, Bot `{bot.BotUin}` offline, msg: {@event.Message.Replace(".", @"\.")}\."));

        _appLifetime.StopApplication();
    }

    private async Task ProcessMessage(MessageChain message, BotContext thisBot)
    {
        var isGroup = message.GroupUin is not null;

        var replyUin = message.FriendUin;
        var allowedList = _botOptions.Value.AllowedFriends;
        var groupInfo = string.Empty;
        var builderBuilder = MessageBuilder.Friend;

        if (isGroup)
        {
            replyUin = message.GroupUin!.Value;
            allowedList = _botOptions.Value.AllowedGroups;
            groupInfo = $".Group({message.GroupUin})";
            builderBuilder = MessageBuilder.Group;
        }

        var scope = _logger.BeginScope($"{nameof(BotContext)}{groupInfo}@{message.FriendUin}");

        try
        {
            var textMessages = message
                .Select(m => m as TextEntity)
                .Where(m => m != null);

            if (!textMessages.Any(m => m!.Text.StartsWith("让我看看")))
                return;

            _logger.LogInformation("Received command `{msg}`.", message.ToPreviewString());

            var messageBuilder = builderBuilder.Invoke(replyUin);

            if (allowedList?.Contains(replyUin) ?? true)
            {
                _logger.LogInformation("Allowed user, send captured image.");
                await SendCaptureMessage(messageBuilder, thisBot);
            }
            else
            {
                _logger.LogWarning("UnAllowed user, reject.");
                await _botCtx.SendMessage(messageBuilder.Text("杰哥，你...你干嘛啊（用户不在白名单）").Build());
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process message.");

            if (!_botOptions.Value.NotificationConfig.NotifyAdminOnException)
                return;

            _logger.LogInformation("{opt} enabled, send error message to admin accounts.",
                nameof(BotOption.NotificationConfig.NotifyAdminOnException));
            var admins = _botOptions.Value.AdminAccounts;
            if (admins.Count == 0)
            {
                _logger.LogWarning("No admin accounts has been configured, can not send message.");
            }
            else
            {
                try
                {
                    await SendErrorToAccounts(admins, e);
                }
                catch (Exception sendError)
                {
                    _logger.LogError(sendError, "Unable to send message");
                }
            }
        }
        finally
        {
            scope?.Dispose();
        }
    }


    Task IHostedLifecycleService.StartingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("1. StartingAsync has been called.");

        return Task.CompletedTask;
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("2. StartAsync has been called.");

        _botCtx.Invoker.OnBotLogEvent += ProcessLog;

        _botCtx.Invoker.OnBotCaptchaEvent += ProcessCaptcha;

        _botCtx.Invoker.OnBotOnlineEvent += ProcessBotOnline;

        _botCtx.Invoker.OnBotOfflineEvent += ProcessBotOffline;

        _botCtx.Invoker.OnGroupMessageReceived +=
            async (bot, @event) => await ProcessMessage(@event.Chain, bot);

        _botCtx.Invoker.OnFriendMessageReceived +=
            async (bot, @event) => await ProcessMessage(@event.Chain, bot);

        return Task.CompletedTask;
    }

    async Task IHostedLifecycleService.StartedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("3. StartedAsync has been called.");

        await LoginAsync(cancellationToken);
    }

    private void OnStarted()
    {
        _logger.LogInformation("4. OnStarted has been called.");
    }

    private void OnStopping()
    {
        _logger.LogInformation("5. OnStopping has been called.");
    }

    Task IHostedLifecycleService.StoppingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("6. StoppingAsync has been called.");

        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("7. StopAsync has been called.");

        return Task.CompletedTask;
    }

    Task IHostedLifecycleService.StoppedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("8. StoppedAsync has been called.");

        return Task.CompletedTask;
    }

    private void OnStopped()
    {
        _logger.LogInformation("9. OnStopped has been called.");
    }
}
