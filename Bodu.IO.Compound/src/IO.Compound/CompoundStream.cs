// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Provides read-only, seekable access to the materialized bytes of a single stream within a compound file.
/// </summary>
/// <remarks>
/// The stream is backed by an in-memory buffer that the owning <see cref="CompoundBinaryFile" /> assembled from the
/// underlying sector chain, so reads never touch the original source after the stream has been opened. The instance is
/// read-only: <see cref="Write(byte[], int, int)" /> and <see cref="SetLength(long)" /> always throw.
/// </remarks>
public sealed class CompoundStream
    : Stream
{
    /// <summary>The materialized stream payload.</summary>
    private readonly byte[] _buffer;

    /// <summary>The current read position within <see cref="_buffer" />.</summary>
    private int _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStream" /> class.
    /// </summary>
    /// <param name="name">The directory name of the stream.</param>
    /// <param name="buffer">The materialized stream payload, already trimmed to the declared size.</param>
    internal CompoundStream(string name, byte[] buffer)
    {
        Name = name;
        _buffer = buffer;
    }

    /// <summary>
    /// Gets the directory name of the stream.
    /// </summary>
    /// <returns>The stream name as stored in the compound-file directory.</returns>
    public string Name { get; }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _buffer.Length;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown on set when the value is negative.</exception>
    public override long Position
    {
        get => _position;
        set
        {
            ThrowHelper.ThrowIfNegative(value);
            _position = (int)Math.Min(value, _buffer.Length);
        }
    }

    /// <summary>
    /// Returns a read-only view over the entire stream payload without copying.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}" /> spanning the materialized bytes.</returns>
    public ReadOnlyMemory<byte> AsMemory() =>
        _buffer;

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfNull(buffer);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);

        int available = _buffer.Length - _position;
        if (available <= 0)
            return 0;

        int toCopy = Math.Min(available, count);
        Array.Copy(_buffer, _position, buffer, offset, toCopy);
        _position += toCopy;
        return toCopy;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        long target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _buffer.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        if (target < 0)
            throw new IOException();

        _position = (int)Math.Min(target, _buffer.Length);
        return _position;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        // No-op: the stream is read-only and fully buffered.
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown; the stream is read-only.</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown; the stream is read-only.</exception>
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
