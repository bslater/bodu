// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTree.Roots.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Collections.Merkle;

/// <summary>
/// Leaf and node hashing, the Merkle Tree Hash over entries or leaf hashes, and the length-bound root.
/// </summary>
public sealed partial class Rfc6962MerkleTree
{
    /// <summary>The width, in bytes, of the big-endian value bound into a root.</summary>
    private const int BoundValueLength = MerkleTreeCore.BoundValueLength;

    /// <summary>
    /// Computes the leaf hash of an entry as <c>H(0x00 || entry)</c>.
    /// </summary>
    /// <param name="entry">The entry's bytes. May be empty.</param>
    /// <returns>The leaf hash, <see cref="HashLength" /> bytes long.</returns>
    /// <remarks>
    /// The prefix is what makes a one-entry tree's root differ from the entry's bare digest, and is not optional:
    /// <c>HashLeaf(d)</c> is never <c>H(d)</c>.
    /// </remarks>
    public byte[] HashLeaf(ReadOnlySpan<byte> entry)
    {
        using HashAlgorithm hasher = CreateAlgorithm();
        return HashWithPrefix(hasher, MerkleTreeFormat.LeafPrefix, entry);
    }

    /// <summary>
    /// Computes an internal node hash from its two child hashes as <c>H(0x01 || left || right)</c>.
    /// </summary>
    /// <param name="left">The left child's hash.</param>
    /// <param name="right">The right child's hash.</param>
    /// <returns>The node hash, <see cref="HashLength" /> bytes long.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="left" /> or <paramref name="right" /> is not <see cref="HashLength" /> bytes long.
    /// </exception>
    /// <remarks>
    /// Order is significant: <c>HashNode(a, b)</c> is not <c>HashNode(b, a)</c>.
    /// </remarks>
    public byte[] HashNode(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        ThrowIfNotHashLength(left.Length, nameof(left));
        ThrowIfNotHashLength(right.Length, nameof(right));

        using HashAlgorithm hasher = CreateAlgorithm();
        return HashWithPrefix(hasher, MerkleTreeFormat.InternalNodePrefix, left, right);
    }

    /// <summary>
    /// Computes the Merkle Tree Hash over an ordered sequence of variable-length entries.
    /// </summary>
    /// <param name="entries">The entries, in order. May be empty; individual entries may be empty.</param>
    /// <returns>
    /// The tree's root: <c>H()</c> when <paramref name="entries" /> is empty, the single entry's leaf hash when it
    /// holds one, and the recursive node hash otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="entries" /> is <see langword="null" />.</exception>
    public byte[] ComputeRoot(IReadOnlyList<ReadOnlyMemory<byte>> entries)
    {
        ThrowHelper.ThrowIfNull(entries);

        using HashAlgorithm hasher = CreateAlgorithm();
        if (entries.Count == 0)
            return HashEmpty(hasher);

        byte[][] leafHashes = new byte[entries.Count][];
        for (int index = 0; index < entries.Count; index++)
            leafHashes[index] = HashWithPrefix(hasher, MerkleTreeFormat.LeafPrefix, entries[index].Span);

        return Mth(leafHashes, hasher);
    }

    /// <summary>
    /// Computes the Merkle Tree Hash over leaf hashes that have already been computed.
    /// </summary>
    /// <param name="leafHashes">The ordered leaf hashes, each <see cref="HashLength" /> bytes long.</param>
    /// <returns>The tree's root, or <c>H()</c> when <paramref name="leafHashes" /> is empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leafHashes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// An element is <see langword="null" /> or is not <see cref="HashLength" /> bytes long.
    /// </exception>
    /// <remarks>
    /// This is the entry point for a caller that streamed the underlying bytes past itself and kept only the leaf
    /// hashes, and so cannot supply the entries again.
    /// </remarks>
    public byte[] ComputeRootOfLeafHashes(IReadOnlyList<byte[]> leafHashes)
    {
        byte[][] copy = ValidateLeafHashes(leafHashes);

        using HashAlgorithm hasher = CreateAlgorithm();
        return copy.Length == 0 ? HashEmpty(hasher) : Mth(copy, hasher);
    }

    /// <summary>
    /// Binds a value into a root as <c>H(0x02 || u64_be(boundValue) || root)</c>, producing a commitment that names one
    /// tree and no other.
    /// </summary>
    /// <param name="root">The tree head to bind, <see cref="HashLength" /> bytes long.</param>
    /// <param name="boundValue">
    /// The value to bind — the entry count in entry mode, or the input's byte length in block mode.
    /// </param>
    /// <returns>The bound root, <see cref="HashLength" /> bytes long.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="root" /> is not <see cref="HashLength" /> bytes long.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="boundValue" /> is negative.</exception>
    /// <remarks>
    /// <para>
    /// This is an addition to RFC 6962, not part of it, and it closes a real hole. RFC 6962's verifier takes the tree
    /// size from its caller; when the caller obtains that size from the party being examined, the party can understate
    /// it. A four-entry tree's first authentication path has exactly the length a three-entry tree's first path wants
    /// and walks to the same head, so the unbound verifier accepts both. A holder that has lost its last entry can
    /// therefore declare a smaller tree, never be asked for that entry, and pass every challenge.
    /// </para>
    /// <para>
    /// Binding the size into the published commitment makes a disagreeing size produce a different root, so the
    /// challenge fails closed. In block mode bind the <em>byte length</em> rather than the block count: it is strictly
    /// stronger, because it also pins the final block's length.
    /// </para>
    /// </remarks>
    public byte[] BindRoot(ReadOnlySpan<byte> root, long boundValue)
    {
        ThrowIfNotHashLength(root.Length, nameof(root));
        ThrowHelper.ThrowIfNegative(boundValue);

        Span<byte> bound = stackalloc byte[BoundValueLength];
        BinaryPrimitives.WriteUInt64BigEndian(bound, (ulong)boundValue);

        using HashAlgorithm hasher = CreateAlgorithm();
        return HashWithPrefix(hasher, MerkleTreeFormat.RootPrefix, bound, root);
    }

    /// <summary>
    /// Computes the Merkle Tree Hash over a non-empty span of leaf hashes.
    /// </summary>
    /// <param name="leafHashes">The leaf hashes, in order. Must not be empty.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <returns>The subtree's root.</returns>
    /// <remarks>
    /// Recursion depth is logarithmic in the entry count, so no stack guard is required. A single leaf's hash is
    /// returned unchanged — a subtree root is promoted, never re-hashed, which is the single point on which this
    /// construction differs from a level-by-level reduction.
    /// </remarks>
    private byte[] Mth(ReadOnlySpan<byte[]> leafHashes, HashAlgorithm hasher) =>
        MerkleTreeCore.Mth(leafHashes, hasher, HashLength);

    /// <summary>
    /// Computes the empty tree's root, <c>H()</c>.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <returns>The hash of zero bytes.</returns>
    private byte[] HashEmpty(HashAlgorithm hasher) => MerkleTreeCore.HashEmpty(hasher, HashLength);

    /// <summary>
    /// Creates a hash algorithm for one operation.
    /// </summary>
    /// <returns>A fresh <see cref="HashAlgorithm" />.</returns>
    /// <exception cref="InvalidOperationException">The factory returned <see langword="null" />.</exception>
    private HashAlgorithm CreateAlgorithm() =>
        _algorithmFactory()
            ?? throw new InvalidOperationException(CollectionsResourceStrings.Arg_Invalid_MerkleAlgorithmFactoryNull);

    /// <summary>
    /// Copies and validates a caller-supplied list of leaf hashes.
    /// </summary>
    /// <param name="leafHashes">The list to validate.</param>
    /// <returns>A defensive copy, so later mutation by the caller cannot affect the computation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leafHashes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// An element is <see langword="null" /> or is not <see cref="HashLength" /> bytes long.
    /// </exception>
    private byte[][] ValidateLeafHashes(IReadOnlyList<byte[]> leafHashes)
    {
        ThrowHelper.ThrowIfNull(leafHashes);

        byte[][] copy = new byte[leafHashes.Count][];
        for (int index = 0; index < leafHashes.Count; index++)
        {
            byte[] leafHash = leafHashes[index]
                ?? throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CollectionsResourceStrings.Arg_Invalid_MerkleLeafHashNullAtIndex,
                        index),
                    nameof(leafHashes));

            if (leafHash.Length != HashLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CollectionsResourceStrings.Arg_Invalid_MerkleLeafHashLengthAtIndex,
                        index,
                        HashLength,
                        leafHash.Length),
                    nameof(leafHashes));
            }

            copy[index] = leafHash;
        }

        return copy;
    }

    /// <summary>
    /// Throws when a supplied hash is not the configured digest width.
    /// </summary>
    /// <param name="length">The supplied length, in bytes.</param>
    /// <param name="paramName">The parameter to name in the exception.</param>
    /// <exception cref="ArgumentException"><paramref name="length" /> is not <see cref="HashLength" />.</exception>
    private void ThrowIfNotHashLength(int length, string paramName)
    {
        if (length != HashLength)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CollectionsResourceStrings.Arg_Invalid_MerkleHashLength,
                    HashLength,
                    length),
                paramName);
        }
    }
}
