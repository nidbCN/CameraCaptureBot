using System.Buffers;

namespace VideoStreamCaptureBot.Core.Codecs;

public class CodecResultStream : Stream
{
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    // default 1kb+(128x512b) capacity without copy array reference.
    private readonly List<byte[]> _data = new(128);

    public CodecResultStream() : this(1024)
    {

    }

    public CodecResultStream(int capacity)
    {
        Capacity = capacity;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => true;

    private int _length = 0;
    public override long Length => _length;

    private int _capacity = 0;
    private const int _blockSize = 1024;

    public int Capacity
    {
        get => _capacity;

        set
        {
            if (value < Length)
                throw new ArgumentOutOfRangeException(nameof(value));

            // nothing to do
            if (value == _capacity) return;

            if (value > _capacity)
            {
                var blockNum = ((value - _capacity) & 1023) + 1;

                for (var i = 0; i < blockNum; i++)
                {
                    var array = _pool.Rent(_blockSize);
                    _data.Add(array);
                }

                _capacity += blockNum * _blockSize;
            }
            else
            {
                var blockNum = ((value - _capacity) & 1023) - 1;
                for (var i = blockNum; i >= 0; i++)
                {
                    var array = _data[i];
                    _data.RemoveAt(i);
                    _pool.Return(array);
                }

                _capacity -= blockNum * _blockSize;
            }
        }
    }

    private int _position;

    public override long Position
    {
        get => _position;
        set
        {
            if (value <= Capacity)
                _position = (int)value;
            else
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }

    public override void Flush()
    {

    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        // bytes in stream to read
        var n = (int)(Length - Position);
        if (n > count)
            n = count;
        if (n <= 0) return 0;

        // When there is very limited data, copy directly
        if (n <= 8)
        {
            for (var i = 0; i < n; i++)
                buffer[offset + i] = this[(int)Position + i];
        }
        else
        {
            var copied = 0;

            while (copied + _blockSize < n)
                copied += BlockCopyTo((int)Position + copied, buffer, offset + copied);

            var posInSrc = (int)Position + copied;
            var posInBlock = posInSrc & (_blockSize - 1);

            Buffer.BlockCopy(
                _data[(posInSrc) >> 9], posInBlock,
                buffer, offset + copied, _blockSize - posInBlock);
        }

        Position += n;

        return n;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return SeekCore(offset, origin switch
        {
            SeekOrigin.Begin => 0,
            SeekOrigin.Current => _position,
            SeekOrigin.End => _length,
            _ => throw new ArgumentException(nameof(origin))
        });
    }

    private long SeekCore(long offset, int loc)
    {
        Position = loc + (int)offset;
        return Position;
    }

    public override void SetLength(long value)
    {
        if (value is < 0 or > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value));

        var newLength = (int)value;

        if (newLength > Capacity)
        {
            Capacity = newLength;
        }

        _length = newLength;
        if (Position > newLength)
            Position = newLength;
    }


    private byte this[int index]
    {
        get => _data[index >> 9][index & (_blockSize - 1)];
        set => _data[index >> 9][index & (_blockSize - 1)] = value;
    }

    private int BlockCopyTo(int offset, byte[] dest, int destOffset)
    {
        var block = _data[offset >> 9];
        var posInBlock = offset & (_blockSize - 1);
        Buffer.BlockCopy(block, posInBlock, dest, destOffset, block.Length - posInBlock);
        return block.Length - posInBlock;
    }

    private int BlockCopyFrom(byte[] src, int srcOffset, int offset)
    {
        var block = _data[offset >> 9];
        var posInBlock = offset & (_blockSize - 1);
        Buffer.BlockCopy(src, srcOffset, block, posInBlock, _blockSize - posInBlock);
        return block.Length - posInBlock;
    }

    private int BlockFill(byte value, int offset, int count = -1)
    {
        var block = _data[offset >> 9];
        var posInBlock = offset & (_blockSize - 1);

        if (count == -1)
        {
            count = _blockSize - posInBlock;
        }

        Array.Fill(block, value, posInBlock, count);
        return count;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        var endIndex = (int)Position + count;
        // Check for overflow
        ArgumentNullException.ThrowIfNull(endIndex);

        if (endIndex > Length)
        {
            // expand to endIndex
            if (endIndex > Capacity)
            {
                Capacity = endIndex;
            }

            _length = endIndex;
        }

        if (count <= 8)
        {
            for (var i = 0; i < count; i++)
                this[(int)Position + i] = buffer[offset + i];
        }
        else
        {
            var copied = 0;

            while (count - copied > _blockSize)
                copied += BlockCopyFrom(buffer, offset + copied, (int)Position + copied);

            var posInData = (int)Position + copied;
            var posInBlock = posInData & (_blockSize - 1);

            Buffer.BlockCopy(
                buffer, offset + copied,
                _data[posInData >> 9], posInBlock,
                count - copied);
        }

        Position = endIndex;
    }

    protected override void Dispose(bool disposing)
    {
        _length = 0;
        Capacity = 0;

        base.Dispose(disposing);
    }
}