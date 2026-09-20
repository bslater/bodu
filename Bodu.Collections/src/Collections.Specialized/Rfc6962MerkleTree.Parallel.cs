// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTree.Parallel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Collections.Specialized;

/// <summary>
/// Parallel leaf hashing. The tree shape is unchanged — only the work of hashing leaves is spread across threads.
/// </summary>
/// <remarks>
/// <para>
/// Leaf hashing is where a block-mode computation spends essentially all of its time: one hash over <c>blockSize</c>
/// bytes per leaf, against <c>n − 1</c> node hashes over twice the digest width each. Hashing a 512 MiB object at
/// one-mebibyte blocks is 512 MiB of leaf hashing and roughly 33 KiB of reduction, so the leaves are the only part
/// worth parallelizing.
/// </para>
/// <para>
/// <strong>Why the two paths cannot disagree.</strong> The parallel entry points differ from the sequential ones only
/// in <em>who</em> computes each leaf hash. Once the leaf hashes exist they are folded by the same <c>Mth</c> used
/// everywhere else, over an array indexed by block position, so the tree shape is not reimplemented here and cannot
/// drift. Leaf hashes are written to fixed indices rather than appended, so completion order has no effect on the
/// result.
/// </para>
/// </remarks>
public sealed partial class Rfc6962MerkleTree
{
    /// <summary>
    /// Computes the root over fixed-size blocks of a stream, hashing leaves in parallel, and returns it with the
    /// ordered leaf hashes.
    /// </summary>
    /// <param name="source">The stream to read to its end. Must be readable.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of blocks to hash concurrently, or <c>-1</c> to use the processor count.
    /// </param>
    /// <param name="cancellationToken">A token observed between batches.</param>
    /// <returns>
    /// The same result <see cref="ComputeBlocked(Stream, int, CancellationToken)" /> would return over the same bytes.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero, or <paramref name="maxDegreeOfParallelism" /> is
    /// zero or below <c>-1</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// <para>
    /// The stream is read sequentially — a stream cannot be read out of order — into a batch of buffers, and the batch
    /// is then hashed concurrently. Peak input buffering is therefore
    /// <c>min(maxDegreeOfParallelism, 256) × (blockSize + 1)</c> bytes: 8 MiB for one-mebibyte blocks across eight
    /// threads. Reading and hashing alternate per batch rather than overlapping, which keeps the memory bound exact and
    /// the ordering trivially deterministic.
    /// </para>
    /// <para>
    /// <strong>Expect a modest gain here, and choose the overload to match the source.</strong> Copying each block off
    /// the stream happens on the calling thread and cannot be parallelized, so it caps the achievable speedup. When the
    /// bytes are already in memory, prefer
    /// <see cref="ComputeBlockedParallel(ReadOnlyMemory{byte}, int, int, CancellationToken)" />, which copies and
    /// hashes each block inside its own worker and therefore scales considerably better.
    /// </para>
    /// <para>
    /// Measured over 64 MiB at one-mebibyte blocks on four cores, this overload returned 2.2× to 2.6× over the
    /// sequential fold, and the in-memory overload 3.1× to 3.4×, across SHA-256, SHA-512, Tiger and BLAKE2b alike. The
    /// leaf hash makes little difference — the same per-block work is being spread either way — so the figure tracks
    /// core count and the source far more than the digest. Treat those numbers as shape rather than specification and
    /// measure your own case; the benchmark under <c>Bodu.Collections/bench</c> reproduces them.
    /// </para>
    /// <para>
    /// The returned root is bit-identical to the sequential overload's for every input, so switching between them is
    /// never a compatibility question — only a performance one.
    /// </para>
    /// </remarks>
    public MerkleComputation ComputeBlockedParallel(
        Stream source,
        int blockSize,
        int maxDegreeOfParallelism = -1,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        MerkleBlocks.ThrowIfBlockSizeInvalid(blockSize);
        ThrowIfDegreeOfParallelismInvalid(maxDegreeOfParallelism);

        int batchSize = ResolveBatchSize(maxDegreeOfParallelism);
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken,
        };

        List<byte[]> leafHashes = [];
        long inputLength = 0;

        byte[]?[] batch = new byte[]?[batchSize];
        int[] batchLengths = new int[batchSize];
        byte[][] batchHashes = new byte[batchSize][];
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Fill the batch sequentially: the prefix sits at index 0 so each block is hashed straight out of
                // the buffer it was read into.
                int filledBlocks = 0;
                while (filledBlocks < batchSize)
                {
                    batch[filledBlocks] ??= ArrayPool<byte>.Shared.Rent(blockSize + 1);
                    byte[] buffer = batch[filledBlocks]!;
                    buffer[0] = MerkleTreeFormat.LeafPrefix;

                    int filled = 0;
                    while (filled < blockSize)
                    {
                        int read = source.Read(buffer, 1 + filled, blockSize - filled);
                        if (read <= 0)
                            break;

                        filled += read;
                    }

                    if (filled == 0)
                        break;

                    batchLengths[filledBlocks] = filled;
                    inputLength += filled;
                    filledBlocks++;

                    if (filled < blockSize)
                        break;
                }

                if (filledBlocks == 0)
                    break;

                HashBatchInParallel(batch, batchLengths, batchHashes, filledBlocks, options);

                // Appended sequentially: the workers wrote to distinct array slots, and the list is only ever
                // mutated from this thread.
                for (int offset = 0; offset < filledBlocks; offset++)
                    leafHashes.Add(batchHashes[offset]);

                // A partially-filled final block ends the stream.
                if (batchLengths[filledBlocks - 1] < blockSize)
                    break;
            }
        }
        finally
        {
            foreach (byte[]? buffer in batch)
            {
                if (buffer is not null)
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        using HashAlgorithm reducer = CreateAlgorithm();
        byte[] root = leafHashes.Count == 0
            ? HashEmpty(reducer)
            : Mth(CollectionsMarshal.AsSpan(leafHashes), reducer);

        return new MerkleComputation(root, inputLength, blockSize, leafHashes);
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a buffer already in memory, hashing leaves in parallel.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of blocks to hash concurrently, or <c>-1</c> to use the processor count.
    /// </param>
    /// <param name="cancellationToken">A token observed while hashing.</param>
    /// <returns>
    /// The same result <see cref="ComputeBlocked(ReadOnlySpan{byte}, int)" /> would return over the same bytes.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero, or <paramref name="maxDegreeOfParallelism" /> is
    /// zero or below <c>-1</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// <para>
    /// This scales better than the stream overload and is the one to prefer when the bytes are already in memory.
    /// Reading a stream is necessarily sequential, so the stream overload copies each block on the calling thread
    /// before any worker can hash it; here every block is both copied and hashed inside its own worker, so the whole
    /// per-block cost parallelizes.
    /// </para>
    /// <para>
    /// That is why this overload scales close to the core count where the stream overload cannot: on four cores it
    /// returned 3.1× to 3.4× against the sequential fold, regardless of whether the leaf hash was a
    /// hardware-accelerated SHA-256 or a managed digest. See the remarks on
    /// <see cref="ComputeBlockedParallel(Stream, int, int, CancellationToken)" /> for the full measured comparison.
    /// </para>
    /// </remarks>
    public MerkleComputation ComputeBlockedParallel(
        ReadOnlyMemory<byte> source,
        int blockSize,
        int maxDegreeOfParallelism = -1,
        CancellationToken cancellationToken = default)
    {
        MerkleBlocks.ThrowIfBlockSizeInvalid(blockSize);
        ThrowIfDegreeOfParallelismInvalid(maxDegreeOfParallelism);

        long count = MerkleBlocks.BlockCount(source.Length, blockSize);
        if (count == 0)
        {
            using HashAlgorithm empty = CreateAlgorithm();
            return new MerkleComputation(HashEmpty(empty), 0, blockSize, []);
        }

        byte[][] leafHashes = new byte[count][];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken,
        };

        _ = Parallel.For(
            0L,
            count,
            options,
            CreateAlgorithm,
            (index, _, hasher) =>
            {
                int offset = (int)MerkleBlocks.BlockOffset(index, blockSize);
                int length = MerkleBlocks.BlockLength(source.Length, index, blockSize);
                leafHashes[index] = HashWithPrefix(
                    hasher,
                    MerkleTreeFormat.LeafPrefix,
                    source.Slice(offset, length).Span);
                return hasher;
            },
            hasher => hasher.Dispose());

        using HashAlgorithm reducer = CreateAlgorithm();
        return new MerkleComputation(Mth(leafHashes, reducer), source.Length, blockSize, leafHashes);
    }

    /// <summary>
    /// Computes the Merkle Tree Hash over an ordered sequence of entries, hashing leaves in parallel.
    /// </summary>
    /// <param name="entries">The entries, in order.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of entries to hash concurrently, or <c>-1</c> to use the processor count.
    /// </param>
    /// <param name="cancellationToken">A token observed while hashing.</param>
    /// <returns>
    /// The same root <see cref="ComputeRoot(IReadOnlyList{ReadOnlyMemory{byte}})" /> would return over the same
    /// entries.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="entries" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxDegreeOfParallelism" /> is zero or below <c>-1</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// Worthwhile only when the entries are individually large; for a log of short entries the reduction and the thread
    /// coordination together outweigh the leaf hashing, and the sequential overload is faster.
    /// </remarks>
    public byte[] ComputeRootParallel(
        IReadOnlyList<ReadOnlyMemory<byte>> entries,
        int maxDegreeOfParallelism = -1,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(entries);
        ThrowIfDegreeOfParallelismInvalid(maxDegreeOfParallelism);

        using HashAlgorithm reducer = CreateAlgorithm();
        if (entries.Count == 0)
            return HashEmpty(reducer);

        byte[][] leafHashes = new byte[entries.Count][];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken,
        };

        _ = Parallel.For(
            0,
            entries.Count,
            options,
            CreateAlgorithm,
            (index, _, hasher) =>
            {
                leafHashes[index] = HashWithPrefix(hasher, MerkleTreeFormat.LeafPrefix, entries[index].Span);
                return hasher;
            },
            hasher => hasher.Dispose());

        return Mth(leafHashes, reducer);
    }

    /// <summary>
    /// Hashes a filled batch of block buffers concurrently, writing each leaf hash to its own position.
    /// </summary>
    /// <param name="batch">The rented buffers, each holding the leaf prefix at index zero.</param>
    /// <param name="batchLengths">The number of payload bytes in each buffer.</param>
    /// <param name="batchHashes">Receives each block's leaf hash at its own position within the batch.</param>
    /// <param name="filledBlocks">The number of buffers that hold a block.</param>
    /// <param name="options">The parallel options, carrying the degree limit and cancellation.</param>
    /// <remarks>
    /// Each worker holds its own <see cref="HashAlgorithm" /> for the batch, so the factory is invoked once per worker
    /// rather than once per block. Writes go to distinct slots of a plain array, so no synchronization is needed and
    /// completion order cannot affect the result.
    /// </remarks>
    private void HashBatchInParallel(
        byte[]?[] batch,
        int[] batchLengths,
        byte[][] batchHashes,
        int filledBlocks,
        ParallelOptions options)
    {
        _ = Parallel.For(
            0,
            filledBlocks,
            options,
            CreateAlgorithm,
            (offset, _, hasher) =>
            {
                byte[] buffer = batch[offset]!;
                batchHashes[offset] = HashBuffer(hasher, buffer.AsSpan(0, 1 + batchLengths[offset]));
                return hasher;
            },
            hasher => hasher.Dispose());
    }

    /// <summary>
    /// Hashes a payload that already carries its domain-separation prefix at index zero.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="prefixedPayload">The prefix byte followed by the node's payload.</param>
    /// <returns>The resulting hash.</returns>
    private byte[] HashBuffer(HashAlgorithm hasher, ReadOnlySpan<byte> prefixedPayload) =>
        MerkleTreeCore.HashBuffer(hasher, HashLength, prefixedPayload);

    /// <summary>The greatest number of blocks buffered per batch, whatever degree of parallelism is requested.</summary>
    /// <remarks>
    /// Buffering is <c>batchSize × (blockSize + 1)</c> bytes, so an unreasonably large requested degree would otherwise
    /// translate directly into an unreasonably large allocation. Capping the batch costs nothing: the requested degree
    /// is still handed to <see cref="ParallelOptions" />, and a degree above this cap simply has fewer blocks available
    /// to work on at once.
    /// </remarks>
    private const int MaximumBatchSize = MerkleTreeCore.MaximumBatchSize;

    /// <summary>
    /// Resolves a degree-of-parallelism argument to a concrete batch size.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The requested degree, or <c>-1</c> for the processor count.</param>
    /// <returns>The number of blocks to buffer and hash per batch.</returns>
    private static int ResolveBatchSize(int maxDegreeOfParallelism) =>
        MerkleTreeCore.ResolveBatchSize(maxDegreeOfParallelism);

    /// <summary>
    /// Throws when a degree-of-parallelism argument is neither <c>-1</c> nor a positive count.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The value to validate.</param>
    /// <param name="paramName">The caller-supplied parameter name, captured automatically.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxDegreeOfParallelism" /> is zero or is less than <c>-1</c>.
    /// </exception>
    private static void ThrowIfDegreeOfParallelismInvalid(
        int maxDegreeOfParallelism,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(maxDegreeOfParallelism))] string? paramName = null)
    {
        if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(
                    CultureInfo.CurrentCulture,
                    CollectionsResourceStrings.Arg_OutOfRange_MerkleParallelism,
                    maxDegreeOfParallelism));
        }
    }
}
