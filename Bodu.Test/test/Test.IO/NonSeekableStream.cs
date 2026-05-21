// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonSeekableStream.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A read-only, non-seekable stream wrapper that forwards reads to an inner
/// <see cref="MemoryStream" /> while explicitly disabling all seek operations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CanSeek" /> returns <see langword="false" /> and both <see cref="Position" /> and
/// <see cref="Length" /> throw <see cref="NotSupportedException" />, matching the contract of
/// streams such as <see cref="System.Net.Sockets.NetworkStream" /> or a raw pipe endpoint. Any attempt to call
/// <see cref="Seek" /> or set <see cref="Position" /> also throws.
/// </para>
/// <para>
/// Use this stream in tests that verify a consumer never calls seek-related members on its
/// input stream — confirming compatibility with sources that can only be read sequentially.
/// </para>
/// <para>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </para>
/// </remarks>
public sealed class NonSeekableStream 
    : System.IO.Stream
{
    private readonly MemoryStream inner;

    /// <summary>
    /// Initialises a new instance of <see cref="NonSeekableStream" /> backed by
    /// <paramref name="data" />.
    /// </summary>
    /// <param name="data">The byte array to expose as a forward-only readable stream.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
    public NonSeekableStream(byte[] data)
    {
        this.inner = new MemoryStream(
            data ?? throw new ArgumentNullException(nameof(data)),
            writable: false);
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown — the stream is not seekable.</exception>
    public override long Length => throw new NotSupportedException("Stream is not seekable.");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown — the stream is not seekable.</exception>
    public override long Position
    {
        get => throw new NotSupportedException("Stream is not seekable.");
        set => throw new NotSupportedException("Stream is not seekable.");
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => this.inner.Read(buffer, offset, count);

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => this.inner.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown — the stream is not seekable.</exception>
    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException("Stream is not seekable.");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown — the stream is not writable.</exception>
    public override void SetLength(long value)
        => throw new NotSupportedException("Stream is not writable.");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown — the stream is not writable.</exception>
    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Stream is not writable.");

    /// <inheritdoc />
    public override void Flush() { }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            this.inner.Dispose();

        base.Dispose(disposing);
    }
}