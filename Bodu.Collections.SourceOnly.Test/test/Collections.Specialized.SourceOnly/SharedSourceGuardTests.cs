// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SharedSourceGuardTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

// The shared source is compiled into this assembly under the SECURITY_CRYPTOGRAPHY symbol, so its types land in
// Bodu.Security.Cryptography rather than Bodu.Collections.Generic. That is the point of the guard, not an accident:
// it proves the namespace switch works and keeps these copies from colliding with the real package's types.
using Bodu.Security.Cryptography;

namespace Bodu.Collections.Specialized.SourceOnly;

/// <summary>
/// Holds the source-compiled <c>MerkleTreeCore</c> to the published RFC 6962 vectors, proving that the shared
/// source is usable by an assembly that takes no dependency on <c>Bodu.Collections</c>.
/// </summary>
/// <remarks>
/// <para>
/// That this assembly <em>compiles</em> is most of the guard: the project references only <c>Bodu.Core</c> and the
/// test infrastructure, so introducing a resource lookup, a helper from the owning project, or any other outside
/// type into the shared source breaks this build while leaving the owning package green.
/// </para>
/// <para>
/// The assertions add the other half. A file can compile in isolation and still be wrong — for instance if a guard
/// that was moved into the facade left the core accepting something it should not — so the primitives are driven
/// through the same published values the owning package's suite pins, reached here entirely through source.
/// </para>
/// <para>
/// The inputs are the eight entries of the RFC 6962 reference test suite, so a failure here is a disagreement with
/// the standard rather than with this repository.
/// </para>
/// </remarks>
[TestClass]
public sealed class SharedSourceGuardTests
{
    /// <summary>The eight reference entries, in order. The first is the empty entry.</summary>
    private static readonly byte[][] ReferenceEntries =
    [
        [],
        [0x00],
        [0x10],
        [0x20, 0x21],
        [0x30, 0x31],
        [0x40, 0x41, 0x42, 0x43],
        [0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57],
        [0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f],
    ];

    /// <summary>
    /// Verifies that the source-compiled core reproduces the published Merkle Tree Hash for entry counts zero
    /// through eight.
    /// </summary>
    /// <param name="entryCount">The number of reference entries to fold.</param>
    /// <param name="expectedRoot">The published root, lowercase hex.</param>
    [TestMethod]
    [DataRow(0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [DataRow(1, "6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d")]
    [DataRow(2, "fac54203e7cc696cf0dfcb42c92a1d9dbaf70ad9e621f4bd8d98662f00e3c125")]
    [DataRow(3, "aeb6bcfe274b70a14fb067a5e5578264db0fa9b51af5e0ba159158f329e06e77")]
    [DataRow(4, "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7")]
    [DataRow(5, "4e3bbb1f7b478dcfe71fb631631519a3bca12c9aefca1612bfce4c13a86264d4")]
    [DataRow(6, "76e67dadbcdf1e10e1b74ddc608abd2f98dfb16fbce75277b5232a127f2087ef")]
    [DataRow(7, "ddb89be403809e325750d3d263cd78929c2942b7942a34b77e122c9594a74c8c")]
    [DataRow(8, "5dc9da79a70659a9ad559cb701ded9a2ab9d823aad2f4960cfe370eff4604328")]
    public void Mth_WhenCompiledFromSharedSource_ShouldReproduceRfc6962MerkleTreeHash(
        int entryCount, string expectedRoot)
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        string actual = entryCount == 0
            ? Hex(MerkleTreeCore.HashEmpty(hasher, hashLength))
            : Hex(MerkleTreeCore.Mth(LeafHashes(hasher, hashLength, entryCount), hasher, hashLength));

        Assert.AreEqual(expectedRoot, actual);
    }

    /// <summary>
    /// Verifies that the source-compiled core still applies RFC 6962's domain separation, so a one-entry tree's root
    /// is the entry's leaf hash and never its bare digest.
    /// </summary>
    [TestMethod]
    public void HashWithPrefix_WhenCompiledFromSharedSource_ShouldDomainSeparateLeavesFromNodes()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        byte[] entry = [0x61, 0x62, 0x63];

        byte[] leaf = MerkleTreeCore.HashWithPrefix(hasher, hashLength, MerkleTreeFormat.LeafPrefix, entry);

        Assert.AreEqual("609f6e36d2405585188d5cfd761f407c7cc46a7d3f314c88270469dde315fcd1", Hex(leaf));
        Assert.AreNotEqual(Hex(SHA256.HashData(entry)), Hex(leaf));
        Assert.AreEqual(0x00, MerkleTreeFormat.LeafPrefix);
        Assert.AreEqual(0x01, MerkleTreeFormat.InternalNodePrefix);
        Assert.AreEqual(0x02, MerkleTreeFormat.RootPrefix);
    }

    /// <summary>
    /// Verifies that the source-compiled core splits three entries 2 + 1 rather than 1 + 2, the point on which
    /// RFC 6962 differs from a naive halving.
    /// </summary>
    [TestMethod]
    public void SplitPoint_WhenCompiledFromSharedSource_ShouldSplitAtTheLargestPowerOfTwoBelowTheCount()
    {
        Assert.AreEqual(2, MerkleTreeCore.SplitPoint(3));
        Assert.AreEqual(4, MerkleTreeCore.SplitPoint(7));
        Assert.AreEqual(4, MerkleTreeCore.SplitPoint(8));
        Assert.AreEqual(8, MerkleTreeCore.SplitPoint(9));
    }

    /// <summary>
    /// Verifies that a path generated by the source-compiled core walks back to the same root, including the
    /// short path that leaf 6 of a seven-entry tree carries.
    /// </summary>
    [TestMethod]
    public void WalkToHead_WhenCompiledFromSharedSource_ShouldRoundTripEveryLeafOfASevenEntryTree()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        byte[][] leaves = LeafHashes(hasher, hashLength, 7);
        byte[] root = MerkleTreeCore.Mth(leaves, hasher, hashLength);

        for (int leafIndex = 0; leafIndex < 7; leafIndex++)
        {
            List<byte[]> path = [];
            MerkleTreeCore.AppendPath(leaves, leafIndex, path, hasher, hashLength);

            byte[]? head = MerkleTreeCore.WalkToHead(
                7,
                leafIndex,
                leaves[leafIndex],
                path.Select(step => (ReadOnlyMemory<byte>)step).ToArray(),
                hasher,
                hashLength);

            Assert.IsNotNull(head, $"leaf {leafIndex} must walk to a head");
            Assert.AreEqual(Hex(root), Hex(head), $"leaf {leafIndex}");
            Assert.AreEqual(leafIndex == 6 ? 2 : 3, path.Count, $"leaf {leafIndex} path length");
        }
    }

    /// <summary>
    /// Verifies that the source-compiled core rejects a structurally invalid proof by returning
    /// <see langword="null" /> rather than throwing, which is what keeps the public verifiers total.
    /// </summary>
    [TestMethod]
    public void WalkToHead_WhenProofIsInvalid_ShouldReturnNullRatherThanThrow()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        byte[][] leaves = LeafHashes(hasher, hashLength, 4);

        // Index at the tree size, a zero tree size, and a path longer than ceil(log2 n) all reject.
        Assert.IsNull(MerkleTreeCore.WalkToHead(4, 4, leaves[0], [], hasher, hashLength));
        Assert.IsNull(MerkleTreeCore.WalkToHead(0, 0, leaves[0], [], hasher, hashLength));
        Assert.IsNull(MerkleTreeCore.WalkToHead(
            4, 0, leaves[0], Enumerable.Repeat((ReadOnlyMemory<byte>)new byte[32], 5).ToArray(), hasher, hashLength));
    }

    /// <summary>
    /// Verifies that the source-compiled block arithmetic agrees with the published behaviour, including the
    /// zero-length input having no blocks at all and the final block being short rather than padded.
    /// </summary>
    [TestMethod]
    public void BlockArithmetic_WhenCompiledFromSharedSource_ShouldMatchPublishedBehaviour()
    {
        Assert.AreEqual(0L, MerkleTreeCore.BlockCount(0, 4), "a zero-length input has no blocks, not one empty one");
        Assert.AreEqual(1L, MerkleTreeCore.BlockCount(1, 4));
        Assert.AreEqual(9L, MerkleTreeCore.BlockCount(33, 4));

        Assert.AreEqual(4, MerkleTreeCore.BlockLength(33, 0, 4));
        Assert.AreEqual(1, MerkleTreeCore.BlockLength(33, 8, 4), "the final block is short, never padded");
        Assert.AreEqual(0, MerkleTreeCore.BlockLength(33, 9, 4));

        // 64-bit arithmetic: a one-mebibyte block size passes int.MaxValue at 2048 blocks.
        Assert.AreEqual(2_147_483_648L, MerkleTreeCore.BlockOffset(2048, 1024 * 1024));
    }

    /// <summary>
    /// Returns the leaf hashes of the first <paramref name="count" /> reference entries.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="count">The number of entries to hash.</param>
    /// <returns>The leaf hashes, in order.</returns>
    private static byte[][] LeafHashes(HashAlgorithm hasher, int hashLength, int count)
    {
        byte[][] leaves = new byte[count][];
        for (int index = 0; index < count; index++)
            leaves[index] = MerkleTreeCore.HashWithPrefix(
                hasher, hashLength, MerkleTreeFormat.LeafPrefix, ReferenceEntries[index]);

        return leaves;
    }

    /// <summary>Converts a hash to the lowercase hex used throughout the published vectors.</summary>
    /// <param name="value">The bytes to convert.</param>
    /// <returns>The lowercase hex encoding.</returns>
    private static string Hex(ReadOnlySpan<byte> value) => Convert.ToHexString(value).ToLowerInvariant();
}
