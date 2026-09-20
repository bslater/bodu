// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Numerics;
using System.Security.Cryptography;

#if SECURITY_CRYPTOGRAPHY
namespace Bodu.Security.Cryptography;
#else
namespace Bodu.Collections.Specialized;
#endif

/// <summary>
/// Provides the stateless RFC 6962 primitives — the split point, the Merkle Tree Hash, the authentication-path and
/// consistency walks, the block arithmetic, and the streaming fold — behind the public <c>Rfc6962MerkleTree</c> and
/// <c>MerkleBlocks</c> facades.
/// </summary>
/// <remarks>
/// <para>
/// This file lives in <c>Bodu.Collections/shared/</c> so it can be source-compiled into an assembly that must not take
/// a package dependency, following the <c>Bodu.IO.Hashing/shared</c> pattern. The consuming project selects the
/// namespace with the <c>SECURITY_CRYPTOGRAPHY</c> symbol; with no symbol it compiles into <c>Bodu.Collections</c>. Its
/// only outside dependency is <c>Bodu.Core</c>'s <c>ThrowHelper</c> and the BCL.
/// </para>
/// <para>
/// Every member is <see langword="static" /> and parameter-driven: callers supply the hash algorithm and its digest
/// length rather than the core holding either, so one instance of anything is never shared across threads by this code.
/// Nothing here reads a resource file, so argument failures throw without a message — shared source carries no
/// resources, and the facades perform the message-bearing validation before delegating. The guards that remain here are
/// backstops against a caller that skipped that validation, not the primary contract.
/// </para>
/// <para>
/// <strong>What is deliberately not here.</strong> The level-by-level reduction used by <c>MerkleTreeHash</c> and
/// <c>ParallelMerkleTreeHash</c> is a different tree and is not expressed through these primitives. Only the
/// domain-separation prefixes are common to both, and those live in <c>MerkleTreeFormat</c> alongside this file.
/// </para>
/// </remarks>
internal static class MerkleTreeCore
{
    /// <summary>The width, in bytes, of the big-endian value bound into a root.</summary>
    internal const int BoundValueLength = sizeof(ulong);

    /// <summary>The greatest number of blocks buffered per batch, whatever degree of parallelism is requested.</summary>
    /// <remarks>
    /// Buffering is <c>batchSize × (blockSize + 1)</c> bytes, so an unreasonably large requested degree would otherwise
    /// translate directly into an unreasonably large allocation. Capping the batch costs nothing: the requested degree
    /// still reaches <see cref="ParallelOptions" />, and a degree above this cap simply has fewer blocks available to
    /// work on at once.
    /// </remarks>
    internal const int MaximumBatchSize = 256;

    /// <summary>
    /// Returns the largest power of two strictly less than <paramref name="count" /> — RFC 6962's split point.
    /// </summary>
    /// <param name="count">The number of entries in the subtree being split. Must be greater than one.</param>
    /// <returns>The number of entries belonging to the perfect left subtree.</returns>
    /// <remarks>
    /// This is deliberately not a halving. For seven entries the split is 4 + 3, not 3 + 4 or 4 + 4; a tree built by
    /// halving has the same leaves and a different root.
    /// </remarks>
    internal static int SplitPoint(int count)
    {
        int split = 1;
        while (split * 2 < count)
            split *= 2;

        return split;
    }

    /// <summary>
    /// Returns the largest number of steps any authentication path in a tree of the given size can carry.
    /// </summary>
    /// <param name="treeSize">The number of entries in the tree.</param>
    /// <returns><c>ceil(log2(treeSize))</c>, or zero for a tree of one entry or fewer.</returns>
    /// <remarks>
    /// This is an upper bound across <em>all</em> leaf indices, and is deliberately not the length expected of any
    /// particular index. Path length varies by index — in a seven-leaf tree leaves 0 to 5 have three steps and leaf 6
    /// has two — so a guard tightened to a per-index length would reject valid proofs. Only a path longer than this
    /// bound can be discarded before it is walked; a path that is too short is caught by the walk itself.
    /// </remarks>
    internal static int MaximumPathLength(long treeSize) =>
        treeSize <= 1 ? 0 : 64 - BitOperations.LeadingZeroCount((ulong)(treeSize - 1));

    /// <summary>
    /// Returns whether a value is an exact power of two.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> when <paramref name="value" /> is a positive power of two.</returns>
    internal static bool IsPowerOfTwo(long value) => value > 0 && (value & (value - 1)) == 0;

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
    /// Returns the number of blocks an input of the given length divides into.
    /// </summary>
    /// <param name="inputLength">The total length, in bytes, of the input.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <returns>
    /// Zero when <paramref name="inputLength" /> is zero, otherwise <c>ceil(inputLength / blockSize)</c>.
    /// </returns>
    internal static long BlockCount(long inputLength, int blockSize) => (inputLength + blockSize - 1) / blockSize;

    /// <summary>
    /// Returns the byte offset at which a block begins, in 64-bit arithmetic.
    /// </summary>
    /// <param name="blockIndex">The zero-based index of the block.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <returns>The offset, in bytes, from the start of the input.</returns>
    internal static long BlockOffset(long blockIndex, int blockSize) => blockIndex * blockSize;

    /// <summary>
    /// Returns the length of a block, which is short only for the final block of an input whose length is not a whole
    /// multiple of the block size.
    /// </summary>
    /// <param name="inputLength">The total length, in bytes, of the input.</param>
    /// <param name="blockIndex">The zero-based index of the block.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <returns>The block's length in bytes; zero when it begins at or beyond the input.</returns>
    internal static int BlockLength(long inputLength, long blockIndex, int blockSize)
    {
        long offset = blockIndex * blockSize;
        return offset >= inputLength ? 0 : (int)Math.Min(blockSize, inputLength - offset);
    }

    /// <summary>
    /// Computes <c>H(prefix || first || second)</c> using a single pooled buffer.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="prefix">The domain-separation prefix byte.</param>
    /// <param name="first">The first payload segment.</param>
    /// <param name="second">The second payload segment, empty when the payload has only one.</param>
    /// <returns>The resulting hash.</returns>
    /// <remarks>
    /// The one-shot <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" /> resets the
    /// algorithm's state on every call, so one instance serves an entire computation without per-node allocation.
    /// </remarks>
    internal static byte[] HashWithPrefix(
        HashAlgorithm hasher,
        int hashLength,
        byte prefix,
        ReadOnlySpan<byte> first,
        ReadOnlySpan<byte> second = default)
    {
        int total = 1 + first.Length + second.Length;
        byte[] rented = ArrayPool<byte>.Shared.Rent(total);
        try
        {
            rented[0] = prefix;
            first.CopyTo(rented.AsSpan(1));
            second.CopyTo(rented.AsSpan(1 + first.Length));

            return HashBuffer(hasher, hashLength, rented.AsSpan(0, total));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }

    /// <summary>
    /// Hashes a payload that already carries its domain-separation prefix at index zero.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="prefixedPayload">The prefix byte followed by the node's payload.</param>
    /// <returns>The resulting hash.</returns>
    /// <exception cref="CryptographicException">
    /// The algorithm could not write its digest into a destination sized from its own reported length, meaning it
    /// contradicted itself.
    /// </exception>
    internal static byte[] HashBuffer(HashAlgorithm hasher, int hashLength, ReadOnlySpan<byte> prefixedPayload)
    {
        byte[] result = new byte[hashLength];
        if (!hasher.TryComputeHash(prefixedPayload, result, out int written))
            throw new CryptographicException();

        if (written == result.Length)
            return result;

        byte[] trimmed = new byte[written];
        Buffer.BlockCopy(result, 0, trimmed, 0, written);
        return trimmed;
    }

    /// <summary>
    /// Computes the empty tree's root, <c>H()</c>.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <returns>The hash of zero bytes.</returns>
    internal static byte[] HashEmpty(HashAlgorithm hasher, int hashLength) =>
        HashBuffer(hasher, hashLength, []);

    /// <summary>
    /// Computes the Merkle Tree Hash over a non-empty span of leaf hashes.
    /// </summary>
    /// <param name="leafHashes">The leaf hashes, in order. Must not be empty.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <returns>The subtree's root.</returns>
    /// <remarks>
    /// Recursion depth is logarithmic in the entry count, so no stack guard is required. A single leaf's hash is
    /// returned unchanged — a subtree root is promoted, never re-hashed, which is the single point on which this
    /// construction differs from a level-by-level reduction.
    /// </remarks>
    internal static byte[] Mth(ReadOnlySpan<byte[]> leafHashes, HashAlgorithm hasher, int hashLength)
    {
        if (leafHashes.Length == 1)
            return leafHashes[0];

        int split = SplitPoint(leafHashes.Length);
        return HashWithPrefix(
            hasher,
            hashLength,
            MerkleTreeFormat.InternalNodePrefix,
            Mth(leafHashes[..split], hasher, hashLength),
            Mth(leafHashes[split..], hasher, hashLength));
    }

    /// <summary>
    /// Appends the sibling subtree roots on the way from a leaf to the root, leaf-upward.
    /// </summary>
    /// <param name="leafHashes">The subtree's leaf hashes.</param>
    /// <param name="index">The index within this subtree of the leaf being proved.</param>
    /// <param name="path">The path being built.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    internal static void AppendPath(
        ReadOnlySpan<byte[]> leafHashes,
        int index,
        List<byte[]> path,
        HashAlgorithm hasher,
        int hashLength)
    {
        if (leafHashes.Length <= 1)
            return;

        int split = SplitPoint(leafHashes.Length);
        if (index < split)
        {
            AppendPath(leafHashes[..split], index, path, hasher, hashLength);
            path.Add(Mth(leafHashes[split..], hasher, hashLength));
        }
        else
        {
            AppendPath(leafHashes[split..], index - split, path, hasher, hashLength);
            path.Add(Mth(leafHashes[..split], hasher, hashLength));
        }
    }

    /// <summary>
    /// Appends the subproof for a prefix of <paramref name="leafHashes" />, per RFC 6962 §2.1.
    /// </summary>
    /// <param name="leafHashes">The subtree's leaf hashes.</param>
    /// <param name="first">The prefix length within this subtree.</param>
    /// <param name="onBoundary">
    /// Whether the prefix ends exactly on this subtree's boundary, in which case its root is already implied and is not
    /// carried in the proof.
    /// </param>
    /// <param name="proof">The proof being built.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    internal static void AppendSubProof(
        ReadOnlySpan<byte[]> leafHashes,
        int first,
        bool onBoundary,
        List<byte[]> proof,
        HashAlgorithm hasher,
        int hashLength)
    {
        if (first == leafHashes.Length)
        {
            if (!onBoundary)
                proof.Add(Mth(leafHashes, hasher, hashLength));

            return;
        }

        int split = SplitPoint(leafHashes.Length);
        if (first <= split)
        {
            AppendSubProof(leafHashes[..split], first, onBoundary, proof, hasher, hashLength);
            proof.Add(Mth(leafHashes[split..], hasher, hashLength));
        }
        else
        {
            AppendSubProof(leafHashes[split..], first - split, onBoundary: false, proof, hasher, hashLength);
            proof.Add(Mth(leafHashes[..split], hasher, hashLength));
        }
    }

    /// <summary>
    /// Walks an authentication path from a leaf hash to the tree head, following RFC 6962 §2.1.1.
    /// </summary>
    /// <param name="treeSize">The number of entries the tree is claimed to hold.</param>
    /// <param name="leafIndex">The zero-based index the leaf is claimed to occupy.</param>
    /// <param name="leafHash">The leaf's hash.</param>
    /// <param name="path">The authentication path, leaf-upward.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <returns>The computed head, or <see langword="null" /> when the proof is structurally invalid.</returns>
    /// <remarks>
    /// <para>
    /// The <c>sn</c> bookkeeping alone rejects a path that is too short or too long; the only length check applied
    /// before the walk is the strict upper bound of <see cref="MaximumPathLength(long)" />, which cannot reject a valid
    /// proof.
    /// </para>
    /// <para>
    /// The inner shift loop terminates on <c>sn = 0</c>, which is RFC 6962 §2.1.1's own wording. Note that §2.1.2's
    /// consistency walk terminates the equivalent loop on <c>fn = 0</c> instead; that asymmetry is the standard's own
    /// and both are implemented verbatim, so do not harmonize them.
    /// </para>
    /// </remarks>
    internal static byte[]? WalkToHead(
        long treeSize,
        long leafIndex,
        ReadOnlySpan<byte> leafHash,
        IReadOnlyList<ReadOnlyMemory<byte>> path,
        HashAlgorithm hasher,
        int hashLength)
    {
        if (treeSize <= 0 || leafIndex < 0 || leafIndex >= treeSize)
            return null;

        if (path.Count > MaximumPathLength(treeSize))
            return null;

        ulong fn = (ulong)leafIndex;
        ulong sn = (ulong)(treeSize - 1);
        byte[] running = leafHash.ToArray();

        for (int step = 0; step < path.Count; step++)
        {
            ReadOnlySpan<byte> sibling = path[step].Span;
            if (sn == 0 || sibling.Length != hashLength)
                return null;

            if ((fn & 1) == 1 || fn == sn)
            {
                running = HashWithPrefix(hasher, hashLength, MerkleTreeFormat.InternalNodePrefix, sibling, running);

                if ((fn & 1) == 0)
                {
                    while ((fn & 1) == 0 && sn != 0)
                    {
                        fn >>= 1;
                        sn >>= 1;
                    }
                }
            }
            else
            {
                running = HashWithPrefix(hasher, hashLength, MerkleTreeFormat.InternalNodePrefix, running, sibling);
            }

            fn >>= 1;
            sn >>= 1;
        }

        return sn == 0 ? running : null;
    }

    /// <summary>
    /// Replaces the top two entries of a pending-subtree stack with their parent node.
    /// </summary>
    /// <param name="pending">The pending subtree roots.</param>
    /// <param name="pendingLeafCounts">The number of leaves each pending root covers.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    internal static void MergeTopTwo(
        List<byte[]> pending,
        List<long> pendingLeafCounts,
        HashAlgorithm hasher,
        int hashLength)
    {
        int last = pending.Count - 1;

        pending[last - 1] = HashWithPrefix(
            hasher,
            hashLength,
            MerkleTreeFormat.InternalNodePrefix,
            pending[last - 1],
            pending[last]);
        pendingLeafCounts[last - 1] += pendingLeafCounts[last];

        pending.RemoveAt(last);
        pendingLeafCounts.RemoveAt(last);
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
            rented[0] = MerkleTreeFormat.LeafPrefix;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int filled = 0;
                while (filled < blockSize)
                {
                    int read = source.Read(rented, 1 + filled, blockSize - filled);
                    if (read <= 0)
                        break;

                    filled += read;
                }

                if (filled == 0)
                    break;

                inputLength += filled;
                onLeafHash(HashBuffer(hasher, hashLength, rented.AsSpan(0, 1 + filled)));

                // A partially-filled block can only be the last: the inner loop exits early solely on end of stream.
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
}
