using CameraCaptureBot.Core.Configs;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using System.IO.IsolatedStorage;
using System.Text.Json;

namespace CameraCaptureBot.Core;
internal sealed class StartUp
{
    internal static async Task LoginAsync(BotContext botCtx, IsolatedStorageFile isoStorage, ILogger logger, BotOption botOption, CancellationToken stoppingToken)
    {
        var loggedIn = false;

        var pwdLoginTimeoutTokenSrc = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            logger.LogInformation("Try login use password, timeout value: 5 sec.");
            loggedIn = await botCtx.LoginByPassword(pwdLoginTimeoutTokenSrc.Token);
        }
        catch (TaskCanceledException e)
        {
            logger.LogError(e, "Password login timeout, try QRCode.");
        }
        finally
        {
            pwdLoginTimeoutTokenSrc.Dispose();
        }

        if (!loggedIn)
        {
            logger.LogWarning("Password login failed, try QRCode.");

            var (url, _) = await botCtx.FetchQrCode()
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
                throw;
            }
        }

        // save device info and keystore
        try
        {
            await using var deviceInfoFileStream = isoStorage.OpenFile(botOption.DeviceInfoFile, FileMode.OpenOrCreate, FileAccess.Write);
            await JsonSerializer.SerializeAsync(deviceInfoFileStream, botCtx.UpdateDeviceInfo(), cancellationToken: stoppingToken);

            await using var keyFileStream = isoStorage.OpenFile(botOption.KeyStoreFile, FileMode.OpenOrCreate, FileAccess.Write);
            await JsonSerializer.SerializeAsync(keyFileStream, botCtx.UpdateKeystore(), cancellationToken: stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Save device info and key files failed.");
        }
        finally
        {
            isoStorage.Close();
        }
    }
}
