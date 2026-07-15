// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbStreamDataSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.Internal;

/// <summary>
/// A <see cref="CfbDataSource" /> backed by a seekable stream, reading the requested ranges on demand.
/// </summary>
/// <remarks>
/// Reads are serialized by a semaphore because they move the shared stream position, so a streaming compound file
/// supports concurrent access but not parallel reads. The same gate guards the synchronous and asynchronous read paths
/// (a monitor lock cannot span an <c>await</c>). The source does not own the stream; the owning
/// <see cref="CompoundFile" /> disposes it according to its <c>leaveOpen</c> contract.
/// </remarks>
internal sealed class CfbStreamDataSource
    : CfbDataSource
{
    /// <summary>The backing seekable stream.</summary>
    private readonly Stream _stream;

    /// <summary>The captured stream length.</summary>
    private readonly long _length;

    /// <summary>Serializes seek-and-read against the shared stream position across the sync and async paths.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CfbStreamDataSource" /> class.
    /// </summary>
    /// <param name="stream">The seekable stream containing the compound file.</param>
    public CfbStreamDataSource(Stream stream)
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
        _gate.Wait();
        try
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.ReadExactly(destination);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public override async ValueTask ReadAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            await _stream.ReadExactlyAsync(destination, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public override void Dispose() =>
        _gate.Dispose();
}
