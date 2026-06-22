// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Provides read-only, seekable access to the bytes of a single stream within a compound file.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/io-compound-stream-access.svg" alt="CompoundStreamEntry.Open returns a CompoundStream, a read-only seekable Stream cursor whose CanWrite is false. When the owning CompoundFile was opened buffered (the default), the stream's whole payload is materialized into an in-memory byte array at open time and Read, Seek, and AsMemory work over that array. When the file was opened with buffered false, the cursor walks the stream's sector chain in the source on demand: each Read locates the sector for the current Position, copies only the bytes within that sector, and advances, so the full payload is never held in memory. AsMemory materializes the whole chain on request."/>
/// </para>
/// <para>
/// <see cref="CompoundStream" /> is a standard <see cref="Stream" /> cursor obtained from
/// <see cref="CompoundStreamEntry.Open" />, so it composes with the BCL surfaces that consume a <see cref="Stream" />
/// — <see cref="System.IO.StreamReader" />, <see cref="System.IO.BinaryReader" />, <see cref="Stream.CopyTo(Stream)" />,
/// and the deserializers built on top of them.
/// </para>
/// <para>
/// A stream opened from a buffered compound file is backed by an in-memory payload assembled at open time. A stream
/// opened from a streaming compound file (see
/// <see cref="CompoundFile.Open(System.IO.Stream, CompoundFileMode, bool, bool)" /> with <c>buffered: false</c>) reads
/// its sectors on demand from the underlying source, so it never materializes the whole payload. In the streaming case
/// the owning <see cref="CompoundFile" /> and its source must remain open for the lifetime of the cursor.
/// </para>
/// <para>
/// The instance is read-only: <see cref="CanWrite" /> is always <see langword="false" /> and both
/// <see cref="Write(byte[], int, int)" /> and <see cref="SetLength(long)" /> always throw
/// <see cref="NotSupportedException" />. Because reads against a streaming source advance a shared position, a single
/// cursor is not safe for concurrent use; open one cursor per reader.
/// </para>
/// </remarks>
/// <example>
/// The following example opens a compound file, navigates to a named stream, and reads its leading bytes through the
/// cursor.
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Compound;
///
/// using CompoundFile file = CompoundFile.Open(File.OpenRead("book.xls"));
/// CompoundStreamEntry entry = file.RootStorage.OpenStream("Workbook");
///
/// using CompoundStream stream = entry.Open();
/// byte[] signature = new byte[8];
/// stream.ReadExactly(signature);
///
/// // Seek back to the start and hand the cursor to any Stream consumer.
/// stream.Seek(0, SeekOrigin.Begin);
/// using var reader = new BinaryReader(stream);
/// ushort recordType = reader.ReadUInt16();
///]]>
/// </code>
/// </example>
public sealed class CompoundStream
    : Stream
{
    /// <summary>The materialized payload, or <see langword="null" /> when the stream reads on demand.</summary>
    private readonly byte[]? _buffer;

    /// <summary>The sector reader used for on-demand reads, or <see langword="null" /> when buffered.</summary>
    private readonly CompoundSectorReader? _sectors;

    /// <summary>The ordered sector chain for on-demand reads, or <see langword="null" /> when buffered.</summary>
    private readonly uint[]? _chain;

    /// <summary>The declared payload length.</summary>
    private readonly long _length;

    /// <summary>The regular sector size, in bytes, used for on-demand reads.</summary>
    private readonly int _sectorSize;

    /// <summary>The current read position within the payload.</summary>
    private long _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStream" /> class over a materialized payload.
    /// </summary>
    /// <param name="name">The directory name of the stream.</param>
    /// <param name="buffer">The materialized stream payload, already trimmed to the declared size.</param>
    internal CompoundStream(string name, byte[] buffer)
    {
        Name = name;
        _buffer = buffer;
        _length = buffer.Length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStream" /> class that reads its sectors on demand.
    /// </summary>
    /// <param name="name">The directory name of the stream.</param>
    /// <param name="sectors">The sector reader used to read sectors on demand.</param>
    /// <param name="chain">The ordered sector chain of the stream.</param>
    /// <param name="size">The declared payload length, in bytes.</param>
    /// <param name="sectorSize">The regular sector size, in bytes.</param>
    internal CompoundStream(string name, CompoundSectorReader sectors, uint[] chain, long size, int sectorSize)
    {
        Name = name;
        _sectors = sectors;
        _chain = chain;
        _length = size;
        _sectorSize = sectorSize;
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
    public override long Length => _length;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown on set when the value is negative.</exception>
    public override long Position
    {
        get => _position;
        set
        {
            ThrowHelper.ThrowIfNegative(value);
            _position = Math.Min(value, _length);
        }
    }

    /// <summary>
    /// Returns a read-only view over the entire stream payload, materializing it for an on-demand stream.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}" /> spanning the payload bytes.</returns>
    /// <remarks>
    /// For a buffered stream this returns a view over the already-materialized payload without copying; for a streaming
    /// stream it reads the whole payload into memory. The returned view does not advance or depend on
    /// <see cref="Position" />. Prefer chunked <see cref="Read(Span{byte})" /> for large streaming payloads.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using CompoundStream stream = entry.Open();
    /// ReadOnlyMemory<byte> payload = stream.AsMemory();
    /// ushort recordType = BinaryPrimitives.ReadUInt16LittleEndian(payload.Span);
    ///]]>
    /// </code>
    /// </example>
    public ReadOnlyMemory<byte> AsMemory() =>
        _buffer ?? (_length == 0 ? ReadOnlyMemory<byte>.Empty : _sectors!.ReadChain(_chain![0], _length));

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfNull(buffer);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);

        return Read(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        long remaining = _length - _position;
        if (remaining <= 0)
            return 0;

        int want = (int)Math.Min(buffer.Length, remaining);
        if (_buffer is not null)
        {
            _buffer.AsSpan((int)_position, want).CopyTo(buffer);
            _position += want;
            return want;
        }

        int total = 0;
        while (want > 0)
        {
            int sectorIndex = (int)(_position / _sectorSize);
            int within = (int)(_position % _sectorSize);
            int n = Math.Min(want, _sectorSize - within);
            _sectors!.ReadWithinSector(_chain![sectorIndex], within, buffer.Slice(total, n));
            want -= n;
            total += n;
            _position += n;
        }

        return total;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfNull(buffer);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<int>(cancellationToken);

        try
        {
            return Task.FromResult(Read(buffer.AsSpan(offset, count)));
        }
        catch (Exception ex)
        {
            return Task.FromException<int>(ex);
        }
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<int>(cancellationToken);

        try
        {
            return new ValueTask<int>(Read(buffer.Span));
        }
        catch (Exception ex)
        {
            return ValueTask.FromException<int>(ex);
        }
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        long target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        if (target < 0)
            throw new IOException();

        _position = Math.Min(target, _length);
        return _position;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        // No-op: the stream is read-only.
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
