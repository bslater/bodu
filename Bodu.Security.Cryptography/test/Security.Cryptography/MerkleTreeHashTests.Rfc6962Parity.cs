// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTests.Rfc6962Parity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Collections.Specialized;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Proves that <see cref="MerkleTreeHash" /> and <see cref="ParallelMerkleTreeHash" /> at a fan-out of two produce
/// bit-identical roots to <see cref="Rfc6962MerkleTree" /> over the same blocks, for every leaf count up to
/// sixty-four with and without a short tail — so an inclusion proof from the RFC 6962 type verifies against a root
/// from either hasher.
/// </summary>
/// <remarks>
/// The three types are facades over one shared fold, so this is less a test of arithmetic than a guard that the
/// facades keep feeding it the same leaves: the same block boundaries, the same tail handling, the same empty-input
/// root. The comparison crosses the package boundary through a test-only project reference.
/// </remarks>
public partial class MerkleTreeHashTests
{
    /// <summary>The block size the three types are compared at.</summary>
    private const int ParityBlockSize = 4;

    /// <summary>
    /// Verifies that the sequential hasher's root equals the RFC 6962 type's for every leaf count up to sixty-four,
    /// full and short-tailed.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void ComputeHash_WhenFanOutIsTwo_ShouldEqualRfc6962MerkleTreeForEveryLeafCountUpToSixtyFour()
    {
        var tree = new Rfc6962MerkleTree(SHA256.Create);
        using var hasher = new MerkleTreeHash(SHA256.Create, ParityBlockSize, fanOut: 2);

        foreach ((byte[] input, string label) in ParityInputs())
        {
            string expected = Convert.ToHexString(tree.ComputeBlocked(input, ParityBlockSize).Root);

            Assert.AreEqual(expected, Convert.ToHexString(hasher.ComputeHash(input)), label);
        }
    }

    /// <summary>
    /// Verifies that the parallel hasher's root equals the RFC 6962 type's for every leaf count up to sixty-four, full
    /// and short-tailed, over both its in-memory and its stream surface.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task ParallelComputeHash_WhenFanOutIsTwo_ShouldEqualRfc6962MerkleTreeForEveryLeafCountUpToSixtyFour()
    {
        var tree = new Rfc6962MerkleTree(SHA256.Create);
        using var hasher = new ParallelMerkleTreeHash(SHA256.Create, ParityBlockSize, fanOut: 2);

        foreach ((byte[] input, string label) in ParityInputs())
        {
            string expected = Convert.ToHexString(tree.ComputeBlocked(input, ParityBlockSize).Root);

            Assert.AreEqual(expected, Convert.ToHexString(hasher.ComputeHash(input)), label);
            Assert.AreEqual(expected, Convert.ToHexString(await hasher.ComputeHashAsync(new MemoryStream(input))), label);
        }
    }

    /// <summary>
    /// Verifies that an inclusion proof produced by the RFC 6962 type verifies against a root computed by the
    /// sequential hasher, which is the interoperability the shared tree buys.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenFanOutIsTwo_ShouldProduceRootsThatRfc6962InclusionProofsVerifyAgainst()
    {
        var tree = new Rfc6962MerkleTree(SHA256.Create);
        using var hasher = new MerkleTreeHash(SHA256.Create, ParityBlockSize, fanOut: 2);
        byte[] input = ParityInput(7 * ParityBlockSize + 1);   // eight leaves, the last a single byte

        byte[] root = hasher.ComputeHash(input);
        IReadOnlyList<byte[]> leafHashes = tree.ComputeBlocked(input, ParityBlockSize).LeafHashes;
        long blockCount = MerkleBlocks.BlockCount(input.Length, ParityBlockSize);

        for (long leaf = 0; leaf < blockCount; leaf++)
        {
            ReadOnlyMemory<byte>[] path = Array.ConvertAll(
                tree.AuthenticationPath(leafHashes, leaf),
                static step => (ReadOnlyMemory<byte>)step);
            int offset = (int)MerkleBlocks.BlockOffset(leaf, ParityBlockSize);
            int length = MerkleBlocks.BlockLength(input.Length, leaf, ParityBlockSize);

            Assert.IsTrue(
                tree.VerifyInclusion(root, blockCount, leaf, input.AsSpan(offset, length), path),
                $"leaf {leaf}");
        }
    }

    /// <summary>
    /// Enumerates inputs of zero to sixty-four full blocks, each also with a one-byte tail, labelled for failures.
    /// </summary>
    /// <returns>The inputs and their labels.</returns>
    private static IEnumerable<(byte[] Input, string Label)> ParityInputs()
    {
        for (int leafCount = 0; leafCount <= 64; leafCount++)
        {
            yield return (ParityInput(leafCount * ParityBlockSize), $"{leafCount} full blocks");
            yield return (ParityInput((leafCount * ParityBlockSize) + 1), $"{leafCount} full blocks + 1 byte");
        }
    }

    /// <summary>
    /// Returns the first <paramref name="length" /> bytes of the sequence <c>0x00, 0x01, 0x02, …</c>.
    /// </summary>
    /// <param name="length">The number of bytes.</param>
    /// <returns>The bytes.</returns>
    private static byte[] ParityInput(int length)
    {
        byte[] bytes = new byte[length];
        for (int i = 0; i < length; i++)
            bytes[i] = (byte)(i & 0xFF);

        return bytes;
    }
}
