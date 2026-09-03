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
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _starts[^1];

    /// <inheritdoc />
    public override long Position
    {
        get => _position;
        set
        {
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
