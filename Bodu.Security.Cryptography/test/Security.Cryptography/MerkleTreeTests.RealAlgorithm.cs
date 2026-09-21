// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.RealAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Cross-checks against real digests of several widths, and the order and configuration sensitivity a real hash
/// exposes.
/// </summary>
public partial class MerkleTreeTests
{
    private static Func<HashAlgorithm> AlgorithmFactory(string algorithm) =>
        algorithm switch
        {
            "SHA1" => SHA1.Create,
            "SHA384" => SHA384.Create,
            "SHA512" => SHA512.Create,
            _ => SHA256.Create,
        };

    private static byte[] ComputeReferenceMerkleRoot(Func<HashAlgorithm> factory, byte[] data, int blockSize, int fanOut)
    {
        using HashAlgorithm hasher = factory();

        var level = new List<byte[]>();
        for (int offset = 0; offset < data.Length; offset += blockSize)
        {
            int length = Math.Min(blockSize, data.Length - offset);
            byte[] leaf = new byte[1 + length];
            leaf[0] = MerkleTree.LeafPrefix;
            Array.Copy(data, offset, leaf, 1, length);
            level.Add(hasher.ComputeHash(leaf));
        }

        while (level.Count > 1)
        {
            var next = new List<byte[]>();
            for (int start = 0; start < level.Count; start += fanOut)
            {
                int groupSize = Math.Min(fanOut, level.Count - start);
                if (groupSize == 1)
                {
                    next.Add(level[start]);
                    continue;
                }

                byte[] combined = [MerkleTree.InternalNodePrefix, .. level.GetRange(start, groupSize).SelectMany(h => h)];
                next.Add(hasher.ComputeHash(combined));
            }

            level = next;
        }

        return level[0];
    }

    /// <summary>
    /// Verifies that an alternative digest width produces a root of that width equal to an independent reference
    /// reduction, at a wide fan-out, on the sequential and the parallel instance.
    /// </summary>
    /// <param name="algorithm">The algorithm name.</param>
    /// <param name="expectedDigestLength">The algorithm's digest width in bytes.</param>
    [TestMethod]
    [DataRow("SHA1", 20)]
    [DataRow("SHA384", 48)]
    [DataRow("SHA512", 64)]
    public void ComputeRootOfBlocks_WhenAlternativeAlgorithmUsed_ShouldMatchReferenceReductionAndDigestSize(string algorithm, int expectedDigestLength)
    {
        Func<HashAlgorithm> factory = AlgorithmFactory(algorithm);
        byte[] data = MakeData(50);
        byte[] expected = ComputeReferenceMerkleRoot(factory, data, blockSize: 4, fanOut: 3);

        byte[] sequential = new MerkleTree(factory, fanOut: 3).ComputeRootOfBlocks(data, 4);
        byte[] parallel = new MerkleTree(factory, fanOut: 3, maxDegreeOfParallelism: -1).ComputeRootOfBlocks(data, 4);

        Assert.HasCount(expectedDigestLength, sequential, $"{algorithm} root should be {expectedDigestLength} bytes.");
        CollectionAssert.AreEqual(expected, sequential, $"{algorithm} sequential root diverged from the reference reduction.");
        CollectionAssert.AreEqual(expected, parallel, $"{algorithm} parallel root diverged from the reference reduction.");
    }

    /// <summary>
    /// Verifies that leaves stay length-bound under a 64-byte digest.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenAlternativeAlgorithmUsed_ShouldStillLengthBindLeaves()
    {
        var tree = new MerkleTree(SHA512.Create);

        Assert.AreNotEqual(
            Hex(tree.ComputeRootOfBlocks(new byte[] { 0x41 }, 8)),
            Hex(tree.ComputeRootOfBlocks(new byte[] { 0x41, 0x00 }, 8)),
            "SHA-512 leaves are not length-bound — trailing-zero inputs collided.");
    }

    /// <summary>
    /// Verifies that reversing the input changes the root.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenInputReversed_ShouldProduceDifferentRoot()
    {
        byte[] forward = MakeData(64);
        byte[] reversed = [.. Enumerable.Reverse(forward)];
        MerkleTree tree = CreateTree();

        Assert.AreNotEqual(Hex(tree.ComputeRootOfBlocks(forward, VectorBlockSize)), Hex(tree.ComputeRootOfBlocks(reversed, VectorBlockSize)));
    }

    /// <summary>
    /// Verifies that swapping two adjacent leaf blocks changes the root — siblings are ordered.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenAdjacentBlocksSwapped_ShouldProduceDifferentRoot()
    {
        byte[] original = MakeData(16);
        byte[] swapped = (byte[])original.Clone();
        for (int index = 0; index < 4; index++)
            (swapped[index], swapped[index + 4]) = (swapped[index + 4], swapped[index]);

        MerkleTree tree = CreateTree();

        Assert.AreNotEqual(Hex(tree.ComputeRootOfBlocks(original, 4)), Hex(tree.ComputeRootOfBlocks(swapped, 4)));
    }

    /// <summary>
    /// Verifies that repeated computations on one instance are identical — the instance holds no state between calls.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenCalledRepeatedly_ShouldProduceIdenticalResults()
    {
        byte[] data = MakeData(100);
        MerkleTree tree = CreateTree();

        string first = Hex(tree.ComputeRootOfBlocks(data, VectorBlockSize));

        Assert.AreEqual(first, Hex(tree.ComputeRootOfBlocks(data, VectorBlockSize)));
        Assert.AreEqual(first, Hex(tree.ComputeRootOfBlocks(data, VectorBlockSize)));
        Assert.HasCount(32, tree.ComputeRootOfBlocks(data, VectorBlockSize));
    }

    /// <summary>
    /// Verifies that the fan-out and the block size each change the root — the topology is part of the commitment.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenFanOutOrBlockSizeChanges_ShouldProduceDifferentRoot()
    {
        byte[] data = MakeData(64);
        MerkleTree binary = CreateTree();

        Assert.AreNotEqual(Hex(binary.ComputeRootOfBlocks(data, 4)), Hex(new MerkleTree(SHA256.Create, fanOut: 4).ComputeRootOfBlocks(data, 4)), "fan-out");
        Assert.AreNotEqual(Hex(binary.ComputeRootOfBlocks(data, 4)), Hex(binary.ComputeRootOfBlocks(data, 8)), "block size");
    }

    /// <summary>
    /// Verifies two hand-specified SHA-256 shapes: a two-leaf tree and a three-leaf tree with a promoted leaf.
    /// </summary>
    /// <param name="length">The input length in bytes.</param>
    [TestMethod]
    [DataRow(8)]
    [DataRow(12)]
    public void ComputeRootOfBlocks_WhenSha256KnownShape_ShouldMatchTheReferenceReduction(int length)
    {
        byte[] data = MakeData(length);

        CollectionAssert.AreEqual(ComputeReferenceMerkleRoot(SHA256.Create, data, 4, 2), CreateTree().ComputeRootOfBlocks(data, 4));
    }
}
