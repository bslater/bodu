// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTestsBase.RealAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests that exercise both Merkle hasher implementations with a <b>real, non-commutative</b>
/// hash algorithm (<see cref="SHA256" />) rather than the additive
/// <c>MonitoringHashAlgorithm</c>.
/// </summary>
/// <remarks>
/// <para>
/// The additive hash used elsewhere in this suite is <b>commutative across input boundaries</b> —
/// its value depends only on the multiset of input bytes, not their order. That property makes
/// expected values easy to hand-compute but also makes it impossible for the monitoring suite
/// to catch any bug that shuffles input order, re-groups nodes incorrectly, or swaps
/// <see cref="HashAlgorithm.TransformBlock" />/<see cref="HashAlgorithm.TransformFinalBlock" />
/// semantics. A real cryptographic hash is order-dependent at every boundary and will reject
/// any such error as a mismatched root.
/// </para>
/// <para>
/// These tests are declared as a partial extension of
/// <see cref="MerkleTreeHashTestsBase{THasher}" /> so both derived classes inherit them
/// automatically.
/// </para>
/// </remarks>
public abstract partial class MerkleTreeHashTestsBase<THasher>
{
    private static Func<HashAlgorithm> Sha256Factory => SHA256.Create;

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Order sensitivity — the central property a real hash exposes
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that reversing the input byte order produces a different root under a real hash
    /// algorithm. With the additive monitoring hash this would incorrectly succeed, so this
    /// test can only run with SHA-256.
    /// </summary>
    [TestMethod]
    public void ComputeHash_RealAlgorithm_WhenInputReversed_ShouldProduceDifferentRoot()
    {
        var forward = MakeData(64);
        var reversed = (byte[])forward.Clone();
        Array.Reverse(reversed);

        using THasher h1 = Construct(Sha256Factory, DefaultBlockSize, DefaultFanOut);
        using THasher h2 = Construct(Sha256Factory, DefaultBlockSize, DefaultFanOut);

        CollectionAssert.AreNotEqual(
            ComputeHash(h1, forward),
            ComputeHash(h2, reversed),
            "Reversed input produced the same SHA-256 root — implies a commutativity bug.");
    }

    /// <summary>
    /// Verifies that swapping two adjacent blocks at the leaf level produces a different root —
    /// guards against any level-assembly bug that accidentally commutes sibling nodes.
    /// </summary>
    [TestMethod]
    public void ComputeHash_RealAlgorithm_WhenAdjacentBlocksSwapped_ShouldProduceDifferentRoot()
    {
        var original = MakeData(16); // exactly 4 blocks of 4 bytes

        // Swap block 0 and block 1.
        var swapped = (byte[])original.Clone();
        for (var i = 0; i < 4; i++)
            (swapped[i], swapped[i + 4]) = (swapped[i + 4], swapped[i]);

        using THasher h1 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        using THasher h2 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);

        CollectionAssert.AreNotEqual(
            ComputeHash(h1, original),
            ComputeHash(h2, swapped),
            "Swapping adjacent leaf blocks produced the same root — implies a sibling-ordering bug.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Result shape and determinism
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that SHA-256 produces a 32-byte root, confirming the implementation reports the
    /// backing algorithm's <see cref="HashAlgorithm.HashSize" /> correctly rather than assuming
    /// a fixed output length.
    /// </summary>
    [TestMethod]
    public void ComputeHash_RealAlgorithm_WhenSha256Used_ShouldReturn32ByteRoot()
    {
        using THasher hasher = Construct(Sha256Factory, DefaultBlockSize, DefaultFanOut);
        var result = ComputeHash(hasher, MakeData(64));

        Assert.AreEqual(32, result.Length,
            "SHA-256 root must be 32 bytes; implementation may be assuming MonitoringHashAlgorithm's 4-byte size.");
    }

    /// <summary>
    /// Verifies that repeated SHA-256 computations of the same input on the same instance yield
    /// byte-identical results, confirming full state reset between calls.
    /// </summary>
    [TestMethod]
    public void ComputeHash_RealAlgorithm_WhenCalledRepeatedly_ShouldProduceIdenticalResults()
    {
        var data = MakeData(100);
        using THasher hasher = Construct(Sha256Factory, DefaultBlockSize, DefaultFanOut);

        var first = ComputeHash(hasher, data);
        var second = ComputeHash(hasher, data);
        var third = ComputeHash(hasher, data);

        CollectionAssert.AreEqual(first, second, "Second call diverged from first.");
        CollectionAssert.AreEqual(first, third, "Third call diverged from first.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Cross-configuration sensitivity
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that different fan-out values produce different roots for the same input under
    /// a real hash. The additive monitoring algorithm conceals this because its result depends
    /// only on the total byte count; SHA-256 exposes it cleanly.
    /// </summary>
    [TestMethod]
    public void ComputeHash_RealAlgorithm_WhenFanOutChanges_ShouldProduceDifferentRoot()
    {
        var data = MakeData(64);

        using THasher h2 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        using THasher h4 = Construct(Sha256Factory, blockSize: 4, fanOut: 4);

        CollectionAssert.AreNotEqual(
            ComputeHash(h2, data),
            ComputeHash(h4, data),
            "Different fan-out values produced the same SHA-256 root — tree topology must affect the root.");
    }

    /// <summary>
    /// Verifies that different block sizes produce different roots for the same input under a
    /// real hash — confirming the leaf-size choice genuinely feeds through the computation.
    /// </summary>
    [TestMethod]
    public void ComputeHash_RealAlgorithm_WhenBlockSizeChanges_ShouldProduceDifferentRoot()
    {
        var data = MakeData(64);

        using THasher h4 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        using THasher h8 = Construct(Sha256Factory, blockSize: 8, fanOut: 2);

        CollectionAssert.AreNotEqual(
            ComputeHash(h4, data),
            ComputeHash(h8, data),
            "Different block sizes produced the same SHA-256 root — block size must affect the root.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Known-answer tests — hand-specified SHA-256 root vectors
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies a specific known-answer: SHA-256 Merkle root of the first 8 bytes 1..8 with
    /// blockSize=4 and fanOut=2.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tree shape: two leaves (each a zero-padded 4-byte block), combined by concatenating their
    /// 32-byte hashes and hashing the 64-byte result.
    /// </para>
    /// <para>
    /// This vector is recomputed from first principles inside the test rather than hard-coded,
    /// because the exact zero-padding width is implementation-defined. It acts as a direct
    /// reference test that will fail on any change to the leaf-padding or combination strategy.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void ComputeHash_RealAlgorithm_KnownAnswer_Sha256_TwoLeafTree()
    {
        byte[] data = [1, 2, 3, 4, 5, 6, 7, 8];
        var expected = ComputeSha256MerkleRoot(data, blockSize: 4, fanOut: 2);

        using THasher hasher = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    /// <summary>
    /// Verifies the SHA-256 root for a three-leaf, uneven-remainder tree — the classic case
    /// where an internal node receives a single child and must be passed through rather than
    /// combined.
    /// </summary>
    [TestMethod]
    public void ComputeHash_RealAlgorithm_KnownAnswer_Sha256_UnevenTree()
    {
        var data = MakeData(12); // three 4-byte leaves with fanOut=2 → uneven at level 1
        var expected = ComputeSha256MerkleRoot(data, blockSize: 4, fanOut: 2);

        using THasher hasher = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        CollectionAssert.AreEqual(expected, ComputeHash(hasher, data));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Helper — reference SHA-256 Merkle reduction
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reference SHA-256 Merkle reduction used to cross-validate implementation output.
    /// Mirrors the structure of <see cref="MerkleTestData.ComputeAdditiveRoot" /> but uses
    /// SHA-256 for both leaf and internal nodes.
    /// </summary>
    /// <param name="data">The raw input bytes. Must not be empty.</param>
    /// <param name="blockSize">The byte-count per leaf block; partial tails are zero-padded to this.</param>
    /// <param name="fanOut">The maximum children combined into each parent node.</param>
    /// <returns>A 32-byte SHA-256 root hash computed by full tree reduction.</returns>
    private static byte[] ComputeSha256MerkleRoot(byte[] data, int blockSize, int fanOut)
    {
        using var sha = SHA256.Create();

        // Leaves: zero-padded blocks hashed individually.
        var level = new List<byte[]>();
        for (var offset = 0; offset < data.Length; offset += blockSize)
        {
            var len = Math.Min(blockSize, data.Length - offset);
            var block = new byte[blockSize];
            Array.Copy(data, offset, block, 0, len);
            level.Add(sha.ComputeHash(block));
        }

        // Reduce: concatenate each group of up to fanOut child hashes and hash the combination.
        while (level.Count > 1)
        {
            var next = new List<byte[]>();
            for (var i = 0; i < level.Count; i += fanOut)
            {
                var groupSize = Math.Min(fanOut, level.Count - i);

                // Concatenate child hashes into a single buffer, then hash.
                var totalLen = 0;
                for (var j = 0; j < groupSize; j++) totalLen += level[i + j].Length;
                var combined = new byte[totalLen];
                var cursor = 0;
                for (var j = 0; j < groupSize; j++)
                {
                    Buffer.BlockCopy(level[i + j], 0, combined, cursor, level[i + j].Length);
                    cursor += level[i + j].Length;
                }

                next.Add(sha.ComputeHash(combined));
            }
            level = next;
        }

        return level[0];
    }
}
