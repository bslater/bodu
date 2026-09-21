// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTree.LeafBatch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class MerkleTree
{
    /// <summary>
    /// The rented block buffers, lengths and hashes of one parallel batch, with the fill, hash and return steps the
    /// synchronous and asynchronous stream loops share.
    /// </summary>
    private sealed class LeafBatch
    {
        /// <summary>The size, in bytes, of a full block.</summary>
        private readonly int _blockSize;

        /// <summary>The options every batch's parallel loop runs under.</summary>
        private readonly ParallelOptions _options;

        /// <summary>The rented buffers, each holding the leaf prefix at index zero once first used.</summary>
        private readonly byte[]?[] _buffers;

        /// <summary>The number of payload bytes in each buffer.</summary>
        private readonly int[] _lengths;

        /// <summary>Each block's leaf hash at its own position within the batch.</summary>
        private readonly byte[][] _hashes;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeafBatch" /> class.
        /// </summary>
        /// <param name="blockSize">The size, in bytes, of a full block.</param>
        /// <param name="maxDegreeOfParallelism">The degree limit, which also sizes the batch.</param>
        /// <param name="cancellationToken">The token the parallel loop observes.</param>
        internal LeafBatch(int blockSize, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            int batchSize = ResolveBatchSize(maxDegreeOfParallelism);
            _blockSize = blockSize;
            _options = CreateOptions(maxDegreeOfParallelism, cancellationToken);
            _buffers = new byte[]?[batchSize];
            _lengths = new int[batchSize];
            _hashes = new byte[batchSize][];
        }

        /// <summary>
        /// Fills the batch sequentially from a stream until it is full, the stream ends, or a block comes up short.
        /// </summary>
        /// <param name="source">The stream to read.</param>
        /// <returns>The number of buffers holding a block; zero at end of stream.</returns>
        internal int Fill(Stream source)
        {
            int filledBlocks = 0;
            while (filledBlocks < _buffers.Length)
            {
                int filled = FillBlock(source, Rent(filledBlocks), _blockSize);
                if (filled == 0)
                    break;

                _lengths[filledBlocks++] = filled;
                if (filled < _blockSize)
                    break;
            }

            return filledBlocks;
        }

        /// <summary>
        /// Fills the batch sequentially from a stream asynchronously until it is full, the stream ends, or a block
        /// comes up short.
        /// </summary>
        /// <param name="source">The stream to read.</param>
        /// <returns>The number of buffers holding a block; zero at end of stream.</returns>
        internal async ValueTask<int> FillAsync(Stream source)
        {
            int filledBlocks = 0;
            while (filledBlocks < _buffers.Length)
            {
                int filled = await FillBlockAsync(source, Rent(filledBlocks), _blockSize, _options.CancellationToken).ConfigureAwait(false);
                if (filled == 0)
                    break;

                _lengths[filledBlocks++] = filled;
                if (filled < _blockSize)
                    break;
            }

            return filledBlocks;
        }

        /// <summary>
        /// Hashes the filled blocks concurrently, one algorithm per worker, then reports the hashes in block order from
        /// the calling thread.
        /// </summary>
        /// <param name="filledBlocks">The number of buffers holding a block.</param>
        /// <param name="algorithmFactory">Supplies one fresh algorithm per worker.</param>
        /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
        /// <param name="onLeafHash">Receives each leaf hash in block order.</param>
        /// <returns>The number of payload bytes the batch holds.</returns>
        internal long HashAndReport(int filledBlocks, Func<HashAlgorithm> algorithmFactory, int hashLength, Action<byte[]> onLeafHash)
        {
            RunParallel(() => Parallel.For(
                0,
                filledBlocks,
                _options,
                algorithmFactory,
                (offset, _, hasher) =>
                {
                    _hashes[offset] = HashBuffer(hasher, hashLength, _buffers[offset].AsSpan(0, 1 + _lengths[offset]));
                    return hasher;
                },
                static hasher => hasher.Dispose()));

            long bytes = 0;
            for (int offset = 0; offset < filledBlocks; offset++)
            {
                bytes += _lengths[offset];
                onLeafHash(_hashes[offset]);
            }

            return bytes;
        }

        /// <summary>
        /// Returns whether the last filled block was short, which only the end of the stream can cause.
        /// </summary>
        /// <param name="filledBlocks">The number of buffers holding a block.</param>
        /// <returns><see langword="true" /> when the stream is exhausted.</returns>
        internal bool EndedInPartialBlock(int filledBlocks) =>
            _lengths[filledBlocks - 1] < _blockSize;

        /// <summary>
        /// Returns every rented buffer to the pool, clearing its contents.
        /// </summary>
        internal void Return()
        {
            foreach (byte[]? buffer in _buffers)
            {
                if (buffer is not null)
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        /// <summary>
        /// Rents the buffer at a batch position on first use and stamps the leaf prefix into it.
        /// </summary>
        /// <param name="offset">The position within the batch.</param>
        /// <returns>The buffer, ready to receive a block from index one.</returns>
        private byte[] Rent(int offset)
        {
            byte[] buffer = _buffers[offset] ??= ArrayPool<byte>.Shared.Rent(_blockSize + 1);
            buffer[0] = LeafPrefix;
            return buffer;
        }
    }
}
