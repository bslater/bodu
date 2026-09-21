// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockAccumulatorTests.FinishComputation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MerkleBlockAccumulatorTests
{
    /// <summary>
    /// Verifies that the computation returned after retaining leaf hashes matches the pull-style block computation
    /// in root, length, block size and every leaf hash.
    /// </summary>
    [TestMethod]
    public void FinishComputation_WhenLeafHashesAreRetained_ShouldMatchThePullStyleComputation()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] data = SeededInput((5 * SmallBlock) + 2);
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock, retainLeafHashes: true);
        AppendInRandomChunks(accumulator, data, seed: 7);

        MerkleBlockComputation actual = accumulator.FinishComputation();
        MerkleBlockComputation expected = tree.ComputeBlocked(data, SmallBlock);

        Assert.AreEqual(Hex(expected.Root), Hex(actual.Root));
        Assert.AreEqual(expected.InputLength, actual.InputLength);
        Assert.AreEqual(expected.BlockSize, actual.BlockSize);
        CollectionAssert.AreEqual(
            expected.LeafHashes.Select(leaf => Hex(leaf)).ToList(),
            actual.LeafHashes.Select(leaf => Hex(leaf)).ToList());
    }

    /// <summary>
    /// Verifies that every block of the input can be proved against the accumulator's bound root with a path built
    /// from the retained leaf hashes, and that the block one position over is rejected.
    /// </summary>
    [TestMethod]
    public void FinishComputation_WhenPathsAreBuiltFromTheRetainedLeaves_ShouldVerifyEveryBlockAgainstTheBoundRoot()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] data = SeededInput((6 * SmallBlock) + 5);
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock, retainLeafHashes: true);
        AppendInRandomChunks(accumulator, data, seed: 11);

        MerkleBlockComputation computation = accumulator.FinishComputation();
        byte[] boundRoot = accumulator.FinishBound();

        for (long index = 0; index < computation.BlockCount; index++)
        {
            byte[][] path = tree.AuthenticationPath(computation.LeafHashes, index);
            ReadOnlyMemory<byte>[] steps = Array.ConvertAll(path, step => (ReadOnlyMemory<byte>)step);
            ReadOnlySpan<byte> block = data.AsSpan((int)computation.BlockOffset(index), computation.BlockLength(index));

            Assert.IsTrue(tree.VerifyBlockInclusion(boundRoot, data.Length, SmallBlock, index, block, steps), $"block {index}");
            Assert.IsFalse(tree.VerifyBlockInclusion(boundRoot, data.Length, SmallBlock, (index + 1) % computation.BlockCount, block, steps), $"block {index} at the wrong index");
        }
    }

    /// <summary>
    /// Verifies that asking for the computation without having retained leaf hashes throws
    /// <see cref="InvalidOperationException" /> and does not finish the accumulator.
    /// </summary>
    [TestMethod]
    public void FinishComputation_WhenLeafHashesWereNotRetained_ShouldThrowInvalidOperationException()
    {
        using MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock);
        accumulator.Append(SeededInput(SmallBlock + 1));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = accumulator.FinishComputation();
        });

        Assert.IsFalse(accumulator.IsFinished);
    }
}
