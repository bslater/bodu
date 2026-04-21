// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedLengthIncrementingStream.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A test stream that emits a predictable sequence of incrementing byte values up to a fixed length.
/// </summary>
public class FixedLengthIncrementingStream
    : System.IO.Stream
{
    private readonly int size;
    private int written;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedLengthIncrementingStream" /> class.
    /// </summary>
    /// <param name="size">The total number of bytes to emit.</param>
    public FixedLengthIncrementingStream(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");

        this.size = size;
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => written;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (written < 0)
            throw new ObjectDisposedException(nameof(FixedLengthIncrementingStream));

        if (written == size)
            return 0;

        int remaining = size - written;
        int localLimit = Math.Min(count, Math.Max(1, remaining / 2));

        for (int i = 0; i < localLimit; i++)
        {
            buffer[offset + i] = (byte)written;
            unchecked { written++; }
        }

        return localLimit;
    }

    /// <inheritdoc />
    public override void Flush()
    { }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        written = -1;
        base.Dispose(disposing);
    }
}