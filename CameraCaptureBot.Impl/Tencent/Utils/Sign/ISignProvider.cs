namespace CameraCaptureBot.Impl.Tencent.Utils.Sign;

public interface ISignProvider
{
    void GenerateKey();
    void GenerateKey(string seed);

    void Sign(string message);
    void Sign(byte[] data);
}
