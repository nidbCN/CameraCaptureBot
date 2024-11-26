using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CameraCaptureBot.Core.Configs;
using CameraCaptureBot.Core.Services;
using CameraCaptureBot.Core.Utils;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Options;
using BotLogLevel = Lagrange.Core.Event.EventArg.LogLevel;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CameraCaptureBot.Core;
internal class BotHost(
    ILogger<BotHost> logger,
    IHostApplicationLifetime appLifetime,
    IServiceProvider services,
    IOptions<BotOption> botOptions,
    BotContext botCtx,
    IsolatedStorageFile isoStorage)
    : IHostedLifecycleService
{
    private async Task LoginAsync(CancellationToken stoppingToken = new())
    {
        var loggedIn = false;
        var keyStore = botCtx.UpdateKeystore();

        // password Login
        if (keyStore.Uin != 0 &&
            (keyStore.Session.TempPassword is not null || !string.IsNullOrEmpty(keyStore.PasswordMd5)))
        {
            using var pwdLoginTimeoutTokenSrc = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            using var pwdLoginStoppingTokenSrc = CancellationTokenSource
                .CreateLinkedTokenSource(stoppingToken, pwdLoginTimeoutTokenSrc.Token);

            botCtx.Invoker.OnBotCaptchaEvent += async (bot, @event) =>
            {
                logger.LogWarning("Need user finish captcha, url: {msg}", @event.Url);
                logger.LogInformation("Open the link and dev-tools in browser, finish captcha, "
                                      + "copy the response of `cap_union_new_verify` "
                                      + "and paste response json to stdin.");
                logger.LogInformation("Press Enter and Ctrl-D(send EOF) to finish input and continue login.");
                
                await using var inputStream = Console.OpenStandardInput();

                CaptchaResult? inputCaptchaResult;

                try
                {
                    inputCaptchaResult = await JsonSerializer.DeserializeAsync<CaptchaResult>(inputStream, cancellationToken: stoppingToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Deserialize failed!");
                    throw;
                }

                await Task.Run(() =>
                {
                    if (inputCaptchaResult is null)
                    {
                        logger.LogError("Deserialize failed, result is null.");
                    }
                    else
                    {
                        logger.LogInformation("Receive captcha, ticket {t}, rand-str {s}", inputCaptchaResult.Ticket, inputCaptchaResult.RandomString);
                        bot.SubmitCaptcha(inputCaptchaResult.Ticket, inputCaptchaResult.RandomString);
                    }
                }, stoppingToken);
            };

            try
            {
                logger.LogInformation("Try login use password, timeout value: 2 min.");
                loggedIn = await botCtx.LoginByPassword(pwdLoginStoppingTokenSrc.Token);

                if (!loggedIn)
                    logger.LogWarning("Password login failed, try QRCode.");
            }
            catch (TaskCanceledException e)
            {
                logger.LogError(e, "Password login timeout, try QRCode.");
            }
        }

        // password login success
        if (loggedIn) return;

        // QRCode login
        logger.LogInformation("Try login use QRCode, timeout value: 2 min.");

        var (url, _) = await botCtx.FetchQrCode()
                       ?? throw new ApplicationException(message: "Fetch QRCode failed.\n");

        // The QrCode will be expired in 2 minutes.
        using var qrLoginTimeoutTokenSrc = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var link = new UriBuilder("https://util-functions.azurewebsites.net/api/QrCode")
        {
            Query = await new FormUrlEncodedContent(
                new Dictionary<string, string> {
                        {"content", url}
                }).ReadAsStringAsync(stoppingToken)
        };

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Open link `{link}` and scan the QRCode to login.", link.Uri.ToString());
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
            await botCtx.LoginByQrCode(qrLoginStoppingTokenSrc.Token);
        }
        catch (TaskCanceledException e)
        {
            logger.LogError(e, "QRCode login timeout, can't boot.");
            appLifetime.StopApplication();
        }
    }

    private async Task SendCaptureMessage(MessageBuilder message, BotContext thisBot)
    {
        var captureService = services.GetRequiredService<CaptureService>();

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
        => await Parallel.ForEachAsync(accounts, async (account, _) =>
        {
            var msg = MessageBuilder
                .Friend(account)
                .Text(e.Message + '\n' + e.StackTrace)
                .Build();
            await botCtx.SendMessage(msg);
        });

    private void ProcessLog(BotContext bot, BotLogEvent @event)
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
    }

    private record CaptchaResult
    {
        [JsonPropertyName("ticket")]
        public required string Ticket { get; init; }

        // ReSharper disable once StringLiteralTypo
        [JsonPropertyName("randstr")]
        public required string RandomString { get; init; }
    }

    private async Task ProcessBotOnline(BotContext bot, BotOnlineEvent _)
    {
        logger.LogInformation("Login Success! Bot {id} online.", bot.BotUin);

        // save device info and keystore
        try
        {
            await using var deviceInfoFileStream = isoStorage.OpenFile(botOptions.Value.DeviceInfoFile, FileMode.OpenOrCreate, FileAccess.Write);
            await JsonSerializer.SerializeAsync(deviceInfoFileStream, botCtx.UpdateDeviceInfo());

            var keyStore = botCtx.UpdateKeystore();

            // update password hash
            if (string.IsNullOrEmpty(keyStore.PasswordMd5))
            {
                if (botOptions.Value.AccountPasswords?.TryGetValue(keyStore.Uin, out var pwd) ?? false)
                {
                    if (pwd.Hashed)
                    {
                        keyStore.PasswordMd5 = pwd.Password;
                    }
                    else
                    {
                        var hashData = MD5.HashData(Encoding.UTF8.GetBytes(pwd.Password));
                        var buffer = new char[hashData.Length * 2];
                        for (var i = 0; i < hashData.Length; i++)
                        {
                            var hex = ByteHex.ByteToHex(hashData[i], ByteHex.HexCasing.LowerCase);
                            buffer[2 * i] = hex.High;
                            buffer[2 * i + 1] = hex.Low;
                        }

                        keyStore.PasswordMd5 = new(buffer);
                    }
                }
            }

            await using var keyFileStream = isoStorage.OpenFile(botOptions.Value.KeyStoreFile, FileMode.OpenOrCreate, FileAccess.Write);
            await JsonSerializer.SerializeAsync(keyFileStream, keyStore);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Save device info and key files failed.");
        }
        finally
        {
            isoStorage.Close();
        }

        //if (_botOptions.Value.NotificationConfig.NotifyWebhookOnHeartbeat)
        //{
        //    await _httpClient.PostAsync(_botOptions.Value.NotificationConfig.WebhookUrl,
        //        new StringContent(@$"Time: `{@event.EventTime}`, Bot `{bot.BotUin}` online\."));
        //}
    }

    private void ProcessBotOffline(BotContext bot, BotOfflineEvent _)
    {
        logger.LogError("Bot {id} offline.", bot.BotUin);

        //    if (!_botOptions.Value.NotificationConfig.NotifyWebhookOnHeartbeat) return;

        //    logger.LogWarning("{option} set true, send HTTP POST to webhook.",
        //        nameof(_botOptions.Value.NotificationConfig.NotifyWebhookOnHeartbeat));
        //    await _httpClient.PostAsync(botOptions.Value.NotificationConfig.WebhookUrl,
        //        new StringContent(@$"Time: `{@event.EventTime}`, Bot `{bot.BotUin}` offline, msg: {@event.Message.Replace(".", @"\.")}\."));

        appLifetime.StopApplication();
    }

    private async Task ProcessMessage(MessageChain message, BotContext thisBot)
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

            if (!botOptions.Value.NotificationConfig.NotifyAdminOnException)
                return;

            logger.LogInformation("{opt} enabled, send error message to admin accounts.",
                nameof(BotOption.NotificationConfig.NotifyAdminOnException));
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

    Task IHostedLifecycleService.StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        botCtx.Invoker.OnBotLogEvent += ProcessLog;
        botCtx.Invoker.OnBotOnlineEvent +=
            async (bot, @event) => await ProcessBotOnline(bot, @event);
        botCtx.Invoker.OnBotOfflineEvent += ProcessBotOffline;
        botCtx.Invoker.OnGroupMessageReceived +=
            async (bot, @event) => await ProcessMessage(@event.Chain, bot);
        botCtx.Invoker.OnFriendMessageReceived +=
            async (bot, @event) => await ProcessMessage(@event.Chain, bot);

        return Task.CompletedTask;
    }

    async Task IHostedLifecycleService.StartedAsync(CancellationToken cancellationToken)
    {
        await LoginAsync(cancellationToken);
    }

    Task IHostedLifecycleService.StoppingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        botCtx.Dispose();
        return Task.CompletedTask;
    }

    Task IHostedLifecycleService.StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
