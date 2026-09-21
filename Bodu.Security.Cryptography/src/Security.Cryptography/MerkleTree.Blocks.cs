// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTree.Blocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Block mode: roots over fixed-size blocks of a byte stream or buffer, in a single forward pass, with the leaves
/// hashed on the calling thread or by parallel workers as the instance is configured.
/// </summary>
/// <remarks>
/// <para>
/// A stream is read sequentially — it cannot be read out of order — so a parallel instance reads it into a batch of
/// buffers and hashes the batch concurrently; peak input buffering is
/// <c>min(maxDegreeOfParallelism, 256) × (blockSize + 1)</c> bytes. Copying each block off the stream happens on the
/// calling thread, which caps the achievable speedup; a buffer already in memory has every block sliced and hashed
/// inside its own worker, so the in-memory overloads scale considerably better. Measured over 64 MiB at one-mebibyte
/// blocks on four cores, the stream path returned 2.2× to 2.6× over the sequential fold and the in-memory path 3.1× to
/// 3.4×, across SHA-256, SHA-512, Tiger and BLAKE2b alike; the benchmark under <c>Bodu.Security.Cryptography/bench</c>
/// reproduces them. The root is bit-identical whichever path and whichever degree produced it.
/// </para>
/// <para>
/// A leaf algorithm that faults inside a worker surfaces its exception as itself, never wrapped in an
/// <see cref="AggregateException" />, and cancellation always surfaces as <see cref="OperationCanceledException" />.
/// </para>
/// </remarks>
public sealed partial class MerkleTree
{
    /// <summary>
    /// Computes the root over fixed-size blocks of a stream and returns it together with the ordered leaf hashes, so
    /// one pass serves both publishing a root and answering authentication paths.
    /// </summary>
    /// <param name="source">The stream to read to its end. Must be readable.</param>
    /// <param name="blockSize">The size, in bytes, of each block — the chunk one leaf covers.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <param name="cancellationToken">A token observed between blocks.</param>
    /// <returns>The computation's root, input length, block size and leaf hashes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// <para>
    /// The stream is read once, forward only, and never buffered in full. Retaining the leaf hashes costs
    /// <c>leafCount × <see cref="HashLength" /></c> bytes — 16 KiB for a 512 MiB input at one-mebibyte blocks. When
    /// only the root is wanted, prefer
    /// <see cref="ComputeRootOfBlocks(Stream, int, MerkleTreeDiagnostics, CancellationToken)" />, which holds a
    /// logarithmic number of hashes instead.
    /// </para>
    /// <para>
    /// A zero-length stream yields zero leaves and the empty tree's root, not one empty leaf. A final short block is
    /// hashed at its actual length and never padded.
    /// </para>
    /// </remarks>
    public MerkleBlockComputation ComputeBlocked(
        Stream source,
        int blockSize,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowIfBlockSizeInvalid(blockSize);

        using HashAlgorithm hasher = CreateAlgorithm();

        List<byte[]> leafHashes = [];
        long inputLength = IsParallel
            ? ForEachLeafHashParallel(source, blockSize, CreateAlgorithm, HashLength, MaxDegreeOfParallelism, leafHashes.Add, cancellationToken)
            : ForEachLeafHash(source, blockSize, hasher, HashLength, leafHashes.Add, cancellationToken);

        byte[] root = Reduce(CollectionsMarshal.AsSpan(leafHashes), hasher, diagnostics);
        return new MerkleBlockComputation(root, inputLength, blockSize, leafHashes);
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a stream asynchronously and returns it together with the ordered
    /// leaf hashes.
    /// </summary>
    /// <param name="source">The stream to read to its end. Must be readable.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <param name="cancellationToken">A token observed between blocks and by every read.</param>
    /// <returns>A task whose result is the computation's root, input length, block size and leaf hashes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// Reads are awaited, so the calling thread is free while the stream is slow; leaf hashing happens on the
    /// continuation thread, or on parallel workers when the instance is configured for them. The result is the same
    /// <see cref="ComputeBlocked(Stream, int, MerkleTreeDiagnostics, CancellationToken)" /> returns.
    /// </remarks>
    public async Task<MerkleBlockComputation> ComputeBlockedAsync(
        Stream source,
        int blockSize,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowIfBlockSizeInvalid(blockSize);

        using HashAlgorithm hasher = CreateAlgorithm();

        List<byte[]> leafHashes = [];
        long inputLength;
        try
        {
            inputLength = IsParallel
                ? await ForEachLeafHashParallelAsync(source, blockSize, CreateAlgorithm, HashLength, MaxDegreeOfParallelism, leafHashes.Add, cancellationToken).ConfigureAwait(false)
                : await ForEachLeafHashAsync(source, blockSize, hasher, HashLength, leafHashes.Add, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw Normalize(ex, cancellationToken);
        }

        byte[] root = Reduce(CollectionsMarshal.AsSpan(leafHashes), hasher, diagnostics);
        return new MerkleBlockComputation(root, inputLength, blockSize, leafHashes);
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a buffer already in memory, together with its leaf hashes.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <param name="cancellationToken">A token observed while hashing.</param>
    /// <returns>The computation's root, input length, block size and leaf hashes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// The overload to prefer on a parallel instance when the bytes are already in memory: every block is sliced and
    /// hashed inside its own worker, so nothing is copied and the whole per-block cost parallelizes.
    /// </remarks>
    public MerkleBlockComputation ComputeBlocked(
        ReadOnlyMemory<byte> source,
        int blockSize,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfBlockSizeInvalid(blockSize);

        using HashAlgorithm hasher = CreateAlgorithm();
        byte[][] leafHashes = IsParallel
            ? HashLeavesParallel(source, blockSize, CreateAlgorithm, HashLength, MaxDegreeOfParallelism, cancellationToken)
            : HashBlocks(source.Span, blockSize, hasher, cancellationToken);

        return new MerkleBlockComputation(Reduce(leafHashes, hasher, diagnostics), source.Length, blockSize, leafHashes);
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a buffer already in memory, together with its leaf hashes.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <returns>The computation's root, input length, block size and leaf hashes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Equivalent to the <see cref="ReadOnlyMemory{T}" /> overload; on a parallel instance the span is copied into a
    /// pooled buffer first, because a span cannot be captured by workers, so prefer that overload when a memory is at
    /// hand.
    /// </remarks>
    public MerkleBlockComputation ComputeBlocked(ReadOnlySpan<byte> source, int blockSize, MerkleTreeDiagnostics? diagnostics = null)
    {
        ThrowIfBlockSizeInvalid(blockSize);

        if (IsParallel)
            return WithPooledCopy(source, (memory, tree) => tree.ComputeBlocked(memory, blockSize, diagnostics));

        using HashAlgorithm hasher = CreateAlgorithm();
        byte[][] leafHashes = HashBlocks(source, blockSize, hasher, CancellationToken.None);
        return new MerkleBlockComputation(Reduce(leafHashes, hasher, diagnostics), source.Length, blockSize, leafHashes);
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of an array, together with its leaf hashes.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <param name="cancellationToken">A token observed while hashing.</param>
    /// <returns>The computation's root, input length, block size and leaf hashes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    public MerkleBlockComputation ComputeBlocked(
        byte[] source,
        int blockSize,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);

        return ComputeBlocked(source.AsMemory(), blockSize, diagnostics, cancellationToken);
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a stream while holding only a logarithmic number of hashes.
    /// </summary>
    /// <param name="source">The stream to read to its end. Must be readable.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <param name="cancellationToken">A token observed between blocks.</param>
    /// <returns>The tree's root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// <para>
    /// Peak memory is <c>O(blockSize + log n × <see cref="HashLength" />)</c> on a sequential instance: the read
    /// buffer, plus at most <c>fanOut − 1</c> pending nodes per level. A parallel instance adds the batch of block
    /// buffers. The root is identical to
    /// <see cref="ComputeBlocked(Stream, int, MerkleTreeDiagnostics, CancellationToken)" />'s — this overload simply
    /// cannot produce an authentication path afterwards, because it does not keep the leaf hashes.
    /// </para>
    /// <para>
    /// Leaves are folded as they arrive, level by level: a level holds at most <c>fanOut − 1</c> pending nodes, and the
    /// moment it fills the group is hashed and the parent carried up. At the end a lone node is promoted unchanged,
    /// never re-hashed — which is what reproduces RFC 6962's shape at a fan-out of two without ever having held the
    /// whole tree. Recording into <paramref name="diagnostics" /> retains one entry per node, so the memory bound does
    /// not hold while a recorder is supplied.
    /// </para>
    /// </remarks>
    public byte[] ComputeRootOfBlocks(
        Stream source,
        int blockSize,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowIfBlockSizeInvalid(blockSize);

        using HashAlgorithm hasher = CreateAlgorithm();
        var fold = CreateFold(hasher, diagnostics);

        if (IsParallel)
            _ = ForEachLeafHashParallel(source, blockSize, CreateAlgorithm, HashLength, MaxDegreeOfParallelism, fold.Add, cancellationToken);
        else
            _ = ForEachLeafHash(source, blockSize, hasher, HashLength, fold.Add, cancellationToken);

        return fold.Finish();
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a stream asynchronously while holding only a logarithmic number of
    /// hashes.
    /// </summary>
    /// <param name="source">The stream to read to its end. Must be readable.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <param name="cancellationToken">A token observed between blocks and by every read.</param>
    /// <returns>A task whose result is the tree's root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// The asynchronous twin of
    /// <see cref="ComputeRootOfBlocks(Stream, int, MerkleTreeDiagnostics, CancellationToken)" />: reads are awaited,
    /// the fold runs on the continuation thread, and a parallel instance hashes each batch of leaves on workers before
    /// folding.
    /// </remarks>
    public async Task<byte[]> ComputeRootOfBlocksAsync(
        Stream source,
        int blockSize,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowIfBlockSizeInvalid(blockSize);

        using HashAlgorithm hasher = CreateAlgorithm();
        var fold = CreateFold(hasher, diagnostics);

        try
        {
            if (IsParallel)
                _ = await ForEachLeafHashParallelAsync(source, blockSize, CreateAlgorithm, HashLength, MaxDegreeOfParallelism, fold.Add, cancellationToken).ConfigureAwait(false);
            else
                _ = await ForEachLeafHashAsync(source, blockSize, hasher, HashLength, fold.Add, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw Normalize(ex, cancellationToken);
        }

        return fold.Finish();
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a buffer already in memory.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <param name="cancellationToken">A token observed while hashing.</param>
    /// <returns>The tree's root.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    public byte[] ComputeRootOfBlocks(
        ReadOnlyMemory<byte> source,
        int blockSize,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfBlockSizeInvalid(blockSize);

        using HashAlgorithm hasher = CreateAlgorithm();
        if (IsParallel)
            return Reduce(HashLeavesParallel(source, blockSize, CreateAlgorithm, HashLength, MaxDegreeOfParallelism, cancellationToken), hasher, diagnostics);

        return FoldBlocks(source.Span, blockSize, hasher, diagnostics, cancellationToken);
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a buffer already in memory.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <returns>The tree's root.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// On a sequential instance each block is hashed straight out of the span and folded at once, holding a logarithmic
    /// number of hashes; on a parallel instance the span is copied into a pooled buffer first, so prefer the
    /// <see cref="ReadOnlyMemory{T}" /> overload there.
    /// </remarks>
    public byte[] ComputeRootOfBlocks(ReadOnlySpan<byte> source, int blockSize, MerkleTreeDiagnostics? diagnostics = null)
    {
        ThrowIfBlockSizeInvalid(blockSize);

        if (IsParallel)
            return WithPooledCopy(source, (memory, tree) => tree.ComputeRootOfBlocks(memory, blockSize, diagnostics));

        using HashAlgorithm hasher = CreateAlgorithm();
        return FoldBlocks(source, blockSize, hasher, diagnostics, CancellationToken.None);
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of an array.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <param name="cancellationToken">A token observed while hashing.</param>
    /// <returns>The tree's root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    public byte[] ComputeRootOfBlocks(
        byte[] source,
        int blockSize,
        MerkleTreeDiagnostics? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);

        return ComputeRootOfBlocks(source.AsMemory(), blockSize, diagnostics, cancellationToken);
    }

    /// <summary>
    /// Creates an accumulator that builds the root over fixed-size blocks from bytes appended as they are written, so a
    /// writer can feed the tree from the same calls that feed its flat digest.
    /// </summary>
    /// <param name="blockSize">The size, in bytes, of each block — the chunk one leaf covers.</param>
    /// <param name="retainLeafHashes">
    /// <see langword="true" /> to keep every leaf hash so the accumulator can return a
    /// <see cref="MerkleBlockComputation" /> for authentication paths; <see langword="false" /> to hold only a
    /// logarithmic number of hashes.
    /// </param>
    /// <param name="diagnostics">
    /// The recorder that receives the tree's nodes as they are produced, or <see langword="null" /> to record nothing.
    /// </param>
    /// <returns>A new accumulator owning one algorithm from this tree's factory.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="InvalidOperationException">The algorithm factory returned <see langword="null" />.</exception>
    /// <remarks>
    /// The accumulator's root is identical to
    /// <see cref="ComputeRootOfBlocks(Stream, int, MerkleTreeDiagnostics, CancellationToken)" />'s over the same bytes
    /// at this instance's fan-out, and its <see cref="MerkleBlockAccumulator.FinishBound" /> is <see cref="BindRoot" />
    /// of that root and the byte length — the published shape for a possession check. It is sequential by nature: one
    /// writer appends, and each leaf is hashed as its block completes, whatever <see cref="MaxDegreeOfParallelism" />
    /// is.
    /// </remarks>
    public MerkleBlockAccumulator CreateBlockAccumulator(
        int blockSize,
        bool retainLeafHashes = false,
        MerkleTreeDiagnostics? diagnostics = null)
    {
        ThrowIfBlockSizeInvalid(blockSize);

        return new MerkleBlockAccumulator(this, CreateAlgorithm(), blockSize, retainLeafHashes, diagnostics);
    }

    /// <summary>
    /// Hashes every block of a span on the calling thread, one leaf hash per block index.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="cancellationToken">A token observed between blocks.</param>
    /// <returns>The leaf hashes in block order; empty for an empty span.</returns>
    private byte[][] HashBlocks(ReadOnlySpan<byte> source, int blockSize, HashAlgorithm hasher, CancellationToken cancellationToken)
    {
        long count = BlockCount(source.Length, blockSize);
        byte[][] leafHashes = new byte[count][];
        for (long index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int offset = (int)BlockOffset(index, blockSize);
            int length = BlockLength(source.Length, index, blockSize);
            leafHashes[index] = HashWithPrefix(hasher, HashLength, LeafPrefix, source.Slice(offset, length));
        }

        return leafHashes;
    }

    /// <summary>
    /// Hashes every block of a span on the calling thread and folds each leaf at once, holding only the fold's pending
    /// nodes.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="diagnostics">The recorder that receives every leaf and node, or <see langword="null" />.</param>
    /// <param name="cancellationToken">A token observed between blocks.</param>
    /// <returns>The root.</returns>
    private byte[] FoldBlocks(ReadOnlySpan<byte> source, int blockSize, HashAlgorithm hasher, MerkleTreeDiagnostics? diagnostics, CancellationToken cancellationToken)
    {
        var fold = CreateFold(hasher, diagnostics);
        long count = BlockCount(source.Length, blockSize);
        for (long index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int offset = (int)BlockOffset(index, blockSize);
            int length = BlockLength(source.Length, index, blockSize);
            fold.Add(HashWithPrefix(hasher, HashLength, LeafPrefix, source.Slice(offset, length)));
        }

        return fold.Finish();
    }

    /// <summary>
    /// Copies a span into a pooled buffer, runs a computation over it as memory, and returns the buffer cleared.
    /// </summary>
    /// <typeparam name="TResult">The computation's result type.</typeparam>
    /// <param name="source">The bytes to copy.</param>
    /// <param name="compute">The computation to run over the copy.</param>
    /// <returns>The computation's result.</returns>
    /// <remarks>
    /// A span cannot be captured by parallel workers, so the parallel in-memory paths take a memory; this bridges the
    /// span overloads to them at the cost of one copy.
    /// </remarks>
    private TResult WithPooledCopy<TResult>(ReadOnlySpan<byte> source, Func<ReadOnlyMemory<byte>, MerkleTree, TResult> compute)
    {
        byte[] rented = ArrayPool<byte>.Shared.Rent(Math.Max(source.Length, 1));
        try
        {
            source.CopyTo(rented);
            return compute(rented.AsMemory(0, source.Length), this);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }
}
