// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IncrementingByteStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A stream that produces predictable pseudo-random data based on position. Used for testing hashing over streaming
/// input.
/// </summary>
public class IncrementingByteStream
    : System.IO.Stream
{
    private readonly int _size;
    private int _remaining; // Number of unread bytes left to emit
    private byte _written;  // Tracks current byte hashValue to write into the buffer

    /// <summary>
    /// Initializes a new instance of the <see cref="IncrementingByteStream" /> class with the specified byte length.
    /// </summary>
    /// <param name="size">Total number of bytes to generate before reaching end of stream.</param>
    public IncrementingByteStream(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");

        this._size = size;
        _remaining = size;
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
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Flush()
    { }

    /// <summary>
    /// Fills the buffer with sequential bytes, pausing halfway through remaining data if possible.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="offset">The offset in the buffer to begin writing.</param>
    /// <param name="count">The number of bytes requested by the caller.</param>
    /// <returns>The number of bytes actually read.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_remaining < 0) throw new ObjectDisposedException(nameof(IncrementingByteStream));

        if (_remaining == 0)
            return 0;

        // Read no more than half of remaining, or requested count, whichever is smaller
        int localLimit = _remaining / 2;
        if (localLimit == 0 || localLimit > count)
            localLimit = Math.Min(_remaining, count);

        // Fill the buffer with incrementing byte values
        for (int i = 0; i < localLimit; i++)
        {
            buffer[offset + i] = _written++;
        }

        _remaining -= localLimit;
        return localLimit;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>
    /// Generates the full byte sequence from the beginning and returns it as an array.
    /// </summary>
    public byte[] ToArray()
    {
        byte[] result = new byte[_size];
        byte val = 0;
        for (int i = 0; i < _size; i++)
        {
            result[i] = val++;
        }

        return result;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _remaining = -1; // Mark stream as unusable
        base.Dispose(disposing);
    }
}
