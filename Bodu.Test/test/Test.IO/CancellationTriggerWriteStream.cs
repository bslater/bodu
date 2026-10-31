// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellationTriggerWriteStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A readable, seekable and writable stream over an owned <see cref="MemoryStream" /> that cancels a supplied
/// <see cref="CancellationTokenSource" /> once a configurable number of bytes has been written to it, and from then on
/// honors the canceled token on every asynchronous write and flush, so a test can cancel a write part way through
/// without relying on timing.
/// </summary>
/// <remarks>
/// <para>
/// The write that carries <see cref="BytesWritten" /> to <see cref="CancelAfterBytes" /> completes, its bytes land, and
/// the source is canceled as it returns. Every later <see cref="WriteAsync(ReadOnlyMemory{byte}, CancellationToken)" />
/// or <see cref="FlushAsync" /> call whose token is canceled throws <see cref="OperationCanceledException" /> carrying
/// that token before touching the destination, as a destination that observes its token would; a call with a live
/// token, such as a retry with a fresh source, succeeds. Synchronous writes take no token, so they always land, and they
/// count toward the threshold too.
/// </para>
/// <para>
/// The stream may be seeded with existing content, so it can stand in for a file that is opened, read and then
/// rewritten in place. <see cref="ToArray" /> returns every byte the destination holds.
/// </para>
/// <para>
/// The read-side counterpart is <see cref="CancellationTriggerStream" />. This class is intended exclusively for test
/// harness use and must not appear in production code.
/// </para>
/// </remarks>
public sealed class CancellationTriggerWriteStream
    : Stream
{
    /// <summary>The owned destination that holds the bytes written so far.</summary>
    private readonly MemoryStream _inner;

    /// <summary>The source canceled once the threshold is reached.</summary>
    private readonly CancellationTokenSource _cts;

    /// <summary>The total number of bytes written so far.</summary>
    private long _bytesWritten;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancellationTriggerWriteStream" /> class over an empty destination.
    /// </summary>
    /// <param name="cts">The source to cancel once <paramref name="cancelAfterBytes" /> bytes have been written.</param>
    /// <param name="cancelAfterBytes">
    /// The number of bytes after which <paramref name="cts" /> is canceled. Must be non-negative; zero cancels the source
    /// on the first write, whatever its length.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="cts" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cancelAfterBytes" /> is negative.</exception>
    public CancellationTriggerWriteStream(CancellationTokenSource cts, long cancelAfterBytes)
    {
        if (cts is null)
            throw new ArgumentNullException(nameof(cts));
        if (cancelAfterBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(cancelAfterBytes), "cancelAfterBytes must be non-negative.");

        _inner = new MemoryStream();
        _cts = cts;
        CancelAfterBytes = cancelAfterBytes;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CancellationTriggerWriteStream" /> class over a destination seeded
    /// with existing content and positioned at its start.
    /// </summary>
    /// <param name="initialContent">The bytes the destination holds before anything is written to it.</param>
    /// <param name="cts">The source to cancel once <paramref name="cancelAfterBytes" /> bytes have been written.</param>
    /// <param name="cancelAfterBytes">
    /// The number of bytes after which <paramref name="cts" /> is canceled. Must be non-negative; zero cancels the source
    /// on the first write, whatever its length.
    /// </param>
    /// <remarks>
    /// The seeded bytes are not counted as written: <see cref="BytesWritten" /> starts at zero and the threshold applies
    /// to the writes the stream receives.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="initialContent" /> or <paramref name="cts" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cancelAfterBytes" /> is negative.</exception>
    public CancellationTriggerWriteStream(byte[] initialContent, CancellationTokenSource cts, long cancelAfterBytes)
        : this(cts, cancelAfterBytes)
    {
        if (initialContent is null)
            throw new ArgumentNullException(nameof(initialContent));

        _inner.Write(initialContent, 0, initialContent.Length);
        _inner.Position = 0;
    }

    /// <summary>
    /// Gets the number of written bytes at which the source is canceled.
    /// </summary>
    public long CancelAfterBytes { get; }

    /// <summary>
    /// Gets the total number of bytes written so far, by synchronous and asynchronous writes alike.
    /// </summary>
    public long BytesWritten => _bytesWritten;

    /// <summary>
    /// Gets a value indicating whether the stream has canceled its source.
    /// </summary>
    /// <value>
    /// <see langword="true" /> once a write has carried <see cref="BytesWritten" /> to <see cref="CancelAfterBytes" />;
    /// otherwise <see langword="false" />.
    /// </value>
    public bool HasTriggered { get; private set; }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length => _inner.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    /// <summary>
    /// Returns every byte the destination holds.
    /// </summary>
    /// <returns>The seeded content as overwritten or extended by the writes so far.</returns>
    public byte[] ToArray() =>
        _inner.ToArray();

    /// <inheritdoc />
    public override void Flush() =>
        _inner.Flush();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _inner.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) =>
        _inner.Read(buffer, offset, count);

    /// <inheritdoc />
    public override int Read(Span<byte> buffer) =>
        _inner.Read(buffer);

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _inner.ReadAsync(buffer, offset, count, cancellationToken);

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
        ValidateBufferArguments(buffer, offset, count);
        _inner.Write(buffer, offset, count);
        AfterWrite(count);
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _inner.Write(buffer);
        AfterWrite(buffer.Length);
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _inner.WriteAsync(buffer, cancellationToken);
        AfterWrite(buffer.Length);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>
    /// Counts the bytes a write has placed in the destination and cancels the source the first time the total reaches
    /// the threshold.
    /// </summary>
    /// <param name="count">The number of bytes the write placed.</param>
    private void AfterWrite(int count)
    {
        _bytesWritten += count;
        if (!HasTriggered && _bytesWritten >= CancelAfterBytes)
        {
            HasTriggered = true;
            _cts.Cancel();
        }
    }
}
