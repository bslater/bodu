// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTree.Inclusion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Collections.Specialized;

/// <summary>
/// Authentication paths and inclusion-proof verification.
/// </summary>
public sealed partial class Rfc6962MerkleTree
{
    /// <summary>
    /// Produces the authentication path for one entry of a tree: the sibling subtree roots from the leaf upward.
    /// </summary>
    /// <param name="entries">The tree's entries, in order.</param>
    /// <param name="leafIndex">The zero-based index of the entry to prove.</param>
    /// <returns>The path, leaf-upward. Empty for a one-entry tree, whose leaf hash is already the root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entries" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="leafIndex" /> is negative or is not less than the number of entries.
    /// </exception>
    public byte[][] AuthenticationPath(IReadOnlyList<ReadOnlyMemory<byte>> entries, long leafIndex)
    {
        ThrowHelper.ThrowIfNull(entries);

        using HashAlgorithm hasher = CreateAlgorithm();

        byte[][] leafHashes = new byte[entries.Count][];
        for (int index = 0; index < entries.Count; index++)
            leafHashes[index] = HashWithPrefix(hasher, MerkleTreeFormat.LeafPrefix, entries[index].Span);

        return BuildPath(leafHashes, leafIndex, hasher);
    }

    /// <summary>
    /// Produces the authentication path for one leaf from hashes already computed — what a party that streamed a large
    /// input past itself can answer without re-reading it.
    /// </summary>
    /// <param name="leafHashes">The ordered leaf hashes, each <see cref="HashLength" /> bytes long.</param>
    /// <param name="leafIndex">The zero-based index of the leaf to prove.</param>
    /// <returns>The path, leaf-upward.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leafHashes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// An element is <see langword="null" /> or is not <see cref="HashLength" /> bytes long.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="leafIndex" /> is negative or is not less than the number of leaf hashes.
    /// </exception>
    /// <remarks>
    /// The leaf hashes of a streamed computation are available from <see cref="MerkleComputation.LeafHashes" />.
    /// </remarks>
    public byte[][] AuthenticationPath(IReadOnlyList<byte[]> leafHashes, long leafIndex)
    {
        byte[][] copy = ValidateLeafHashes(leafHashes);

        using HashAlgorithm hasher = CreateAlgorithm();
        return BuildPath(copy, leafIndex, hasher);
    }

    /// <summary>
    /// Verifies that an entry occupies the stated position of a tree with the stated root.
    /// </summary>
    /// <param name="root">The trusted root to check against.</param>
    /// <param name="treeSize">The number of entries the tree is claimed to hold.</param>
    /// <param name="leafIndex">The zero-based index the entry is claimed to occupy.</param>
    /// <param name="entry">The entry's bytes, which are hashed as a leaf.</param>
    /// <param name="path">The authentication path, leaf-upward.</param>
    /// <returns>
    /// <see langword="true" /> when the path carries <paramref name="entry" /> to <paramref name="root" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// <strong><paramref name="treeSize" /> is trusted input.</strong> RFC 6962's verifier takes the tree size from its
    /// caller and cannot detect a false one. A caller that obtains the size from the party being examined has <em>no
    /// soundness guarantee</em>: a four-entry tree's first authentication path has exactly the length a three-entry
    /// tree's first path wants and walks to the same head, so this method returns <see langword="true" /> for both. A
    /// holder that has lost its last entry can therefore declare a smaller tree, never be asked for that entry, and
    /// pass every challenge for ever.
    /// </para>
    /// <para>
    /// That behaviour is RFC 6962 working as specified, not a defect here. When the size comes from an untrusted party,
    /// verify against a length-bound root with
    /// <see cref="VerifyInclusionBound(ReadOnlySpan{byte}, long, long, long, ReadOnlySpan{byte}, IReadOnlyList{ReadOnlyMemory{byte}})" />
    /// instead, which fails closed on a size the publisher did not commit to.
    /// </para>
    /// <para>
    /// Every malformed input returns <see langword="false" /> rather than throwing — a wrong index, a path that is too
    /// long or too short, an element of the wrong width, a zero tree size — because a verifier sits directly behind
    /// untrusted input and an exception where a <see langword="false" /> belongs is a denial of service.
    /// </para>
    /// </remarks>
    public bool VerifyInclusion(
        ReadOnlySpan<byte> root,
        long treeSize,
        long leafIndex,
        ReadOnlySpan<byte> entry,
        IReadOnlyList<ReadOnlyMemory<byte>> path)
    {
        ThrowHelper.ThrowIfNull(path);

        if (root.Length != HashLength)
            return false;

        using HashAlgorithm hasher = CreateAlgorithm();
        byte[] leafHash = HashWithPrefix(hasher, MerkleTreeFormat.LeafPrefix, entry);

        byte[]? head = WalkToHead(treeSize, leafIndex, leafHash, path, hasher);
        return head is not null && CryptographicOperations.FixedTimeEquals(head, root);
    }

    /// <summary>
    /// Verifies an inclusion proof from a leaf hash rather than the entry's bytes.
    /// </summary>
    /// <param name="root">The trusted root to check against.</param>
    /// <param name="treeSize">The number of entries the tree is claimed to hold.</param>
    /// <param name="leafIndex">The zero-based index the leaf is claimed to occupy.</param>
    /// <param name="leafHash">The leaf's hash, <see cref="HashLength" /> bytes long.</param>
    /// <param name="path">The authentication path, leaf-upward.</param>
    /// <returns>
    /// <see langword="true" /> when the path carries <paramref name="leafHash" /> to <paramref name="root" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// Prefer
    /// <see cref="VerifyInclusion(ReadOnlySpan{byte}, long, long, ReadOnlySpan{byte}, IReadOnlyList{ReadOnlyMemory{byte}})" />
    /// where the entry's bytes are available. Possession of a leaf <em>hash</em> proves nothing about possession of the
    /// data: a party that kept its leaf hashes and discarded the bytes can still satisfy this overload.
    /// </para>
    /// <para>
    /// <paramref name="treeSize" /> is trusted input here for the same reason and with the same consequence as in the
    /// entry overload.
    /// </para>
    /// </remarks>
    public bool VerifyInclusionOfLeafHash(
        ReadOnlySpan<byte> root,
        long treeSize,
        long leafIndex,
        ReadOnlySpan<byte> leafHash,
        IReadOnlyList<ReadOnlyMemory<byte>> path)
    {
        ThrowHelper.ThrowIfNull(path);

        if (root.Length != HashLength || leafHash.Length != HashLength)
            return false;

        using HashAlgorithm hasher = CreateAlgorithm();
        byte[]? head = WalkToHead(treeSize, leafIndex, leafHash, path, hasher);
        return head is not null && CryptographicOperations.FixedTimeEquals(head, root);
    }

    /// <summary>
    /// Verifies an inclusion proof against a length-bound root, so a tree size the publisher did not commit to is
    /// rejected even when the unbound walk would accept it.
    /// </summary>
    /// <param name="boundRoot">The published bound root.</param>
    /// <param name="boundValue">The value the publisher bound — the entry count, or the input's byte length.</param>
    /// <param name="treeSize">The number of entries the tree is claimed to hold.</param>
    /// <param name="leafIndex">The zero-based index the entry is claimed to occupy.</param>
    /// <param name="entry">The entry's bytes, which are hashed as a leaf.</param>
    /// <param name="path">The authentication path, leaf-upward.</param>
    /// <returns>
    /// <see langword="true" /> when the path carries <paramref name="entry" /> to a head that binds to
    /// <paramref name="boundRoot" /> under <paramref name="boundValue" />; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This is the overload to use when the tree size comes from the party being examined. Because the bound value is
    /// hashed into the commitment, a claimed size that disagrees with the published one produces a different root and
    /// the check fails closed.
    /// </remarks>
    public bool VerifyInclusionBound(
        ReadOnlySpan<byte> boundRoot,
        long boundValue,
        long treeSize,
        long leafIndex,
        ReadOnlySpan<byte> entry,
        IReadOnlyList<ReadOnlyMemory<byte>> path)
    {
        ThrowHelper.ThrowIfNull(path);

        if (boundRoot.Length != HashLength || boundValue < 0)
            return false;

        using HashAlgorithm hasher = CreateAlgorithm();
        byte[] leafHash = HashWithPrefix(hasher, MerkleTreeFormat.LeafPrefix, entry);

        byte[]? head = WalkToHead(treeSize, leafIndex, leafHash, path, hasher);
        if (head is null)
            return false;

        Span<byte> bound = stackalloc byte[BoundValueLength];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bound, (ulong)boundValue);

        byte[] actual = HashWithPrefix(hasher, MerkleTreeFormat.RootPrefix, bound, head);
        return CryptographicOperations.FixedTimeEquals(actual, boundRoot);
    }

    /// <summary>
    /// Verifies that a block occupies the stated position of a block-mode tree whose bound root pins the input's byte
    /// length, and that the block is exactly the length that position requires.
    /// </summary>
    /// <param name="boundRoot">The published bound root, binding <paramref name="inputLength" />.</param>
    /// <param name="inputLength">The input's total length in bytes, as the publisher committed to it.</param>
    /// <param name="blockSize">The block size the tree was built with.</param>
    /// <param name="blockIndex">The zero-based index of the block being proved.</param>
    /// <param name="block">The block's bytes.</param>
    /// <param name="path">The authentication path, leaf-upward.</param>
    /// <returns>
    /// <see langword="true" /> when the block is where it is claimed to be; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="path" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// This is the possession-check shape: the tree size is <em>derived</em> from the bound length and block size
    /// rather than supplied, so there is no size for a holder to misstate. It additionally requires
    /// <paramref name="block" /> to be exactly <see cref="MerkleBlocks.BlockLength(long, long, int)" /> bytes — a check
    /// the entry-mode overloads cannot make, because a variable-length entry has no expected length.
    /// </para>
    /// <para>
    /// The block's <em>bytes</em> are the proof. A party that retained the authentication path but discarded the block
    /// can still produce the path and still cannot answer, which is the difference between this and a digest it could
    /// have cached on receipt.
    /// </para>
    /// </remarks>
    public bool VerifyBlockInclusion(
        ReadOnlySpan<byte> boundRoot,
        long inputLength,
        int blockSize,
        long blockIndex,
        ReadOnlySpan<byte> block,
        IReadOnlyList<ReadOnlyMemory<byte>> path)
    {
        ThrowHelper.ThrowIfNull(path);

        if (boundRoot.Length != HashLength || inputLength <= 0 || blockSize <= 0 || blockIndex < 0)
            return false;

        long treeSize = (inputLength + blockSize - 1) / blockSize;
        if (blockIndex >= treeSize)
            return false;

        long offset = blockIndex * blockSize;
        int expectedLength = (int)Math.Min(blockSize, inputLength - offset);
        if (block.Length != expectedLength)
            return false;

        return VerifyInclusionBound(boundRoot, inputLength, treeSize, blockIndex, block, path);
    }

    /// <summary>
    /// Returns the largest number of steps any authentication path in a tree of the given size can carry.
    /// </summary>
    /// <param name="treeSize">The number of entries in the tree. Must be positive.</param>
    /// <returns><c>ceil(log2(treeSize))</c>.</returns>
    /// <remarks>
    /// This is an upper bound across <em>all</em> leaf indices, and is deliberately not the length expected of any
    /// particular index. Path length varies by index — in a seven-leaf tree leaves 0 to 5 have three steps and leaf 6
    /// has two — so a guard tightened to a per-index length would reject valid proofs. Only a path longer than this
    /// bound can be discarded before it is walked; a path that is too short is caught by the walk itself.
    /// </remarks>
    private static int MaximumPathLength(long treeSize) => MerkleTreeCore.MaximumPathLength(treeSize);

    /// <summary>
    /// Walks an authentication path from a leaf hash to the tree head, following RFC 6962 §2.1.1.
    /// </summary>
    /// <param name="treeSize">The number of entries the tree is claimed to hold.</param>
    /// <param name="leafIndex">The zero-based index the leaf is claimed to occupy.</param>
    /// <param name="leafHash">The leaf's hash.</param>
    /// <param name="path">The authentication path, leaf-upward.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <returns>The computed head, or <see langword="null" /> when the proof is structurally invalid.</returns>
    /// <remarks>
    /// <para>
    /// The <c>sn</c> bookkeeping alone rejects a path that is too short or too long; the only length check applied
    /// before the walk is the strict upper bound of <see cref="MaximumPathLength(long)" />, which cannot reject a valid
    /// proof.
    /// </para>
    /// <para>
    /// The inner shift loop terminates on <c>sn = 0</c>, which is RFC 6962's own wording. An implementation that
    /// terminates on <c>fn = 0</c> instead has been shown to accept and reject exactly the same proofs, but using the
    /// RFC's wording means a reader comparing this code with the standard is never asked to prove an equivalence.
    /// </para>
    /// </remarks>
    private byte[]? WalkToHead(
        long treeSize,
        long leafIndex,
        ReadOnlySpan<byte> leafHash,
        IReadOnlyList<ReadOnlyMemory<byte>> path,
        HashAlgorithm hasher) =>
        MerkleTreeCore.WalkToHead(treeSize, leafIndex, leafHash, path, hasher, HashLength);

    /// <summary>
    /// Builds the authentication path for one leaf of a validated leaf-hash array.
    /// </summary>
    /// <param name="leafHashes">The ordered leaf hashes.</param>
    /// <param name="leafIndex">The zero-based index of the leaf to prove.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <returns>The path, leaf-upward.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="leafIndex" /> is negative or is not less than the number of leaf hashes.
    /// </exception>
    private byte[][] BuildPath(byte[][] leafHashes, long leafIndex, HashAlgorithm hasher)
    {
        ThrowHelper.ThrowIfNegative(leafIndex);
        ThrowHelper.ThrowIfGreaterThanOrEqual(leafIndex, leafHashes.Length);

        List<byte[]> path = [];
        AppendPath(leafHashes, (int)leafIndex, path, hasher);
        return [.. path];
    }

    /// <summary>
    /// Appends the sibling subtree roots on the way from a leaf to the root, leaf-upward.
    /// </summary>
    /// <param name="leafHashes">The subtree's leaf hashes.</param>
    /// <param name="index">The index within this subtree of the leaf being proved.</param>
    /// <param name="path">The path being built.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    private void AppendPath(ReadOnlySpan<byte[]> leafHashes, int index, List<byte[]> path, HashAlgorithm hasher) =>
        MerkleTreeCore.AppendPath(leafHashes, index, path, hasher, HashLength);
}
