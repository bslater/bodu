// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FaultingWriteStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A writable stream over an owned <see cref="MemoryStream" /> that throws <see cref="IOException" /> from the write
/// that would carry the total number of bytes written past a configurable limit, and optionally from every flush,
/// simulating a destination that fails part way through a write, such as a full disk or a dropped connection.
/// </summary>
/// <remarks>
/// <para>
/// Every write whose bytes fit under <see cref="ThrowAfterBytes" /> succeeds. A write that would carry
/// <see cref="BytesWritten" /> past the limit throws before any of its bytes land, so the destination holds exactly the
/// bytes of the writes that preceded it, and every later write that would cross the limit throws the same way until
/// <see cref="Disarm" /> is called. With <see cref="ThrowOnFlush" /> set, <see cref="Flush" /> and <see cref="FlushAsync" /> throw instead of
/// flushing, again until the stream is disarmed. Disarming lets a test check that the code under test can retry over
/// the same destination.
/// </para>
/// <para>
/// By default the stream is readable, seekable and writable, and may be seeded with existing content, so it can stand
/// in for a file that is opened, read and then rewritten in place. In non-seekable mode it is a write-only sink: reads,
/// seeks, <see cref="Length" />, <see cref="Position" /> and <see cref="SetLength" /> throw
/// <see cref="NotSupportedException" />, so a consumer appends at the current position as it would to a pipe. In both
/// modes <see cref="ToArray" /> returns every byte the destination holds.
/// </para>
/// <para>
/// The read-side counterpart is <see cref="FaultingStream" />. This class is intended exclusively for test harness use
/// and must not appear in production code.
/// </para>
/// </remarks>
public sealed class FaultingWriteStream
    : Stream
{
    /// <summary>The owned destination that holds the bytes written so far.</summary>
    private readonly MemoryStream _inner;

    /// <summary>Whether the stream is readable and seekable, or a write-only sink.</summary>
    private readonly bool _seekable;

    /// <summary>Whether the configured faults are still raised; cleared by <see cref="Disarm" />.</summary>
    private bool _armed = true;

    /// <summary>The total number of bytes accepted by writes so far.</summary>
    private long _bytesWritten;

    /// <summary>The number of write calls received, the faulting ones included.</summary>
    private int _writeCount;

    /// <summary>The number of flush calls received, the faulting ones included.</summary>
    private int _flushCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="FaultingWriteStream" /> class over an empty destination.
    /// </summary>
    /// <param name="throwAfterBytes">
    /// The number of bytes that may be written successfully before a write faults. A write that would carry the total
    /// past it throws <see cref="IOException" /> before writing any of its bytes. Must be non-negative; pass
    /// <see cref="long.MaxValue" /> for a stream that faults only on flush.
    /// </param>
    /// <param name="throwOnFlush">
    /// <see langword="true" /> to make <see cref="Flush" /> and <see cref="FlushAsync" /> throw <see cref="IOException" />
    /// while the stream is armed; otherwise <see langword="false" />.
    /// </param>
    /// <param name="seekable">
    /// <see langword="true" /> for a readable, seekable destination; <see langword="false" /> for a write-only sink that
    /// reports <see cref="CanSeek" /> as <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="throwAfterBytes" /> is negative.</exception>
    public FaultingWriteStream(long throwAfterBytes, bool throwOnFlush = false, bool seekable = true)
    {
        if (throwAfterBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(throwAfterBytes), "throwAfterBytes must be non-negative.");

        _inner = new MemoryStream();
        _seekable = seekable;
        ThrowAfterBytes = throwAfterBytes;
        ThrowOnFlush = throwOnFlush;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FaultingWriteStream" /> class over a readable, seekable destination
    /// seeded with existing content and positioned at its start.
    /// </summary>
    /// <param name="initialContent">The bytes the destination holds before anything is written to it.</param>
    /// <param name="throwAfterBytes">
    /// The number of bytes that may be written successfully before a write faults. A write that would carry the total
    /// past it throws <see cref="IOException" /> before writing any of its bytes. Must be non-negative; pass
    /// <see cref="long.MaxValue" /> for a stream that faults only on flush.
    /// </param>
    /// <param name="throwOnFlush">
    /// <see langword="true" /> to make <see cref="Flush" /> and <see cref="FlushAsync" /> throw <see cref="IOException" />
    /// while the stream is armed; otherwise <see langword="false" />.
    /// </param>
    /// <remarks>
    /// The seeded bytes are not counted as written: <see cref="BytesWritten" /> and <see cref="WriteCount" /> start at
    /// zero and the fault limit applies to the writes the stream receives.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="initialContent" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="throwAfterBytes" /> is negative.</exception>
    public FaultingWriteStream(byte[] initialContent, long throwAfterBytes, bool throwOnFlush = false)
        : this(throwAfterBytes, throwOnFlush)
    {
        if (initialContent is null)
            throw new ArgumentNullException(nameof(initialContent));

        _inner.Write(initialContent, 0, initialContent.Length);
        _inner.Position = 0;
    }

    /// <summary>
    /// Gets the number of bytes that may be written successfully before a write faults.
    /// </summary>
    /// <value>The write limit; a write that would carry <see cref="BytesWritten" /> past it throws while armed.</value>
    public long ThrowAfterBytes { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Flush" /> and <see cref="FlushAsync" /> throw while the stream is armed.
    /// </summary>
    /// <value><see langword="true" /> when flushes fault; otherwise <see langword="false" />.</value>
    public bool ThrowOnFlush { get; }

    /// <summary>
    /// Gets a value indicating whether the configured faults are still raised.
    /// </summary>
    /// <value><see langword="true" /> until <see cref="Disarm" /> is called; afterwards <see langword="false" />.</value>
    public bool IsArmed => _armed;

    /// <summary>
    /// Gets the total number of bytes the writes so far have placed in the destination.
    /// </summary>
    /// <value>The bytes accepted; a faulting write adds nothing.</value>
    public long BytesWritten => _bytesWritten;

    /// <summary>
    /// Gets the number of write calls the stream has received, the faulting ones included.
    /// </summary>
    public int WriteCount => _writeCount;

    /// <summary>
    /// Gets the number of flush calls the stream has received, the faulting ones included.
    /// </summary>
    public int FlushCount => _flushCount;

    /// <inheritdoc />
    public override bool CanRead => _seekable;

    /// <inheritdoc />
    public override bool CanSeek => _seekable;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            ThrowIfNotSeekable();
            return _inner.Length;
        }
    }

    /// <inheritdoc />
    public override long Position
    {
        get
        {
            ThrowIfNotSeekable();
            return _inner.Position;
        }

        set
        {
            ThrowIfNotSeekable();
            _inner.Position = value;
        }
    }

    /// <summary>
    /// Stops the stream from faulting, so that every later write and flush succeeds.
    /// </summary>
    /// <remarks>
    /// The counters keep counting, and the bytes already written stay where they are, so a test can check what a retry
    /// over the same destination produces.
    /// </remarks>
    public void Disarm() =>
        _armed = false;

    /// <summary>
    /// Returns every byte the destination holds.
    /// </summary>
    /// <returns>The seeded content as overwritten or extended by the writes so far.</returns>
    public byte[] ToArray() =>
        _inner.ToArray();

    /// <inheritdoc />
    public override void Flush()
    {
        BeforeFlush();
        _inner.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        BeforeFlush();
        return _inner.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfNotSeekable();
        return _inner.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ThrowIfNotSeekable();
        return _inner.Read(buffer);
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfNotSeekable();
        return _inner.ReadAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfNotSeekable();
        return _inner.ReadAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfNotSeekable();
        return _inner.Seek(offset, origin);
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        ThrowIfNotSeekable();
        _inner.SetLength(value);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        BeforeWrite(count);
        _inner.Write(buffer, offset, count);
        _bytesWritten += count;
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        BeforeWrite(buffer.Length);
        _inner.Write(buffer);
        _bytesWritten += buffer.Length;
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
        BeforeWrite(buffer.Length);
        await _inner.WriteAsync(buffer, cancellationToken);
        _bytesWritten += buffer.Length;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>
    /// Counts a write call and throws when it would carry the total past the limit while the stream is armed.
    /// </summary>
    /// <param name="count">The number of bytes the write carries.</param>
    /// <exception cref="IOException">
    /// The write would carry <see cref="BytesWritten" /> past <see cref="ThrowAfterBytes" />.
    /// </exception>
    private void BeforeWrite(int count)
    {
        _writeCount++;
        if (_armed && _bytesWritten + count > ThrowAfterBytes)
        {
            throw new IOException(
                $"Simulated write fault: {count} bytes would carry the total past {ThrowAfterBytes} " +
                $"({_bytesWritten} bytes written so far).");
        }
    }

    /// <summary>
    /// Counts a flush call and throws when flushes are configured to fault while the stream is armed.
    /// </summary>
    /// <exception cref="IOException"><see cref="ThrowOnFlush" /> is set and the stream is armed.</exception>
    private void BeforeFlush()
    {
        _flushCount++;
        if (_armed && ThrowOnFlush)
            throw new IOException($"Simulated flush fault ({_bytesWritten} bytes written so far).");
    }

    /// <summary>
    /// Throws when the stream is a write-only sink and a member that needs a readable, seekable destination is called.
    /// </summary>
    /// <exception cref="NotSupportedException">The stream was created in non-seekable mode.</exception>
    private void ThrowIfNotSeekable()
    {
        if (!_seekable)
            throw new NotSupportedException("The stream is a write-only, non-seekable sink.");
    }
}
