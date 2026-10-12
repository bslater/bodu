// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedChunkStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A <see cref="MemoryStream" /> wrapper that returns at most <see cref="ChunkSize" /> bytes per <see cref="Read" /> or
/// <see cref="ReadAsync" /> call, regardless of how many bytes the caller requests.
/// </summary>
/// <remarks>
/// <para>
/// Use this stream in tests that need to verify that a consumer correctly accumulates partial reads - for example, when
/// the chunk size is smaller than the processing block size, or when a chunk size larger than the block size crosses
/// block boundaries at unexpected offsets.
/// </para>
/// <para>
/// Setting <paramref name="chunkSize" /> to <c>1</c> produces a one-byte-at-a-time stream, which exercises the most
/// extreme form of partial-read accumulation.
/// </para>
/// <para>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </para>
/// </remarks>
public sealed class FixedChunkStream
    : System.IO.MemoryStream
{
    /// <summary>
    /// Initialises a new instance of <see cref="FixedChunkStream" /> backed by <paramref name="data" /> with the given
    /// maximum read chunk size.
    /// </summary>
    /// <param name="data">The byte array to expose as a stream.</param>
    /// <param name="chunkSize">The maximum number of bytes returned per read call. Must be greater than zero.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="chunkSize" /> is less than or equal to zero.
    /// </exception>
    public FixedChunkStream(byte[] data, int chunkSize)
        : this(data, chunkSize, writable: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FixedChunkStream" /> backed by <paramref name="data" /> with the given
    /// maximum read chunk size, writable or read-only.
    /// </summary>
    /// <param name="data">The byte array to expose as a stream.</param>
    /// <param name="chunkSize">The maximum number of bytes returned per read call. Must be greater than zero.</param>
    /// <param name="writable">
    /// Whether the stream supports writing; when <see langword="false" />, <see cref="CanWrite" /> is
    /// <see langword="false" /> and the stream is read-only, as a <see cref="MemoryStream" /> created read-only is.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="chunkSize" /> is less than or equal to zero.
    /// </exception>
    public FixedChunkStream(byte[] data, int chunkSize, bool writable)
        : base(data ?? throw new ArgumentNullException(nameof(data)), writable)
    {
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero.");

        ChunkSize = chunkSize;
    }

    /// <summary>
    /// Gets the maximum number of bytes returned per read call.
    /// </summary>
    public int ChunkSize { get; }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => base.Read(buffer, offset, Math.Min(count, ChunkSize));

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => base.ReadAsync(buffer[..Math.Min(buffer.Length, ChunkSize)], cancellationToken);
}