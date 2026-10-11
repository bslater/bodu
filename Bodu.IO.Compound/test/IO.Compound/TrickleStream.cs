// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrickleStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Provides a read-only, non-seekable stream over a byte array that returns at most a fixed number of bytes from each
/// read, as a network or pipe source may.
/// </summary>
/// <remarks>
/// A consumer that assumes one read fills its buffer reads too little from this stream, so it tests that a reader loops
/// until it has the bytes it asked for.
/// </remarks>
internal sealed class TrickleStream : Stream
{
    /// <summary>The bytes the stream returns.</summary>
    private readonly byte[] _data;

    /// <summary>The largest number of bytes one read returns.</summary>
    private readonly int _bytesPerRead;

    /// <summary>The offset of the next byte to return.</summary>
    private int _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickleStream" /> class.
    /// </summary>
    /// <param name="data">The bytes the stream returns.</param>
    /// <param name="bytesPerRead">The largest number of bytes one read returns.</param>
    public TrickleStream(byte[] data, int bytesPerRead)
    {
        _data = data;
        _bytesPerRead = bytesPerRead;
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
    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        int count = Math.Min(Math.Min(buffer.Length, _bytesPerRead), _data.Length - _position);
        _data.AsSpan(_position, count).CopyTo(buffer);
        _position += count;
        return count;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void Flush()
    {
    }
}
