// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTree.Blocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Collections.Generic;

/// <summary>
/// Block mode: roots over fixed-size blocks of a byte stream, in a single forward pass.
/// </summary>
public sealed partial class Rfc6962MerkleTree
{
    /// <summary>
    /// Computes the root over fixed-size blocks of a stream and returns it together with the ordered leaf hashes, so
    /// one pass serves both publishing a root and answering authentication paths.
    /// </summary>
    /// <param name="source">The stream to read to its end. Must be readable.</param>
    /// <param name="blockSize">The size, in bytes, of each block — the chunk one leaf covers.</param>
    /// <param name="cancellationToken">A token observed between blocks.</param>
    /// <returns>The computation's root, input length, block size and leaf hashes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// <para>
    /// The stream is read once, forward only, and never buffered in full: at most <c>blockSize + 1</c> bytes of input
    /// are held at a time. Retaining the leaf hashes costs <c>leafCount × <see cref="HashLength" /></c> bytes — 16 KiB
    /// for a 512 MiB input at one-mebibyte blocks. When only the root is wanted, prefer
    /// <see cref="ComputeRootOfBlocks(Stream, int, CancellationToken)" />, which holds a logarithmic number of hashes
    /// instead.
    /// </para>
    /// <para>
    /// A zero-length stream yields zero leaves and the empty tree's root, not one empty leaf. A final short block is
    /// hashed at its actual length and never padded.
    /// </para>
    /// </remarks>
    public MerkleComputation ComputeBlocked(
        Stream source,
        int blockSize,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        MerkleBlocks.ThrowIfBlockSizeInvalid(blockSize);

        using HashAlgorithm hasher = CreateAlgorithm();

        List<byte[]> leafHashes = [];
        long inputLength = ForEachLeafHash(source, blockSize, hasher, leafHashes.Add, cancellationToken);

        byte[] root = leafHashes.Count == 0
            ? HashEmpty(hasher)
            : Mth(CollectionsMarshal.AsSpan(leafHashes), hasher);

        return new MerkleComputation(root, inputLength, blockSize, leafHashes);
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a stream while holding only a logarithmic number of hashes.
    /// </summary>
    /// <param name="source">The stream to read to its end. Must be readable.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="cancellationToken">A token observed between blocks.</param>
    /// <returns>The tree's root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was cancelled.</exception>
    /// <remarks>
    /// <para>
    /// Peak memory is <c>O(blockSize + log n × <see cref="HashLength" />)</c>: the read buffer, plus one pending
    /// subtree root per set bit in the leaf count so far. The root is identical to
    /// <see cref="ComputeBlocked(Stream, int, CancellationToken)" />'s — this overload simply cannot produce an
    /// authentication path afterwards, because it does not keep the leaf hashes.
    /// </para>
    /// <para>
    /// Leaves are folded as they arrive: each new leaf is pushed as a one-leaf subtree, and while the top two pending
    /// subtrees cover the same number of leaves they are combined. Every pending entry is therefore a <em>perfect</em>
    /// subtree of strictly decreasing size, and combining them right to left at the end reproduces RFC 6962's shape —
    /// left subtree perfect, right holding the remainder — without ever having held the whole tree.
    /// </para>
    /// </remarks>
    public byte[] ComputeRootOfBlocks(
        Stream source,
        int blockSize,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        MerkleBlocks.ThrowIfBlockSizeInvalid(blockSize);

        using HashAlgorithm hasher = CreateAlgorithm();

        List<byte[]> pending = [];
        List<long> pendingLeafCounts = [];

        _ = ForEachLeafHash(
            source,
            blockSize,
            hasher,
            leafHash =>
            {
                pending.Add(leafHash);
                pendingLeafCounts.Add(1);

                while (pending.Count >= 2 && pendingLeafCounts[^1] == pendingLeafCounts[^2])
                    MergeTopTwo(pending, pendingLeafCounts, hasher);
            },
            cancellationToken);

        if (pending.Count == 0)
            return HashEmpty(hasher);

        while (pending.Count > 1)
            MergeTopTwo(pending, pendingLeafCounts, hasher);

        return pending[0];
    }

    /// <summary>
    /// Computes the root over fixed-size blocks of a buffer already in memory, together with its leaf hashes.
    /// </summary>
    /// <param name="source">The bytes to divide into blocks.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <returns>The computation's root, input length, block size and leaf hashes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Equivalent to <see cref="ComputeBlocked(Stream, int, CancellationToken)" /> over the same bytes; provided so a
    /// caller holding a buffer need not wrap it in a <see cref="MemoryStream" />.
    /// </remarks>
    public MerkleComputation ComputeBlocked(ReadOnlySpan<byte> source, int blockSize)
    {
        MerkleBlocks.ThrowIfBlockSizeInvalid(blockSize);

        using HashAlgorithm hasher = CreateAlgorithm();
        long count = MerkleBlocks.BlockCount(source.Length, blockSize);

        byte[][] leafHashes = new byte[count][];
        for (long index = 0; index < count; index++)
        {
            int offset = (int)MerkleBlocks.BlockOffset(index, blockSize);
            int length = MerkleBlocks.BlockLength(source.Length, index, blockSize);
            leafHashes[index] = HashWithPrefix(hasher, MerkleTreeFormat.LeafPrefix, source.Slice(offset, length));
        }

        byte[] root = count == 0 ? HashEmpty(hasher) : Mth(leafHashes, hasher);

        return new MerkleComputation(root, source.Length, blockSize, leafHashes);
    }

    /// <summary>
    /// Replaces the top two entries of the pending-subtree stack with their parent node.
    /// </summary>
    /// <param name="pending">The pending subtree roots.</param>
    /// <param name="pendingLeafCounts">The number of leaves each pending root covers.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    private void MergeTopTwo(List<byte[]> pending, List<long> pendingLeafCounts, HashAlgorithm hasher) =>
        MerkleTreeCore.MergeTopTwo(pending, pendingLeafCounts, hasher, HashLength);

    /// <summary>
    /// Reads <paramref name="source" /> forward in <paramref name="blockSize" />-byte blocks, invoking
    /// <paramref name="onLeafHash" /> with each block's leaf hash in order.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="onLeafHash">Invoked once per block, in block order.</param>
    /// <param name="cancellationToken">A token observed between blocks.</param>
    /// <returns>The total number of bytes read.</returns>
    /// <remarks>
    /// The rented buffer holds the leaf-domain prefix at index zero and the block's bytes from index one, so each block
    /// is hashed straight out of the buffer it was read into and the stream's bytes are never copied again. A short
    /// read is topped up rather than taken as the end of the stream, which a network, cryptographic or decompression
    /// stream requires — only a read returning zero ends the loop.
    /// </remarks>
    private long ForEachLeafHash(
        Stream source,
        int blockSize,
        HashAlgorithm hasher,
        Action<byte[]> onLeafHash,
        CancellationToken cancellationToken) =>
        MerkleTreeCore.ForEachLeafHash(source, blockSize, hasher, HashLength, onLeafHash, cancellationToken);
}
