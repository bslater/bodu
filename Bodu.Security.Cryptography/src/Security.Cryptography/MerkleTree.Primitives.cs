// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTree.Primitives.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Numerics;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The stateless RFC 6962 primitives — the domain-separation prefixes, the split point, the prefixed hashes, the
/// recursive Merkle Tree Hash, and the authentication-path and consistency walks.
/// </summary>
/// <remarks>
/// Every member here is <see langword="static" /> and parameter-driven: the caller supplies the hash algorithm and its
/// digest length, so nothing is shared across threads by this code. The guards that remain are backstops; the public
/// members validate their arguments, with messages, before delegating.
/// </remarks>
public sealed partial class MerkleTree
{
    /// <summary>The domain-separation prefix byte prepended to leaf data before hashing, <c>H(0x00 || data)</c>.</summary>
    internal const byte LeafPrefix = 0x00;

    /// <summary>The domain-separation prefix byte prepended to an internal node's concatenated child hashes, <c>H(0x01 || children)</c>.</summary>
    internal const byte InternalNodePrefix = 0x01;

    /// <summary>The domain-separation prefix byte prepended to a length-bound root's big-endian value and tree head, as <c>H(0x02 || u64_be(boundValue) || treeHead)</c>. An addition to RFC 6962, not part of it.</summary>
    internal const byte RootPrefix = 0x02;

    /// <summary>The width, in bytes, of the big-endian value bound into a root.</summary>
    internal const int BoundValueLength = sizeof(ulong);

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
    /// Computes an internal node over any number of children, <c>H(0x01 || child₀ || … || child_{k−1})</c>.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="children">The child hashes, in order. Must not be empty.</param>
    /// <returns>The node's hash.</returns>
    /// <remarks>
    /// With two children this is byte-for-byte the node <see cref="HashWithPrefix" /> computes; the general form exists
    /// for the level fold's wider fan-outs.
    /// </remarks>
    internal static byte[] HashChildren(HashAlgorithm hasher, int hashLength, ReadOnlySpan<byte[]> children)
    {
        int total = 1;
        foreach (byte[] child in children)
            total += child.Length;

        byte[] rented = ArrayPool<byte>.Shared.Rent(total);
        try
        {
            rented[0] = InternalNodePrefix;
            int cursor = 1;
            foreach (byte[] child in children)
            {
                child.CopyTo(rented.AsSpan(cursor));
                cursor += child.Length;
            }

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
    /// Computes the Merkle Tree Hash over a non-empty span of leaf hashes by RFC 6962's recursive definition.
    /// </summary>
    /// <param name="leafHashes">The leaf hashes, in order. Must not be empty.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <returns>The subtree's root.</returns>
    /// <remarks>
    /// The root computations reduce through <see cref="LevelFold" /> instead; this recursion serves the proof walks,
    /// which need subtree roots at arbitrary split points. Recursion depth is logarithmic in the entry count, so no
    /// stack guard is required. A single leaf's hash is returned unchanged — a subtree root is promoted, never
    /// re-hashed.
    /// </remarks>
    internal static byte[] Mth(ReadOnlySpan<byte[]> leafHashes, HashAlgorithm hasher, int hashLength)
    {
        if (leafHashes.Length == 1)
            return leafHashes[0];

        int split = SplitPoint(leafHashes.Length);
        return HashWithPrefix(
            hasher,
            hashLength,
            InternalNodePrefix,
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
                running = HashWithPrefix(hasher, hashLength, InternalNodePrefix, sibling, running);

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
                running = HashWithPrefix(hasher, hashLength, InternalNodePrefix, running, sibling);
            }

            fn >>= 1;
            sn >>= 1;
        }

        return sn == 0 ? running : null;
    }
}
