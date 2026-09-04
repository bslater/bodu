// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// A read-only, seekable stream over a node's data payload that reads one leaf block at a time, so an arbitrarily
/// large logical payload is never materialized.
/// </summary>
/// <remarks>
/// The stream resolves the node's ordered leaf block-tree entries once at construction — reading only the internal
/// tree blocks — and thereafter loads leaf payloads on demand through <see cref="PstSource.ReadBlock" />, so repeat
/// visits ride the session's decoded-block cache. Like the owning session, the stream is single-threaded.
/// <para>
/// The stream is bound to its session: once the owning <see cref="PstFile" /> is disposed, every read fails with
/// <see cref="ObjectDisposedException" /> even when the leaf is cached, and disposing the stream itself releases only
/// its cached leaf. Reads complete synchronously; the asynchronous overloads exist for callers on the async API.
/// </para>
/// </remarks>
internal sealed class PstDataStream : Stream
{
    /// <summary>The open source.</summary>
    private readonly PstSource _source;

    /// <summary>The ordered leaf entries whose payloads concatenate to the stream's content.</summary>
    private readonly List<PstBbtEntry> _leaves;

    /// <summary>The logical start offset of each leaf, with the total length as a final sentinel.</summary>
    private readonly long[] _starts;

    /// <summary>The current position.</summary>
    private long _position;

    /// <summary>The index of the leaf held in <see cref="_current" />, or <c>-1</c> when none is loaded.</summary>
    private int _currentIndex = -1;

    /// <summary>The payload of the leaf at <see cref="_currentIndex" />; may alias the session cache — never mutated.</summary>
    private byte[]? _current;

    /// <summary>Whether the stream has been disposed.</summary>
    private bool _disposed;

    /// <inheritdoc />
    public override int ReadByte()
    {
        Span<byte> single = stackalloc byte[1];

        return Read(single) == 1 ? single[0] : -1;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Completes synchronously: the leaf blocks come from the session's decoded-block cache or from an already-open
    /// source stream, so there is no asynchronous work to await. A cancelled token cancels before any byte is read.
    /// </remarks>
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
    /// <remarks>
    /// Completes synchronously, as <see cref="ReadAsync(byte[], int, int, CancellationToken)" /> does.
    /// </remarks>
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
    /// <remarks>
    /// Releases the cached leaf; the session's block cache and source stream are owned by the <see cref="PstFile" />
    /// and are untouched. A disposed stream reports <see cref="CanRead" /> and <see cref="CanSeek" /> as
    /// <see langword="false" /> and throws <see cref="ObjectDisposedException" /> from every other member.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        _disposed = true;
        _current = null;
        _currentIndex = -1;

        base.Dispose(disposing);
    }

    /// <summary>
    /// Throws when the stream has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // The session owns the source; once it is gone even a leaf this stream still holds must not be served.
        ObjectDisposedException.ThrowIf(_source.IsDisposed, typeof(PstFile));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstDataStream" /> class.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="leaves">The ordered leaf entries, from <see cref="PstDataTree.ResolveLeafEntries" />.</param>
    internal PstDataStream(PstSource source, List<PstBbtEntry> leaves)
    {
        _source = source;
        _leaves = leaves;

        _starts = new long[leaves.Count + 1];
        long offset = 0;
        for (int i = 0; i < leaves.Count; i++)
        {
            _starts[i] = offset;
            offset += leaves[i].Length;
        }

        _starts[leaves.Count] = offset;
    }

    /// <inheritdoc />
    public override bool CanRead =>
        !_disposed;

    /// <inheritdoc />
    public override bool CanSeek =>
        !_disposed;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            ThrowIfDisposed();

            return _starts[^1];
        }
    }

    /// <inheritdoc />
    public override long Position
    {
        get
        {
            ThrowIfDisposed();

            return _position;
        }

        set
        {
            ThrowIfDisposed();
            ThrowHelper.ThrowIfNegative(value);

            _position = value;
        }
    }

    /// <inheritdoc />
    public override void Flush()
    {
        // Read-only stream: nothing to flush.
    }

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
        ThrowIfDisposed();

        long remaining = Length - _position;
        if (remaining <= 0 || buffer.IsEmpty)
            return 0;

        int total = 0;
        while (!buffer.IsEmpty && _position < Length)
        {
            int leafIndex = FindLeaf(_position);
            byte[] payload = LoadLeaf(leafIndex);

            int withinLeaf = (int)(_position - _starts[leafIndex]);
            int available = payload.Length - withinLeaf;
            int copied = Math.Min(available, buffer.Length);

            payload.AsSpan(withinLeaf, copied).CopyTo(buffer);
            buffer = buffer[copied..];
            _position += copied;
            total += copied;
        }

        return total;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();

        long target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentException(PstResourceStrings.Arg_Invalid_PstSeekOrigin, nameof(origin)),
        };

        ThrowHelper.ThrowIfNegative(target, nameof(offset));

        _position = target;
        return _position;
    }

    /// <inheritdoc />
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    /// <summary>
    /// Finds the leaf containing a logical offset by binary search over the leaf start table.
    /// </summary>
    /// <param name="position">The logical offset; must be within the stream.</param>
    /// <returns>The leaf index.</returns>
    private int FindLeaf(long position)
    {
        int index = Array.BinarySearch(_starts, 0, _leaves.Count, position);

        return index >= 0 ? index : ~index - 1;
    }

    /// <summary>
    /// Loads a leaf payload, keeping the most recently used leaf resident so sequential reads within one block do not
    /// re-enter the source.
    /// </summary>
    /// <param name="index">The leaf index.</param>
    /// <returns>The leaf payload.</returns>
    private byte[] LoadLeaf(int index)
    {
        if (index != _currentIndex)
        {
            _current = _source.ReadBlock(_leaves[index]);
            _currentIndex = index;
        }

        return _current!;
    }
}
