// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

/// <summary>
/// Provides seekable access to the bytes of a single stream within a compound file.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/io-compound-stream-access.svg" alt="CompoundStorage.OpenStream returns a CompoundStream, a read-only seekable Stream cursor whose CanWrite is false. When the owning CompoundFile was opened buffered (the default), the stream's whole payload is materialized into an in-memory byte array at open time and Read, Seek, and AsMemory work over that array. When the file was opened with buffered false, the cursor walks the stream's sector chain in the source on demand: each Read locates the sector for the current Position, copies only the bytes within that sector, and advances, so the full payload is never held in memory. AsMemory materializes the whole chain on request."/>
/// </para>
/// <para>
/// <see cref="CompoundStream" /> is a standard <see cref="Stream" /> cursor obtained from
/// <see cref="CompoundStorage.OpenStream(string)" />, so it composes with the BCL surfaces that consume a
/// <see cref="Stream" /> — <see cref="System.IO.StreamReader" />, <see cref="System.IO.BinaryReader" />,
/// <see cref="Stream.CopyTo(Stream)" />, and the deserializers built on top of them.
/// </para>
/// <para>
/// A stream opened from a buffered compound file is backed by an in-memory payload assembled at open time. A stream
/// opened from a streaming compound file (see <see cref="CompoundFile.Open(System.IO.Stream, bool, bool)" /> with
/// <c>buffered: false</c>) reads its sectors on demand from the underlying source, so it never materializes the whole
/// payload. In the streaming case the owning <see cref="CompoundFile" /> and its source must remain open for the
/// lifetime of the cursor.
/// </para>
/// <para>
/// A stream opened from a writable compound file (see
/// <see cref="CompoundFile.Create(System.IO.Stream, bool, Bodu.IO.Compound.Builders.CompoundBuildOptions)" />) is
/// backed by a growable in-memory buffer: <see cref="CanWrite" /> is <see langword="true" />, and
/// <see cref="Write(byte[], int, int)" /> and <see cref="SetLength(long)" /> mutate the buffer. The edits are staged in
/// memory and are persisted to the underlying destination only when <see cref="CompoundFile.Commit" /> is called;
/// closing the cursor flushes its buffer into the staging tree but does not write to the destination. A writable cursor
/// holds at most <see cref="int.MaxValue" /> bytes — payloads beyond that are authored through the deferred stream
/// sources on <see cref="Bodu.IO.Compound.Builders.CompoundStorageBuilder" /> (<c>AddStream(name, openRead, length)</c>
/// or <c>AddStreamFromFile</c>), which never buffer the whole payload.
/// </para>
/// <para>
/// A read-only cursor (<see cref="CanWrite" /> is <see langword="false" />) throws <see cref="NotSupportedException" />
/// from <see cref="Write(byte[], int, int)" /> and <see cref="SetLength(long)" />. Because reads against a streaming
/// source advance a shared position, a single cursor is not safe for concurrent use; open one cursor per reader.
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
/// using CompoundStream stream = file.RootStorage.OpenStream("Workbook");
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
    /// <summary>The materialized payload, or <see langword="null" /> when the stream reads on demand or is writable.</summary>
    private readonly byte[]? _buffer;

    /// <summary>The sector reader used for on-demand reads, or <see langword="null" /> when buffered or writable.</summary>
    private readonly CfbSectorReader? _sectors;

    /// <summary>The ordered sector chain for on-demand reads, or <see langword="null" /> when buffered or writable.</summary>
    private readonly uint[]? _chain;

    /// <summary>The declared payload length for a read cursor.</summary>
    private readonly long _length;

    /// <summary>The regular sector size, in bytes, used for on-demand reads.</summary>
    private readonly int _sectorSize;

    /// <summary>The metadata snapshot for the stream entry.</summary>
    private readonly CompoundEntryInfo _info;

    /// <summary>The growable write buffer, or <see langword="null" /> when the cursor is read-only.</summary>
    private readonly MemoryStream? _write;

    /// <summary>The staging node this writable cursor flushes into, or <see langword="null" /> when read-only.</summary>
    private readonly CompoundStreamBuilder? _node;

    /// <summary>The owning writable file marked dirty on flush, or <see langword="null" /> when read-only.</summary>
    private readonly CompoundFile? _file;

    /// <summary>Whether a writable cursor also permits reading.</summary>
    private readonly bool _canRead;

    /// <summary>The storage that owns this cursor, or <see langword="null" /> when none was supplied.</summary>
    private readonly CompoundStorage? _parent;

    /// <summary>The current read position within a read cursor's payload.</summary>
    private long _position;

    /// <summary>Whether this cursor has been disposed.</summary>
    private bool _disposed;

    /// <summary>Whether the writable buffer holds changes not yet flushed into the staging node.</summary>
    private bool _writePending;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStream" /> class over a materialized payload.
    /// </summary>
    /// <param name="info">The metadata snapshot of the stream entry.</param>
    /// <param name="buffer">The materialized stream payload, already trimmed to the declared size.</param>
    /// <param name="parent">The storage that owns the cursor.</param>
    internal CompoundStream(CompoundEntryInfo info, byte[] buffer, CompoundStorage? parent)
    {
        _info = info;
        _buffer = buffer;
        _length = buffer.Length;
        _canRead = true;
        _parent = parent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStream" /> class that reads its sectors on demand.
    /// </summary>
    /// <param name="info">The metadata snapshot of the stream entry.</param>
    /// <param name="sectors">The sector reader used to read sectors on demand.</param>
    /// <param name="chain">The ordered sector chain of the stream.</param>
    /// <param name="size">The declared payload length, in bytes.</param>
    /// <param name="sectorSize">The regular sector size, in bytes.</param>
    /// <param name="parent">The storage that owns the cursor.</param>
    internal CompoundStream(CompoundEntryInfo info, CfbSectorReader sectors, uint[] chain, long size, int sectorSize, CompoundStorage? parent)
    {
        _info = info;
        _sectors = sectors;
        _chain = chain;
        _length = size;
        _sectorSize = sectorSize;
        _canRead = true;
        _parent = parent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStream" /> class as a writable cursor over a staging node.
    /// </summary>
    /// <param name="file">The owning writable compound file, marked dirty when the cursor flushes.</param>
    /// <param name="node">The staging stream node the cursor flushes its buffer into.</param>
    /// <param name="initialContent">
    /// The node's current payload used to seed the buffer; empty for a truncating open.
    /// </param>
    /// <param name="canRead">Whether the cursor also permits reading.</param>
    /// <param name="parent">The storage that owns the cursor.</param>
    internal CompoundStream(CompoundFile file, CompoundStreamBuilder node, ReadOnlyMemory<byte> initialContent, bool canRead, CompoundStorage parent)
    {
        _info = new CompoundEntryInfo { Name = node.Name, EntryType = CompoundEntryType.Stream };
        _file = file;
        _node = node;
        _canRead = canRead;
        _parent = parent;
        _write = new MemoryStream();
        if (!initialContent.IsEmpty)
        {
            _write.Write(initialContent.Span);
            _write.Position = 0;
        }

        // A truncating open (FileMode.Create over an existing stream) seeds the cursor empty but leaves the node's
        // old content in place, so the cursor starts flush-pending: a write-less dispose must still persist the
        // truncation into the staging tree.
        _writePending = true;
    }

    /// <summary>
    /// Gets the directory name of the stream.
    /// </summary>
    /// <value>The stream name as stored in the compound-file directory.</value>
    public string Name => _info.Name;

    /// <summary>
    /// Gets the storage that owns this stream cursor.
    /// </summary>
    /// <value>The owning <see cref="CompoundStorage" />, or <see langword="null" /> when not available.</value>
    public CompoundStorage? Parent => _parent;

    /// <summary>
    /// Gets the metadata snapshot for this stream entry.
    /// </summary>
    /// <value>A <see cref="CompoundEntryInfo" /> describing the stream's name, size, class id, and timestamps.</value>
    public CompoundEntryInfo Stat =>
        _write is null ? _info : _info with { Length = _write.Length };

    /// <inheritdoc />
    public override bool CanRead => !_disposed && (_write is null || _canRead);

    /// <inheritdoc />
    public override bool CanSeek => !_disposed;

    /// <inheritdoc />
    public override bool CanWrite => !_disposed && _write is not null;

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">Thrown when the cursor has been disposed.</exception>
    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return _write?.Length ?? _length;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown on set when the value is negative.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the cursor has been disposed.</exception>
    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _write?.Position ?? _position;
        }

        set
        {
            ThrowIfDisposed();
            ThrowHelper.ThrowIfNegative(value);
            if (_write is not null)
                _write.Position = value;
            else
                _position = value;
        }
    }

    /// <summary>
    /// Returns a read-only view over the entire stream payload, materializing it for an on-demand stream.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyMemory{T}" /> spanning the payload bytes.</returns>
    /// <remarks>
    /// For a buffered stream this returns a view over the already-materialized payload without copying; for a streaming
    /// stream it reads the whole payload into memory; for a writable stream it returns the bytes written so far. The
    /// returned view does not advance or depend on <see cref="Position" />. Prefer chunked
    /// <see cref="Read(Span{byte})" /> for large streaming payloads.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using CompoundStream stream = storage.OpenStream("Workbook");
    /// ReadOnlyMemory<byte> payload = stream.AsMemory();
    /// ushort recordType = BinaryPrimitives.ReadUInt16LittleEndian(payload.Span);
    ///]]>
    /// </code>
    /// </example>
    public ReadOnlyMemory<byte> AsMemory()
    {
        ThrowIfDisposed();
        if (_write is not null)
            return _write.GetBuffer().AsMemory(0, (int)_write.Length);

        return _buffer ?? (_length == 0 ? ReadOnlyMemory<byte>.Empty : _sectors!.ReadChain(_chain![0], _length));
    }

    /// <summary>
    /// Copies the entire stream payload into a new byte array, independent of the current position.
    /// </summary>
    /// <returns>A <see cref="byte" /> array containing the whole payload.</returns>
    /// <remarks>
    /// Mirrors <c>File.ReadAllBytes</c> for callers that consume the whole stream in one pass. Prefer
    /// <see cref="AsMemory" /> when a zero-copy view suffices, or chunked <see cref="Read(Span{byte})" /> for large
    /// streaming payloads.
    /// </remarks>
    public byte[] ReadAllBytes() =>
        AsMemory().ToArray();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfNull(buffer);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);

        return Read(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Thrown when the cursor is write-only.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the cursor has been disposed.</exception>
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        if (_write is not null)
        {
            if (!_canRead)
                throw new NotSupportedException();

            return _write.Read(buffer);
        }

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

            // A crafted directory can declare a stream size larger than its actual sector chain covers. Guard
            // the chain index so an over-declared size surfaces as a catchable CompoundFileFormatException rather
            // than an IndexOutOfRangeException escaping the Stream.Read contract.
            CompoundThrowHelper.ThrowFormatIf(
                (uint)sectorIndex >= (uint)_chain!.Length,
                CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
                CompoundFileError.StreamChainTooShort);

            int within = (int)(_position % _sectorSize);
            int n = Math.Min(want, _sectorSize - within);
            _sectors!.ReadWithinSector(_chain[sectorIndex], within, buffer.Slice(total, n));
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

        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <inheritdoc />
    /// <remarks>
    /// This is truly asynchronous only for a streaming-mode cursor, which reads its sectors on demand from the
    /// underlying source; a buffered or writable cursor reads from memory and completes synchronously.
    /// </remarks>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<int>(cancellationToken);

        // Only a streaming cursor performs real I/O; buffered and writable cursors read from memory synchronously.
        if (_sectors is not null)
            return ReadStreamingAsync(buffer, cancellationToken);

        try
        {
            return new ValueTask<int>(Read(buffer.Span));
        }
        catch (Exception ex)
        {
            return ValueTask.FromException<int>(ex);
        }
    }

    /// <summary>
    /// Reads from a streaming-mode cursor by walking its sector chain with asynchronous source reads.
    /// </summary>
    /// <param name="buffer">The buffer that receives the bytes.</param>
    /// <param name="cancellationToken">A token that cancels the read.</param>
    /// <returns>The number of bytes read.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the cursor has been disposed.</exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the declared size outruns the sector chain.
    /// </exception>
    private async ValueTask<int> ReadStreamingAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        long remaining = _length - _position;
        if (remaining <= 0)
            return 0;

        int want = (int)Math.Min(buffer.Length, remaining);
        int total = 0;
        while (want > 0)
        {
            int sectorIndex = (int)(_position / _sectorSize);

            // A crafted directory can declare a stream size larger than its actual sector chain covers. Guard
            // the chain index so an over-declared size surfaces as a catchable CompoundFileFormatException rather
            // than an IndexOutOfRangeException escaping the Stream.Read contract.
            CompoundThrowHelper.ThrowFormatIf(
                (uint)sectorIndex >= (uint)_chain!.Length,
                CompoundResourceStrings.Format_Invalid_CompoundSectorChain,
                CompoundFileError.StreamChainTooShort);

            int within = (int)(_position % _sectorSize);
            int n = Math.Min(want, _sectorSize - within);
            await _sectors!.ReadWithinSectorAsync(_chain[sectorIndex], within, buffer.Slice(total, n), cancellationToken).ConfigureAwait(false);
            want -= n;
            total += n;
            _position += n;
        }

        return total;
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">Thrown when the cursor has been disposed.</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        if (_write is not null)
            return _write.Seek(offset, origin);

        long target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        if (target < 0)
            throw new IOException();

        // Seeking beyond the end is permitted (Stream contract); a subsequent read simply returns zero.
        _position = target;
        return _position;
    }

    /// <inheritdoc />
    /// <remarks>
    /// For a writable cursor this flushes the buffered bytes into the staging tree and marks the owning file dirty; the
    /// destination is written only by <see cref="CompoundFile.Commit" />. For a read-only cursor this is a no-op.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the cursor has been disposed.</exception>
    public override void Flush()
    {
        ThrowIfDisposed();
        if (_write is not null)
            FlushToNode(disposing: false);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// Thrown when the cursor is read-only, or when <paramref name="value" /> exceeds the <see cref="int.MaxValue" />
    /// writable-cursor payload cap.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the cursor has been disposed.</exception>
    /// <remarks>
    /// A writable cursor buffers its payload in memory and supports at most <see cref="int.MaxValue" /> bytes; author
    /// larger payloads through a deferred stream source (<c>AddStream(name, openRead, length)</c> or
    /// <c>AddStreamFromFile</c>).
    /// </remarks>
    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        if (_write is null)
            throw new NotSupportedException();
        if (value > int.MaxValue)
            throw new NotSupportedException(CompoundResourceStrings.Op_NotSupported_CompoundStreamPayloadTooLarge);

        _write.SetLength(value);
        _writePending = true;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowHelper.ThrowIfNull(buffer);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);

        Write(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// Thrown when the cursor is read-only, or when the write would grow the payload beyond the
    /// <see cref="int.MaxValue" /> writable-cursor cap.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the cursor has been disposed.</exception>
    /// <remarks>
    /// A writable cursor buffers its payload in memory and supports at most <see cref="int.MaxValue" /> bytes; author
    /// larger payloads through a deferred stream source (<c>AddStream(name, openRead, length)</c> or
    /// <c>AddStreamFromFile</c>).
    /// </remarks>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        if (_write is null)
            throw new NotSupportedException();
        if (buffer.Length > int.MaxValue - _write.Position)
            throw new NotSupportedException(CompoundResourceStrings.Op_NotSupported_CompoundStreamPayloadTooLarge);

        _write.Write(buffer);
        _writePending = true;
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Thrown when the cursor is read-only.</exception>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfNull(buffer);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try
        {
            Write(buffer.AsSpan(offset, count));
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Thrown when the cursor is read-only.</exception>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled(cancellationToken);

        try
        {
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            return ValueTask.FromException(ex);
        }
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try
        {
            Flush();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing && _write is not null)
        {
            FlushToNode(disposing: true);
            _write.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Persists the buffered bytes into the staging stream node and marks the owning file dirty; a no-op when nothing
    /// changed since the last flush.
    /// </summary>
    /// <param name="disposing">
    /// Whether the cursor is being disposed. A disposing flush transfers the write buffer to the node without copying —
    /// the dying cursor can no longer mutate it — while a mid-life flush stores a snapshot copy so later cursor writes
    /// cannot alias already-flushed node content.
    /// </param>
    /// <remarks>
    /// A transferred buffer is the write buffer's growth array, so the node can retain up to roughly twice the payload
    /// as slack until its content is next replaced or the staging tree is released; the copy saved on every dispose
    /// outweighs that transient slack for staged edits that are committed and released.
    /// </remarks>
    private void FlushToNode(bool disposing)
    {
        if (!_writePending)
            return;

        int length = (int)_write!.Length;
        if (disposing)
            _node!.Content = _write.GetBuffer().AsMemory(0, length);
        else
            _node!.SetContent(_write.GetBuffer().AsSpan(0, length));

        _file!.MarkDirty();
        _writePending = false;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> when this cursor has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the cursor has been disposed.</exception>
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
