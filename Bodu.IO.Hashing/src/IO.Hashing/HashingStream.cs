// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Provides a pass-through <see cref="Stream" /> that feeds every byte read from or written to an inner stream into a
/// <see cref="NonCryptographicHashAlgorithm" />, so a digest can be computed while data is being transferred.
/// </summary>
/// <remarks>
/// <para>
/// The stream is direction-agnostic: <see cref="CanRead" /> and <see cref="CanWrite" /> delegate to the inner stream,
/// and bytes are appended to the algorithm whichever way they flow. Reads append only the bytes actually returned by
/// the inner stream; writes append after the inner stream has accepted the bytes.
/// </para>
/// <para>
/// Seeking is not supported (<see cref="CanSeek" /> is always <see langword="false" />): repositioning would let bytes
/// bypass or re-enter the digest, silently corrupting it.
/// </para>
/// <para>
/// <see cref="GetCurrentHash" /> reports the digest of the bytes transferred so far without finalizing the ongoing
/// computation; <see cref="GetHashAndReset" /> additionally resets the algorithm. The algorithm instance is supplied by
/// the caller, is not disposed by this stream, and — like all <see cref="NonCryptographicHashAlgorithm" /> instances —
/// is not thread-safe, so concurrent reads and writes through the same <see cref="HashingStream" /> are not supported.
/// </para>
/// </remarks>
public sealed class HashingStream
    : Stream
{
    /// <summary>
    /// The stream that bytes are transferred to or from.
    /// </summary>
    private readonly Stream _innerStream;

    /// <summary>
    /// The algorithm that accumulates every byte transferred through this stream.
    /// </summary>
    private readonly NonCryptographicHashAlgorithm _algorithm;

    /// <summary>
    /// Indicates whether the inner stream is left open when this stream is disposed.
    /// </summary>
    private readonly bool _leaveOpen;

    /// <summary>
    /// Indicates whether this stream has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashingStream" /> class over the specified inner stream and hash
    /// algorithm.
    /// </summary>
    /// <param name="innerStream">The stream to transfer bytes to or from. Must not be <see langword="null" />.</param>
    /// <param name="algorithm">
    /// The algorithm that accumulates the transferred bytes. Must not be <see langword="null" />.
    /// </param>
    /// <param name="leaveOpen">
    /// <see langword="true" /> to leave <paramref name="innerStream" /> open after this stream is disposed; otherwise,
    /// <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="innerStream" /> or <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public HashingStream(Stream innerStream, NonCryptographicHashAlgorithm algorithm, bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(innerStream);
        ThrowHelper.ThrowIfNull(algorithm);

        _innerStream = innerStream;
        _algorithm = algorithm;
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Gets the algorithm that accumulates the bytes transferred through this stream.
    /// </summary>
    /// <returns>The <see cref="NonCryptographicHashAlgorithm" /> supplied to the constructor.</returns>
    public NonCryptographicHashAlgorithm Algorithm =>
        _algorithm;

    /// <summary>
    /// Gets a value indicating whether the stream supports reading.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if this stream is not disposed and the inner stream supports reading; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public override bool CanRead =>
        !_disposed && _innerStream.CanRead;

    /// <summary>
    /// Gets a value indicating whether the stream supports writing.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if this stream is not disposed and the inner stream supports writing; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public override bool CanWrite =>
        !_disposed && _innerStream.CanWrite;

    /// <summary>
    /// Gets a value indicating whether the stream supports seeking.
    /// </summary>
    /// <returns>Always <see langword="false" />; repositioning would corrupt the digest.</returns>
    public override bool CanSeek =>
        false;

    /// <summary>
    /// Gets the length of the stream. Not supported.
    /// </summary>
    /// <returns>This property always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown; the stream does not support seeking.</exception>
    public override long Length =>
        throw new NotSupportedException(HashingResourceStrings.Op_NotSupported_HashingStreamNotSeekable);

    /// <summary>
    /// Gets or sets the position within the stream. Not supported.
    /// </summary>
    /// <returns>This property always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown; the stream does not support seeking.</exception>
    public override long Position
    {
        get => throw new NotSupportedException(HashingResourceStrings.Op_NotSupported_HashingStreamNotSeekable);
        set => throw new NotSupportedException(HashingResourceStrings.Op_NotSupported_HashingStreamNotSeekable);
    }

    /// <summary>
    /// Returns the digest of the bytes transferred so far without resetting the ongoing computation.
    /// </summary>
    /// <returns>The current digest, sized per the algorithm's hash length.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public byte[] GetCurrentHash()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _algorithm.GetCurrentHash();
    }

    /// <summary>
    /// Returns the digest of the bytes transferred so far and resets the algorithm to its initial state.
    /// </summary>
    /// <returns>The digest accumulated since construction or the previous reset.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public byte[] GetHashAndReset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _algorithm.GetHashAndReset();
    }

    /// <summary>
    /// Reads bytes from the inner stream into the buffer, appending the bytes actually read to the digest.
    /// </summary>
    /// <param name="buffer">The destination buffer. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based offset in <paramref name="buffer" /> at which to begin storing data.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The number of bytes read, or <c>0</c> at end of stream.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="offset" /> + <paramref name="count" /> exceeds the buffer length.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);

        return Read(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// Reads bytes from the inner stream into the span, appending the bytes actually read to the digest.
    /// </summary>
    /// <param name="buffer">The destination span.</param>
    /// <returns>The number of bytes read, or <c>0</c> at end of stream.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytesRead = _innerStream.Read(buffer);
        _algorithm.Append(buffer[..bytesRead]);
        return bytesRead;
    }

    /// <summary>
    /// Reads a single byte from the inner stream, appending it to the digest when one is available.
    /// </summary>
    /// <returns>The byte read, or <c>-1</c> at end of stream.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override int ReadByte()
    {
        Span<byte> single = stackalloc byte[1];

        return Read(single) == 0 ? -1 : single[0];
    }

    /// <summary>
    /// Asynchronously reads bytes from the inner stream into the buffer, appending the bytes actually read to the
    /// digest.
    /// </summary>
    /// <param name="buffer">The destination buffer. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based offset in <paramref name="buffer" /> at which to begin storing data.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task producing the number of bytes read, or <c>0</c> at end of stream.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="offset" /> + <paramref name="count" /> exceeds the buffer length.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);

        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <summary>
    /// Asynchronously reads bytes from the inner stream into the memory region, appending the bytes actually read to
    /// the digest.
    /// </summary>
    /// <param name="buffer">The destination memory region.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task producing the number of bytes read, or <c>0</c> at end of stream.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        _algorithm.Append(buffer.Span[..bytesRead]);
        return bytesRead;
    }

    /// <summary>
    /// Writes bytes to the inner stream and appends them to the digest.
    /// </summary>
    /// <param name="buffer">The source buffer. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based offset in <paramref name="buffer" /> at which to begin reading data.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="offset" /> + <paramref name="count" /> exceeds the buffer length.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);

        Write(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// Writes bytes to the inner stream and appends them to the digest.
    /// </summary>
    /// <param name="buffer">The source span.</param>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <remarks>
    /// Bytes are appended to the digest after the inner stream accepts the write, so a failed write does not
    /// contaminate the digest.
    /// </remarks>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _innerStream.Write(buffer);
        _algorithm.Append(buffer);
    }

    /// <summary>
    /// Writes a single byte to the inner stream and appends it to the digest.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override void WriteByte(byte value)
    {
        ReadOnlySpan<byte> single = [value];

        Write(single);
    }

    /// <summary>
    /// Asynchronously writes bytes to the inner stream and appends them to the digest.
    /// </summary>
    /// <param name="buffer">The source buffer. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based offset in <paramref name="buffer" /> at which to begin reading data.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous write.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="offset" /> + <paramref name="count" /> exceeds the buffer length.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);

        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <summary>
    /// Asynchronously writes bytes to the inner stream and appends them to the digest.
    /// </summary>
    /// <param name="buffer">The source memory region.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous write.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _innerStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        _algorithm.Append(buffer.Span);
    }

    /// <summary>
    /// Flushes the inner stream.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _innerStream.Flush();
    }

    /// <summary>
    /// Asynchronously flushes the inner stream.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous flush.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _innerStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Seeks within the stream. Not supported.
    /// </summary>
    /// <param name="offset">Ignored.</param>
    /// <param name="origin">Ignored.</param>
    /// <returns>This method always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown; the stream does not support seeking.</exception>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException(HashingResourceStrings.Op_NotSupported_HashingStreamNotSeekable);

    /// <summary>
    /// Sets the length of the stream. Not supported.
    /// </summary>
    /// <param name="value">Ignored.</param>
    /// <exception cref="NotSupportedException">Always thrown; the stream does not support seeking.</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException(HashingResourceStrings.Op_NotSupported_HashingStreamNotSeekable);

    /// <summary>
    /// Releases the stream, disposing the inner stream unless <c>leaveOpen</c> was specified. The algorithm is never
    /// disposed.
    /// </summary>
    /// <param name="disposing"><see langword="true" /> when called from <see cref="Stream.Dispose()" />.</param>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;

            if (disposing && !_leaveOpen)
                _innerStream.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Asynchronously releases the stream, disposing the inner stream unless <c>leaveOpen</c> was specified. The
    /// algorithm is never disposed.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose.</returns>
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;

            if (!_leaveOpen)
                await _innerStream.DisposeAsync().ConfigureAwait(false);
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
}
