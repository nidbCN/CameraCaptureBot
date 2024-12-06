using Sodium;
using System.Text;
using CameraCaptureBot.Impl.Tencent.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CameraCaptureBot.Impl.Tencent.Utils.Sign;
public class SodiumSignProvider : ISignProvider
{
    private readonly ILogger<SodiumSignProvider>? _logger;
    private readonly IOptions<TencentImplOption>? _options;

    private KeyPair? _key;

    public KeyPair Key
    {
        get
        {
            if (_key is null)
                GenerateKey();
            return _key!;
        }
    }

    public SodiumSignProvider() { }

    public SodiumSignProvider(ILogger<SodiumSignProvider> logger, IOptions<TencentImplOption> options)
    {
        _logger = logger;
        _options = options;

        var botSecret = _options.Value.AppSecret;

        if (botSecret.Length < PublicKeyAuth.SeedBytes)
        {
            botSecret += botSecret[..(PublicKeyAuth.SeedBytes - botSecret.Length)];
        }

        GenerateKey(botSecret);
    }

    public void GenerateKey()
        => _key = PublicKeyAuth.GenerateKeyPair();

    public void GenerateKey(string seed)
    {
        var data = Encoding.ASCII.GetBytes(seed);
        _key = PublicKeyAuth.GenerateKeyPair(data);
    }

    public void Sign(string message)
        => Sign(Encoding.UTF8.GetBytes(message));

    public void Sign(byte[] data)
        => PublicKeyAuth.SignDetached(data, Key.PrivateKey);
}
