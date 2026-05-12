// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisposalTrackingStream.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A writable <see cref="Stream" /> wrapper that records whether write and disposal operations
/// were performed synchronously or asynchronously. Use in tests that assert async extension
/// methods honour the correct async I/O and disposal contracts.
/// </summary>
/// <remarks>
/// <para>
/// Specifically tracks:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="SyncWriteCount" /> — number of calls to the synchronous <see cref="Write(byte[], int, int)" /> path.</description></item>
///   <item><description><see cref="AsyncWriteCount" /> — number of calls to the asynchronous <see cref="WriteAsync(Memory{byte}, CancellationToken)" /> path.</description></item>
///   <item><description><see cref="SyncDisposeCalled" /> — whether <see cref="Dispose" /> was called (via <c>Dispose(true)</c>).</description></item>
///   <item><description><see cref="AsyncDisposeCalled" /> — whether <see cref="DisposeAsync" /> was called.</description></item>
/// </list>
/// <para>
/// Reads and seeks are forwarded to the underlying stream without tracking, since the disposal
/// verification contract concerns write and cleanup paths only.
/// </para>
/// </remarks>
public sealed class DisposalTrackingStream : System.IO.Stream
{
    private readonly Stream _inner;

    /// <summary>
    /// Initialises a new instance of <see cref="DisposalTrackingStream" /> wrapping the supplied
    /// <paramref name="inner" /> stream.
    /// </summary>
    /// <param name="inner">The backing stream all I/O is delegated to. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
    public DisposalTrackingStream(Stream inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>Gets a value indicating whether <see cref="Dispose()" /> was invoked on this instance.</summary>
    public bool SyncDisposeCalled { get; private set; }

    /// <summary>Gets a value indicating whether <see cref="DisposeAsync()" /> was invoked on this instance.</summary>
    public bool AsyncDisposeCalled { get; private set; }

    /// <summary>Gets the number of times <see cref="Write(byte[], int, int)" /> was called.</summary>
    public int SyncWriteCount { get; private set; }

    /// <summary>Gets the number of times <see cref="WriteAsync(Memory{byte}, CancellationToken)" /> was called.</summary>
    public int AsyncWriteCount { get; private set; }

    /// <summary>Gets the content written to the underlying stream as a byte array.</summary>
    /// <returns>A byte array containing all data written to the backing stream.</returns>
    public byte[] ToArray() =>
        _inner is MemoryStream ms ? ms.ToArray() : throw new NotSupportedException("ToArray requires a MemoryStream backing store.");

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
    public override void Flush() => _inner.Flush();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _inner.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) =>
        _inner.Read(buffer, offset, count);

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _inner.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) =>
        _inner.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) =>
        _inner.SetLength(value);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        SyncWriteCount++;
        _inner.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        SyncWriteCount++;
        _inner.Write(buffer);
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        AsyncWriteCount++;
        return _inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        AsyncWriteCount++;
        return _inner.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            SyncDisposeCalled = true;

        _inner.Dispose();
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        AsyncDisposeCalled = true;
        await _inner.DisposeAsync().ConfigureAwait(false);
    }
}
