namespace VideoStreamCaptureBot.Impl.Tencent.Utils.Sign;

public interface ISignProvider
{
    public void GenerateKey();
    public void GenerateKey(string seed);
    public void GenerateKey(byte[] seed);

    public byte[] Sign(string message);
    public byte[] Sign(byte[] data);
}
