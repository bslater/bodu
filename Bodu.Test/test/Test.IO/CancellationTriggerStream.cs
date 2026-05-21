// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellationTriggerStream.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A stream wrapper that cancels a <see cref="CancellationTokenSource" /> after a specified
/// number of successful reads, enabling deterministic cancellation testing without relying
/// on timing.
/// </summary>
public sealed class CancellationTriggerStream :
    System.IO.Stream
{
    private readonly Stream _inner;
    private readonly CancellationTokenSource _cts;
    private readonly int _cancelAfterRead;
    private int _readCount;

    /// <summary>
    /// Initialises a new instance of the <see cref="CancellationTriggerStream" /> class.
    /// </summary>
    /// <param name="inner">The underlying stream to read from.</param>
    /// <param name="cts">The cancellation token source to cancel after the specified number of reads.</param>
    /// <param name="cancelAfterRead">
    ///   The number of successful reads (returning &gt; 0 bytes) after which <paramref name="cts" />
    ///   is cancelled. The cancellation fires after the read completes, so subsequent calls to
    ///   <c>ReadAsync</c> detect the cancelled token before invoking the next synchronous read.
    /// </param>
    public CancellationTriggerStream(Stream inner, CancellationTokenSource cts, int cancelAfterRead)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cts = cts ?? throw new ArgumentNullException(nameof(cts));
        _cancelAfterRead = cancelAfterRead;
    }

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
    public override int Read(byte[] buffer, int offset, int count)
    {
        var result = _inner.Read(buffer, offset, count);
        if (result > 0 && ++_readCount == _cancelAfterRead && !_cts.IsCancellationRequested)
            _cts.Cancel();
        return result;
    }

    /// <inheritdoc />
    public override void Flush() => _inner.Flush();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) => _inner.SetLength(value);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();
        base.Dispose(disposing);
    }
}