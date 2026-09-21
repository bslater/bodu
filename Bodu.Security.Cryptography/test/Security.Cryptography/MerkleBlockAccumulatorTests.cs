// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockAccumulatorTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleBlockAccumulator" />, the write-time counterpart of the tree's block-mode roots.
/// </summary>
[TestClass]
public partial class MerkleBlockAccumulatorTests
{
    /// <summary>The block size the boundary sweeps use: small enough that every shape appears within a few bytes.</summary>
    private const int SmallBlock = 8;

    private static MerkleTree CreateTree() => new(SHA256.Create);

    private static string Hex(ReadOnlySpan<byte> value) => Convert.ToHexString(value).ToLowerInvariant();

    private static byte[] SeededInput(int length, int seed = 0x6962)
    {
        byte[] bytes = new byte[length];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }

    private static void AppendInChunks(MerkleBlockAccumulator accumulator, ReadOnlySpan<byte> data, int chunkSize)
    {
        for (int offset = 0; offset < data.Length; offset += chunkSize)
            accumulator.Append(data.Slice(offset, Math.Min(chunkSize, data.Length - offset)));
    }

    private static void AppendInRandomChunks(MerkleBlockAccumulator accumulator, ReadOnlySpan<byte> data, int seed)
    {
        var random = new Random(seed);
        int offset = 0;
        while (offset < data.Length)
        {
            int take = Math.Min(random.Next(1, 3 * SmallBlock), data.Length - offset);
            accumulator.Append(data.Slice(offset, take));
            offset += take;
        }
    }

    private static byte[] ExpectedRoot(MerkleTree tree, byte[] data, int blockSize) =>
        tree.ComputeRootOfBlocks(new MemoryStream(data), blockSize);

    /// <summary>
    /// Verifies that a newly created accumulator reports the tree's hash length and the requested block size, no
    /// input, no leaves and an unfinished state.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void CreateBlockAccumulator_WhenCreated_ShouldExposeItsConfigurationAndAnEmptyState()
    {
        MerkleTree tree = CreateTree();
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);

        Assert.AreEqual(SmallBlock, accumulator.BlockSize);
        Assert.AreEqual(tree.HashLength, accumulator.HashLength);
        Assert.AreEqual(0L, accumulator.Length);
        Assert.AreEqual(0L, accumulator.LeafCount);
        Assert.IsFalse(accumulator.RetainsLeafHashes);
        Assert.IsFalse(accumulator.IsFinished);
    }

    /// <summary>
    /// Verifies that requesting retained leaf hashes is reported by the accumulator.
    /// </summary>
    [TestMethod]
    public void CreateBlockAccumulator_WhenLeafHashesAreRetained_ShouldReportRetention()
    {
        using MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock, retainLeafHashes: true);

        Assert.IsTrue(accumulator.RetainsLeafHashes);
    }

    /// <summary>
    /// Verifies that a block size that is not positive is rejected with the parameter named.
    /// </summary>
    /// <param name="blockSize">The invalid block size.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void CreateBlockAccumulator_WhenBlockSizeIsNotPositive_ShouldThrowArgumentOutOfRangeException(int blockSize)
    {
        MerkleTree tree = CreateTree();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = tree.CreateBlockAccumulator(blockSize);
        });

        Assert.AreEqual("blockSize", ex.ParamName);
    }
}
