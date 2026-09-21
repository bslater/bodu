// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTree.LeafHashing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The leaf-hashing loops behind block mode and entry mode: a stream read forward in fixed-size blocks, sequentially or
/// in batches hashed by parallel workers, and a buffer or entry list hashed block by block on workers.
/// </summary>
public sealed partial class MerkleTree
{
    /// <summary>The greatest number of blocks buffered per batch, whatever degree of parallelism is requested.</summary>
    /// <remarks>
    /// Buffering is <c>batchSize × (blockSize + 1)</c> bytes, so an unreasonably large requested degree would otherwise
    /// translate directly into an unreasonably large allocation. Capping the batch costs nothing: the requested degree
    /// still reaches <see cref="ParallelOptions" />, and a degree above this cap simply has fewer blocks available to
    /// work on at once.
    /// </remarks>
    internal const int MaximumBatchSize = 256;

    /// <summary>
    /// Resolves a degree-of-parallelism argument to a concrete batch size.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The requested degree, or <c>-1</c> for the processor count.</param>
    /// <returns>The number of blocks to buffer and hash per batch.</returns>
    internal static int ResolveBatchSize(int maxDegreeOfParallelism)
    {
        int requested = maxDegreeOfParallelism == -1 ? Environment.ProcessorCount : maxDegreeOfParallelism;
        return Math.Clamp(requested, 1, MaximumBatchSize);
    }

    /// <summary>
    /// Reads a stream forward in fixed-size blocks, invoking <paramref name="onLeafHash" /> with each block's leaf hash
    /// in order.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="onLeafHash">Invoked once per block, in block order.</param>
    /// <param name="cancellationToken">A token observed between blocks.</param>
    /// <returns>The total number of bytes read.</returns>
    /// <remarks>
    /// The rented buffer holds the leaf-domain prefix at index zero and the block's bytes from index one, so each block
    /// is hashed straight out of the buffer it was read into and the stream's bytes are never copied again. A short
    /// read is topped up rather than taken as the end of the stream, which a network, cryptographic or decompression
    /// stream requires — only a read returning zero ends the loop.
    /// </remarks>
    internal static long ForEachLeafHash(
        Stream source,
        int blockSize,
        HashAlgorithm hasher,
        int hashLength,
        Action<byte[]> onLeafHash,
        CancellationToken cancellationToken)
    {
        long inputLength = 0;
        byte[] rented = ArrayPool<byte>.Shared.Rent(blockSize + 1);
        try
        {
            rented[0] = LeafPrefix;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int filled = FillBlock(source, rented, blockSize);
                if (filled == 0)
                    break;

                inputLength += filled;
                onLeafHash(HashBuffer(hasher, hashLength, rented.AsSpan(0, 1 + filled)));

                // A partially-filled block can only be the last: the fill exits early solely on end of stream.
                if (filled < blockSize)
                    break;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }

        return inputLength;
    }

    /// <summary>
    /// Reads a stream forward in fixed-size blocks asynchronously, invoking <paramref name="onLeafHash" /> with each
    /// block's leaf hash in order.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="onLeafHash">Invoked once per block, in block order, on the continuation thread.</param>
    /// <param name="cancellationToken">A token observed between blocks and by every read.</param>
    /// <returns>The total number of bytes read.</returns>
    /// <remarks>
    /// The asynchronous twin of <see cref="ForEachLeafHash" />: the same buffer layout, the same short-read top-up and
    /// the same end-of-stream rule, with <see cref="Stream.ReadAsync(Memory{byte}, CancellationToken)" /> in place of
    /// the blocking read.
    /// </remarks>
    internal static async ValueTask<long> ForEachLeafHashAsync(
        Stream source,
        int blockSize,
        HashAlgorithm hasher,
        int hashLength,
        Action<byte[]> onLeafHash,
        CancellationToken cancellationToken)
    {
        long inputLength = 0;
        byte[] rented = ArrayPool<byte>.Shared.Rent(blockSize + 1);
        try
        {
            rented[0] = LeafPrefix;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int filled = await FillBlockAsync(source, rented, blockSize, cancellationToken).ConfigureAwait(false);
                if (filled == 0)
                    break;

                inputLength += filled;
                onLeafHash(HashBuffer(hasher, hashLength, rented.AsSpan(0, 1 + filled)));

                if (filled < blockSize)
                    break;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }

        return inputLength;
    }

    /// <summary>
    /// Reads a stream forward in fixed-size blocks, hashing each batch of blocks concurrently and invoking
    /// <paramref name="onLeafHash" /> with every leaf hash in block order.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="algorithmFactory">Supplies one fresh algorithm per worker.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of blocks to hash concurrently, or <c>-1</c> for the processor count.
    /// </param>
    /// <param name="onLeafHash">Invoked once per block, in block order, on the calling thread.</param>
    /// <param name="cancellationToken">A token observed between batches and by the parallel loop.</param>
    /// <returns>The total number of bytes read.</returns>
    /// <remarks>
    /// The stream is read sequentially into a batch of <c>min(maxDegreeOfParallelism, 256)</c> buffers, the batch is
    /// hashed by <see cref="Parallel" /> with one algorithm per worker, and the hashes are then handed to
    /// <paramref name="onLeafHash" /> in order from the calling thread, so a fold or a recorder downstream never sees
    /// concurrent or out-of-order calls. A worker fault surfaces as itself rather than as an
    /// <see cref="AggregateException" />.
    /// </remarks>
    internal static long ForEachLeafHashParallel(
        Stream source,
        int blockSize,
        Func<HashAlgorithm> algorithmFactory,
        int hashLength,
        int maxDegreeOfParallelism,
        Action<byte[]> onLeafHash,
        CancellationToken cancellationToken)
    {
        var batch = new LeafBatch(blockSize, maxDegreeOfParallelism, cancellationToken);
        long inputLength = 0;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int filledBlocks = batch.Fill(source);
                if (filledBlocks == 0)
                    break;

                inputLength += batch.HashAndReport(filledBlocks, algorithmFactory, hashLength, onLeafHash);
                if (batch.EndedInPartialBlock(filledBlocks))
                    break;
            }
        }
        finally
        {
            batch.Return();
        }

        return inputLength;
    }

    /// <summary>
    /// Reads a stream forward in fixed-size blocks asynchronously, hashing each batch of blocks concurrently and
    /// invoking <paramref name="onLeafHash" /> with every leaf hash in block order.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="algorithmFactory">Supplies one fresh algorithm per worker.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of blocks to hash concurrently, or <c>-1</c> for the processor count.
    /// </param>
    /// <param name="onLeafHash">Invoked once per block, in block order, on the continuation thread.</param>
    /// <param name="cancellationToken">
    /// A token observed between batches, by every read and by the parallel loop.
    /// </param>
    /// <returns>The total number of bytes read.</returns>
    /// <remarks>
    /// The asynchronous twin of <see cref="ForEachLeafHashParallel" />: reads are awaited, and each filled batch is
    /// hashed on the thread pool before the continuation reports its hashes in order.
    /// </remarks>
    internal static async ValueTask<long> ForEachLeafHashParallelAsync(
        Stream source,
        int blockSize,
        Func<HashAlgorithm> algorithmFactory,
        int hashLength,
        int maxDegreeOfParallelism,
        Action<byte[]> onLeafHash,
        CancellationToken cancellationToken)
    {
        var batch = new LeafBatch(blockSize, maxDegreeOfParallelism, cancellationToken);
        long inputLength = 0;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int filledBlocks = await batch.FillAsync(source).ConfigureAwait(false);
                if (filledBlocks == 0)
                    break;

                inputLength += batch.HashAndReport(filledBlocks, algorithmFactory, hashLength, onLeafHash);
                if (batch.EndedInPartialBlock(filledBlocks))
                    break;
            }
        }
        finally
        {
            batch.Return();
        }

        return inputLength;
    }

    /// <summary>
    /// Hashes the fixed-size blocks of a buffer concurrently, one leaf hash per block index.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="algorithmFactory">Supplies one fresh algorithm per worker.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of blocks to hash concurrently, or <c>-1</c> for the processor count.
    /// </param>
    /// <param name="cancellationToken">A token the parallel loop observes.</param>
    /// <returns>The leaf hashes in block order; empty for an empty input.</returns>
    /// <remarks>
    /// Every block is sliced and hashed inside its own worker, so the whole per-block cost parallelizes — the reason
    /// this scales closer to the core count than the stream loops, which must copy each block on the calling thread.
    /// </remarks>
    internal static byte[][] HashLeavesParallel(
        ReadOnlyMemory<byte> source,
        int blockSize,
        Func<HashAlgorithm> algorithmFactory,
        int hashLength,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        long count = BlockCount(source.Length, blockSize);
        if (count == 0)
            return [];

        byte[][] leafHashes = new byte[count][];
        RunParallel(() => Parallel.For(
            0L,
            count,
            CreateOptions(maxDegreeOfParallelism, cancellationToken),
            algorithmFactory,
            (index, _, hasher) =>
            {
                int offset = (int)BlockOffset(index, blockSize);
                int length = BlockLength(source.Length, index, blockSize);
                leafHashes[index] = HashWithPrefix(hasher, hashLength, LeafPrefix, source.Slice(offset, length).Span);
                return hasher;
            },
            static hasher => hasher.Dispose()));

        return leafHashes;
    }

    /// <summary>
    /// Hashes an ordered sequence of entries concurrently, one leaf hash per entry.
    /// </summary>
    /// <param name="entries">The entries, in order.</param>
    /// <param name="algorithmFactory">Supplies one fresh algorithm per worker.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of entries to hash concurrently, or <c>-1</c> for the processor count.
    /// </param>
    /// <param name="cancellationToken">A token the parallel loop observes.</param>
    /// <returns>The leaf hashes in entry order; empty for no entries.</returns>
    internal static byte[][] HashLeavesParallel(
        IReadOnlyList<ReadOnlyMemory<byte>> entries,
        Func<HashAlgorithm> algorithmFactory,
        int hashLength,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
            return [];

        byte[][] leafHashes = new byte[entries.Count][];
        RunParallel(() => Parallel.For(
            0,
            entries.Count,
            CreateOptions(maxDegreeOfParallelism, cancellationToken),
            algorithmFactory,
            (index, _, hasher) =>
            {
                leafHashes[index] = HashWithPrefix(hasher, hashLength, LeafPrefix, entries[index].Span);
                return hasher;
            },
            static hasher => hasher.Dispose()));

        return leafHashes;
    }

    /// <summary>
    /// Fills one block buffer from a stream, topping up short reads until the block is full or the stream ends.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="buffer">
    /// The buffer, holding the leaf prefix at index zero; the block is read from index one.
    /// </param>
    /// <param name="blockSize">The size, in bytes, of a full block.</param>
    /// <returns>The number of payload bytes read; zero at end of stream.</returns>
    private static int FillBlock(Stream source, byte[] buffer, int blockSize)
    {
        int filled = 0;
        while (filled < blockSize)
        {
            int read = source.Read(buffer, 1 + filled, blockSize - filled);
            if (read <= 0)
                break;

            filled += read;
        }

        return filled;
    }

    /// <summary>
    /// Fills one block buffer from a stream asynchronously, topping up short reads until the block is full or the
    /// stream ends.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="buffer">
    /// The buffer, holding the leaf prefix at index zero; the block is read from index one.
    /// </param>
    /// <param name="blockSize">The size, in bytes, of a full block.</param>
    /// <param name="cancellationToken">A token every read observes.</param>
    /// <returns>The number of payload bytes read; zero at end of stream.</returns>
    private static async ValueTask<int> FillBlockAsync(Stream source, byte[] buffer, int blockSize, CancellationToken cancellationToken)
    {
        int filled = 0;
        while (filled < blockSize)
        {
            int read = await source
                .ReadAsync(buffer.AsMemory(1 + filled, blockSize - filled), cancellationToken)
                .ConfigureAwait(false);
            if (read <= 0)
                break;

            filled += read;
        }

        return filled;
    }

    /// <summary>
    /// Builds the options for a parallel leaf loop.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The degree limit, or <c>-1</c> for no limit.</param>
    /// <param name="cancellationToken">The token the loop observes.</param>
    /// <returns>The options.</returns>
    private static ParallelOptions CreateOptions(int maxDegreeOfParallelism, CancellationToken cancellationToken) =>
        new() { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = cancellationToken };

    /// <summary>
    /// Runs a parallel loop and surfaces a worker fault as itself rather than wrapped in an
    /// <see cref="AggregateException" />, so a faulting leaf algorithm is reported the way a sequential computation
    /// reports it.
    /// </summary>
    /// <param name="loop">The parallel loop to run.</param>
    /// <remarks>
    /// Cancellation surfaces as the <see cref="OperationCanceledException" /> the loop itself throws. Every worker's
    /// algorithm is disposed by the loop's local finalizer whether the loop completes, faults or is cancelled. Several
    /// workers can fault on the same input at once; the first fault is the one a sequential computation would have
    /// raised, and the others are its echoes.
    /// </remarks>
    private static void RunParallel(Action loop)
    {
        try
        {
            loop();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count > 0)
        {
            ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
        }
    }
}
