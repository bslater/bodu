// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamDataSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// A <see cref="CompoundDataSource" /> backed by a seekable stream, reading the requested ranges on demand.
/// </summary>
/// <remarks>
/// Reads are serialized by a lock because they move the shared stream position, so a streaming compound file supports
/// concurrent access but not parallel reads. The source does not own the stream; the owning <see cref="CompoundFile" />
/// disposes it according to its <c>leaveOpen</c> contract.
/// </remarks>
internal sealed class CompoundStreamDataSource
    : CompoundDataSource
{
    /// <summary>The backing seekable stream.</summary>
    private readonly Stream _stream;

    /// <summary>The captured stream length.</summary>
    private readonly long _length;

    /// <summary>Serializes seek-and-read against the shared stream position.</summary>
    private readonly object _gate = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamDataSource" /> class.
    /// </summary>
    /// <param name="stream">The seekable stream containing the compound file.</param>
    public CompoundStreamDataSource(Stream stream)
    {
        _stream = stream;
        _length = stream.Length;
    }

    /// <inheritdoc />
    public override long Length => _length;

    /// <inheritdoc />
    public override ReadOnlySpan<byte> GetSpan(long offset, int length, Span<byte> scratch)
    {
        Read(offset, scratch.Slice(0, length));
        return scratch.Slice(0, length);
    }

    /// <inheritdoc />
    public override void Read(long offset, Span<byte> destination)
    {
        lock (_gate)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.ReadExactly(destination);
        }
    }
}
