// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringStream.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A stream wrapper that monitors and records all read operations performed on the innerStream stream. Useful for
/// verifying stream access patterns during asynchronous hash computations.
/// </summary>
public sealed class MonitoringStream
    : System.IO.Stream
{
    private readonly Stream _inner;
    private readonly List<(long Offset, int Count)> reads = new();
    private long position;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoringStream" /> class.
    /// </summary>
    /// <param name="inner">The inner stream to monitor.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner" /> is null.</exception>
    public MonitoringStream(Stream inner)
    {
        this._inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public override bool CanRead => this._inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => this._inner.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => this._inner.CanWrite;

    /// <inheritdoc />
    public override long Length => this._inner.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => this.position;
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets a read-only list of all read operations, each represented by the starting offset and number of bytes read.
    /// </summary>
    public IReadOnlyList<(long Offset, int Count)> Reads => reads;

    /// <inheritdoc />
    public override void Flush() => this._inner.Flush();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = this._inner.Read(buffer, offset, count);
        if (bytesRead > 0)
        {
            this.reads.Add((this.position, bytesRead));
            this.position += bytesRead;
        }
        return bytesRead;
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await this._inner.ReadAsync(buffer, offset, count, cancellationToken);
        if (bytesRead > 0)
        {
            this.reads.Add((this.position, bytesRead));
            this.position += bytesRead;
        }
        return bytesRead;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>
    /// Returns the contents of the underlying stream as a byte array.
    /// </summary>
    /// <remarks>
    /// If the underlying stream supports seeking, this method reads the entire content without affecting the current
    /// position. If not, a <see cref="NotSupportedException" /> is thrown.
    /// </remarks>
    /// <returns>A byte array containing the full content of the inner stream.</returns>
    /// <exception cref="NotSupportedException">Thrown if the inner stream does not support seeking.</exception>
    public byte[] ToArray()
    {
        if (!_inner.CanSeek)
            throw new NotSupportedException("The inner _inner must support seeking to use ToArray().");

        var originalPosition = _inner.Position;

        try
        {
            _inner.Position = 0;
            using var memory = new MemoryStream();
            _inner.CopyTo(memory);
            return memory.ToArray();
        }
        finally
        {
            _inner.Position = originalPosition;
        }
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        this._inner.Write(buffer, offset, count);
        this.position += count;
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        this.position += count;
        return this._inner.WriteAsync(buffer, offset, count, cancellationToken);
    }
}