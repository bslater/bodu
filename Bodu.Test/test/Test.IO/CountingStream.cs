// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A fully delegating stream wrapper that counts read operations and bytes read, while preserving the inner stream's
/// seek and position semantics. Useful for asserting how often a reader touches its source — for example, that a cache
/// layer eliminates repeat reads.
/// </summary>
/// <remarks>
/// Unlike <see cref="MonitoringStream" />, which deliberately rejects seeking so access patterns stay linear, this
/// wrapper delegates <see cref="Seek" /> and the <see cref="Position" /> setter to the inner stream, so it can stand
/// in for random-access sources.
/// </remarks>
public sealed class CountingStream
    : System.IO.Stream
{
    /// <summary>The wrapped stream.</summary>
    private readonly System.IO.Stream _inner;

    /// <summary>The number of read operations observed.</summary>
    private int _readCount;

    /// <summary>The total bytes returned by read operations.</summary>
    private long _bytesRead;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingStream" /> class.
    /// </summary>
    /// <param name="inner">The stream to wrap. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner" /> is <see langword="null" />.</exception>
    public CountingStream(System.IO.Stream inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
    }

    /// <summary>
    /// Gets the number of read operations observed so far.
    /// </summary>
    /// <value>The read-call count.</value>
    public int ReadCount => _readCount;

    /// <summary>
    /// Gets the total number of bytes returned by read operations so far.
    /// </summary>
    /// <value>The byte count.</value>
    public long BytesRead => _bytesRead;

    /// <inheritdoc />
    public override bool CanRead => _inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _inner.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => _inner.CanWrite;

    /// <inheritdoc />
    public override long Length => _inner.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    /// <inheritdoc />
    public override void Flush() =>
        _inner.Flush();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = _inner.Read(buffer, offset, count);
        _readCount++;
        _bytesRead += read;

        return read;
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        int read = _inner.Read(buffer);
        _readCount++;
        _bytesRead += read;

        return read;
    }

    /// <inheritdoc />
    public override long Seek(long offset, System.IO.SeekOrigin origin) =>
        _inner.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) =>
        _inner.SetLength(value);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) =>
        _inner.Write(buffer, offset, count);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();

        base.Dispose(disposing);
    }
}
