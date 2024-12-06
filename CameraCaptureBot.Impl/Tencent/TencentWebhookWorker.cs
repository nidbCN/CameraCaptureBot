using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using CameraCaptureBot.Impl.Tencent.Options;
using CameraCaptureBot.Impl.Tencent.Utils.Sign;
using Microsoft.Extensions.Options;

namespace CameraCaptureBot.Impl.Tencent;

public class TencentWebhookWorker(
    ILogger<TencentWebhookWorker> logger,
    IOptions<TencentImplOption> options,
    ISignProvider signProvider) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    => Task.Run(() =>
        {
            using var listener = new HttpListener();

            var listen = options.Value.ListenIpAddress.MapToIPv4().ToString();
            var port = options.Value.ListenPort;
            var route = options.Value.Route;

            logger.LogInformation("Start webhook listen on `http://{ip}:{port}{route}.`", listen, port, route);

            if (options.Value.ListenIpAddress.Equals(IPAddress.Any))
            {
                listen = "+";
            }

            listener.Prefixes.Add($"http://{listen}:{port}{route}");

            listener.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                var ctx = listener.GetContext();



                using var resp = ctx.Response;
                resp.StatusCode = (int)HttpStatusCode.OK;
            }
        }, stoppingToken);

}
