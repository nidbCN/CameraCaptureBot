using Sodium;
using System.Text;
using CameraCaptureBot.Impl.Tencent.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CameraCaptureBot.Impl.Tencent.Utils.Sign;
public class SodiumSignProvider : ISignProvider
{
    #region Fields
    private readonly ILogger<SodiumSignProvider>? _logger;
    private readonly IOptions<TencentImplOption>? _options;

    private KeyPair? _key;
    #endregion

    #region Props

    public KeyPair Key
    {
        get
        {
            if (_key is null)
                GenerateKey();

            return _key!;
        }

        private set => _key = value;
    }

    #endregion

    #region Ctor
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
    #endregion

    #region Methods
    public void GenerateKey()
        => Key = PublicKeyAuth.GenerateKeyPair();

    public void GenerateKey(string seed)
    {
        var data = Encoding.ASCII.GetBytes(seed);
        GenerateKey(data);
    }

    public void GenerateKey(byte[] seed)
    => Key = PublicKeyAuth.GenerateKeyPair(seed);

    public byte[] Sign(string message)
        => Sign(Encoding.UTF8.GetBytes(message));

    public byte[] Sign(byte[] data)
        => PublicKeyAuth.SignDetached(data, Key.PrivateKey);
    #endregion

}
