// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTree.Consistency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Collections.Merkle;

/// <summary>
/// Consistency proofs: evidence that one published tree is a prefix of a later one.
/// </summary>
public sealed partial class Rfc6962MerkleTree
{
    /// <summary>
    /// Produces the consistency proof that the first <paramref name="firstSize" /> entries of
    /// <paramref name="entries" /> form a prefix of the whole.
    /// </summary>
    /// <param name="entries">The later tree's entries, in order.</param>
    /// <param name="firstSize">The number of entries in the earlier tree.</param>
    /// <returns>
    /// The proof, or an empty proof when the two sizes are equal or the earlier tree is empty — in both cases the
    /// consistency is established by the sizes and roots alone.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="entries" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="firstSize" /> is negative or exceeds the number of entries.
    /// </exception>
    public byte[][] ConsistencyProof(IReadOnlyList<ReadOnlyMemory<byte>> entries, long firstSize)
    {
        ThrowHelper.ThrowIfNull(entries);

        using HashAlgorithm hasher = CreateAlgorithm();

        byte[][] leafHashes = new byte[entries.Count][];
        for (int index = 0; index < entries.Count; index++)
            leafHashes[index] = HashWithPrefix(hasher, MerkleTreeFormat.LeafPrefix, entries[index].Span);

        return BuildConsistencyProof(leafHashes, firstSize, hasher);
    }

    /// <summary>
    /// Produces the consistency proof from leaf hashes already computed.
    /// </summary>
    /// <param name="leafHashes">The later tree's ordered leaf hashes.</param>
    /// <param name="firstSize">The number of entries in the earlier tree.</param>
    /// <returns>The proof, or an empty proof when the sizes are equal or the earlier tree is empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leafHashes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// An element is <see langword="null" /> or is not <see cref="HashLength" /> bytes long.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="firstSize" /> is negative or exceeds the number of leaf hashes.
    /// </exception>
    public byte[][] ConsistencyProofOfLeafHashes(IReadOnlyList<byte[]> leafHashes, long firstSize)
    {
        byte[][] copy = ValidateLeafHashes(leafHashes);

        using HashAlgorithm hasher = CreateAlgorithm();
        return BuildConsistencyProof(copy, firstSize, hasher);
    }

    /// <summary>
    /// Verifies that a tree of <paramref name="firstSize" /> entries with root <paramref name="firstRoot" /> is a
    /// prefix of a tree of <paramref name="secondSize" /> entries with root <paramref name="secondRoot" />.
    /// </summary>
    /// <param name="firstRoot">The earlier tree's published root.</param>
    /// <param name="firstSize">The earlier tree's entry count.</param>
    /// <param name="secondRoot">The later tree's published root.</param>
    /// <param name="secondSize">The later tree's entry count.</param>
    /// <param name="proof">The consistency proof.</param>
    /// <returns>
    /// <see langword="true" /> when the proof reconstructs both roots; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="proof" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// This is <see href="https://www.rfc-editor.org/rfc/rfc6962#section-2.1.2">RFC 6962 §2.1.2</see>. Unlike inclusion
    /// verification, both sizes and both roots are inputs, and the proof must reconstruct <em>both</em> — so there is
    /// no analogue here of the tree-size ambiguity that the bound root exists to close.
    /// </para>
    /// <para>
    /// Three degenerate cases are decided before the walk. A <paramref name="secondSize" /> below
    /// <paramref name="firstSize" /> is rejected outright: a log cannot shrink. Equal sizes require an <em>empty</em>
    /// proof and identical roots — a non-empty proof between equal sizes is rejected rather than walked, because the
    /// only evidence that could be offered is evidence of something else. A <paramref name="firstSize" /> of zero
    /// likewise requires an empty proof, since every tree extends the empty tree.
    /// </para>
    /// <para>
    /// Like the inclusion verifiers, this returns <see langword="false" /> for every malformed input rather than
    /// throwing, and only a null proof throws.
    /// </para>
    /// </remarks>
    public bool VerifyConsistency(
        ReadOnlySpan<byte> firstRoot,
        long firstSize,
        ReadOnlySpan<byte> secondRoot,
        long secondSize,
        IReadOnlyList<ReadOnlyMemory<byte>> proof)
    {
        ThrowHelper.ThrowIfNull(proof);

        if (firstRoot.Length != HashLength || secondRoot.Length != HashLength)
            return false;

        if (firstSize < 0 || secondSize < 0 || secondSize < firstSize)
            return false;

        // A tree is consistent with itself only under an empty proof and an identical root.
        if (firstSize == secondSize)
            return proof.Count == 0 && CryptographicOperations.FixedTimeEquals(firstRoot, secondRoot);

        // Every tree extends the empty tree, and no proof can say more than that.
        if (firstSize == 0)
            return proof.Count == 0;

        if (proof.Count == 0)
            return false;

        for (int step = 0; step < proof.Count; step++)
        {
            if (proof[step].Length != HashLength)
                return false;
        }

        // Step 1: when the earlier tree is a perfect subtree its root is not carried in the proof, so supply it.
        bool carriesFirstRoot = IsPowerOfTwo(firstSize);
        byte[][] work = new byte[proof.Count + (carriesFirstRoot ? 1 : 0)][];
        int at = 0;
        if (carriesFirstRoot)
            work[at++] = firstRoot.ToArray();

        for (int step = 0; step < proof.Count; step++)
            work[at++] = proof[step].ToArray();

        // Steps 2 and 3.
        ulong fn = (ulong)(firstSize - 1);
        ulong sn = (ulong)(secondSize - 1);
        while ((fn & 1) == 1)
        {
            fn >>= 1;
            sn >>= 1;
        }

        using HashAlgorithm hasher = CreateAlgorithm();

        // Step 4.
        byte[] firstRunning = work[0];
        byte[] secondRunning = work[0];

        // Step 5.
        for (int index = 1; index < work.Length; index++)
        {
            if (sn == 0)
                return false;

            byte[] sibling = work[index];

            if ((fn & 1) == 1 || fn == sn)
            {
                firstRunning = HashWithPrefix(hasher, MerkleTreeFormat.InternalNodePrefix, sibling, firstRunning);
                secondRunning = HashWithPrefix(hasher, MerkleTreeFormat.InternalNodePrefix, sibling, secondRunning);

                if ((fn & 1) == 0)
                {
                    // RFC 6962 §2.1.2 terminates this shift on fn = 0, where §2.1.1's inclusion walk terminates on
                    // sn = 0. The difference is the standard's own wording; do not harmonize them.
                    while ((fn & 1) == 0 && fn != 0)
                    {
                        fn >>= 1;
                        sn >>= 1;
                    }
                }
            }
            else
            {
                secondRunning = HashWithPrefix(hasher, MerkleTreeFormat.InternalNodePrefix, secondRunning, sibling);
            }

            fn >>= 1;
            sn >>= 1;
        }

        // Step 6.
        return sn == 0
            && CryptographicOperations.FixedTimeEquals(firstRunning, firstRoot)
            && CryptographicOperations.FixedTimeEquals(secondRunning, secondRoot);
    }

    /// <summary>
    /// Returns whether a value is an exact power of two.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> when <paramref name="value" /> is a positive power of two.</returns>
    private static bool IsPowerOfTwo(long value) => MerkleTreeCore.IsPowerOfTwo(value);

    /// <summary>
    /// Builds the consistency proof over a validated leaf-hash array.
    /// </summary>
    /// <param name="leafHashes">The later tree's leaf hashes.</param>
    /// <param name="firstSize">The earlier tree's entry count.</param>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <returns>The proof, leaf-upward.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="firstSize" /> is negative or exceeds the number of leaf hashes.
    /// </exception>
    private byte[][] BuildConsistencyProof(byte[][] leafHashes, long firstSize, HashAlgorithm hasher)
    {
        ThrowHelper.ThrowIfNegative(firstSize);
        ThrowHelper.ThrowIfGreaterThan(firstSize, leafHashes.Length);

        if (firstSize == 0 || firstSize == leafHashes.Length)
            return [];

        List<byte[]> proof = [];
        AppendSubProof(leafHashes, (int)firstSize, onBoundary: true, proof, hasher);
        return [.. proof];
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
    private void AppendSubProof(
        ReadOnlySpan<byte[]> leafHashes,
        int first,
        bool onBoundary,
        List<byte[]> proof,
        HashAlgorithm hasher) =>
        MerkleTreeCore.AppendSubProof(leafHashes, first, onBoundary, proof, hasher, HashLength);
}
