// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTree.Parallel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

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
    /// measure your own case; the benchmark under <c>Bodu.Security.Cryptography/bench</c> reproduces them.
    /// </para>
    /// <para>
    /// The returned root is bit-identical to the sequential overload's for every input, so switching between them is
    /// never a compatibility question — only a performance one. A leaf algorithm that faults surfaces its exception as
    /// itself, not wrapped in an <see cref="AggregateException" />.
    /// </para>
    /// </remarks>
    public MerkleBlockComputation ComputeBlockedParallel(
        Stream source,
        int blockSize,
        int maxDegreeOfParallelism = -1,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        MerkleBlocks.ThrowIfBlockSizeInvalid(blockSize);
        ThrowIfDegreeOfParallelismInvalid(maxDegreeOfParallelism);

        List<byte[]> leafHashes = [];
        long inputLength = MerkleTreeCore.ForEachLeafHashParallel(
            source, blockSize, CreateAlgorithm, HashLength, maxDegreeOfParallelism, leafHashes.Add, cancellationToken);

        using HashAlgorithm reducer = CreateAlgorithm();
        byte[] root = leafHashes.Count == 0
            ? HashEmpty(reducer)
            : Mth(CollectionsMarshal.AsSpan(leafHashes), reducer);

        return new MerkleBlockComputation(root, inputLength, blockSize, leafHashes);
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
    public MerkleBlockComputation ComputeBlockedParallel(
        ReadOnlyMemory<byte> source,
        int blockSize,
        int maxDegreeOfParallelism = -1,
        CancellationToken cancellationToken = default)
    {
        MerkleBlocks.ThrowIfBlockSizeInvalid(blockSize);
        ThrowIfDegreeOfParallelismInvalid(maxDegreeOfParallelism);

        byte[][] leafHashes = MerkleTreeCore.HashLeavesParallel(
            source, blockSize, CreateAlgorithm, HashLength, maxDegreeOfParallelism, cancellationToken);

        using HashAlgorithm reducer = CreateAlgorithm();
        byte[] root = leafHashes.Length == 0 ? HashEmpty(reducer) : Mth(leafHashes, reducer);

        return new MerkleBlockComputation(root, source.Length, blockSize, leafHashes);
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

        byte[][] leafHashes = MerkleTreeCore.HashLeavesParallel(
            entries, CreateAlgorithm, HashLength, maxDegreeOfParallelism, cancellationToken);

        using HashAlgorithm reducer = CreateAlgorithm();
        return leafHashes.Length == 0 ? HashEmpty(reducer) : Mth(leafHashes, reducer);
    }

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
                    CryptoResourceStrings.Arg_OutOfRange_MerkleParallelism,
                    maxDegreeOfParallelism));
        }
    }
}
