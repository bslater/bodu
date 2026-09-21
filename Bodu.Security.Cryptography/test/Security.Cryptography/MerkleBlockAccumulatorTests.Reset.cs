// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockAccumulatorTests.Reset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MerkleBlockAccumulatorTests
{
    /// <summary>
    /// Verifies that a reset accumulator reproduces the root a fresh accumulator computes over the next input, with
    /// its counters and finished state cleared.
    /// </summary>
    [TestMethod]
    public void Reset_WhenReusedAfterAFinish_ShouldReproduceAFreshAccumulatorsRoot()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] first = SeededInput((2 * SmallBlock) + 1, seed: 1);
        byte[] second = SeededInput((4 * SmallBlock) + 6, seed: 2);
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);

        accumulator.Append(first);
        _ = accumulator.Finish();
        accumulator.Reset();

        Assert.IsFalse(accumulator.IsFinished);
        Assert.AreEqual(0L, accumulator.Length);
        Assert.AreEqual(0L, accumulator.LeafCount);

        accumulator.Append(second);

        Assert.AreEqual(Hex(ExpectedRoot(tree, second, SmallBlock)), Hex(accumulator.Finish()));
    }

    /// <summary>
    /// Verifies that resetting mid-input discards the partial block, so bytes appended before the reset never reach
    /// the next root.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalledMidBlock_ShouldDiscardThePartialBlock()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] data = SeededInput(SmallBlock + 2);
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);

        accumulator.Append(SeededInput(SmallBlock - 3, seed: 99));
        accumulator.Reset();
        accumulator.Append(data);

        Assert.AreEqual(Hex(ExpectedRoot(tree, data, SmallBlock)), Hex(accumulator.Finish()));
    }

    /// <summary>
    /// Verifies that a computation handed out before a reset keeps its own leaf hashes.
    /// </summary>
    [TestMethod]
    public void Reset_WhenAComputationWasHandedOut_ShouldLeaveItsLeafHashesIntact()
    {
        using MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock, retainLeafHashes: true);
        accumulator.Append(SeededInput(3 * SmallBlock));
        MerkleBlockComputation computation = accumulator.FinishComputation();

        accumulator.Reset();
        accumulator.Append(SeededInput(SmallBlock, seed: 5));
        _ = accumulator.FinishComputation();

        Assert.AreEqual(3, computation.LeafHashes.Count);
        Assert.AreEqual(3L, computation.BlockCount);
    }
}
