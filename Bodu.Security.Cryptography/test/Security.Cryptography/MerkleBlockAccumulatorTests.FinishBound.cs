// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockAccumulatorTests.FinishBound.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public partial class MerkleBlockAccumulatorTests
{
    /// <summary>
    /// Verifies that the bound root is the tree's <c>BindRoot</c> of the plain root and the appended length.
    /// </summary>
    [TestMethod]
    public void FinishBound_WhenFinished_ShouldBindTheAppendedLengthIntoTheRoot()
    {
        MerkleTree tree = CreateTree();
        byte[] data = SeededInput((3 * SmallBlock) + 5);
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);
        accumulator.Append(data);

        byte[] bound = accumulator.FinishBound();

        Assert.AreEqual(Hex(tree.BindRoot(accumulator.Finish(), data.Length)), Hex(bound));
        Assert.AreNotEqual(Hex(accumulator.Finish()), Hex(bound));
    }

    /// <summary>
    /// Verifies that the published FallbackPlan bound roots over one-mebibyte leaves are reproduced when the counter
    /// stream is appended in seeded random-sized chunks, so a writer feeding the accumulator from arbitrary writes
    /// publishes exactly the pull-style commitment.
    /// </summary>
    /// <param name="kat">The input length and the published bound root.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(MerkleTreeTests.WholePreimageBoundRoots), typeof(MerkleTreeTests), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void FinishBound_WhenTheCounterStreamIsAppendedInRandomChunks_ShouldReproducePublishedBoundRoots(ValidKat<int, string> kat)
    {
        byte[] preimage = MerkleTreeTests.CounterStream(kat.Input);
        using MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(MerkleTreeTests.OneMebibyteBlock);

        var random = new Random(kat.Input);
        int offset = 0;
        while (offset < preimage.Length)
        {
            int take = Math.Min(random.Next(1, 300_000), preimage.Length - offset);
            accumulator.Append(preimage.AsSpan(offset, take));
            offset += take;
        }

        Assert.AreEqual(kat.Expected, Hex(accumulator.FinishBound()));
        Assert.AreEqual(kat.Input, accumulator.Length);
    }

    /// <summary>
    /// Verifies that the bound root over nothing binds a zero length to the empty tree's root.
    /// </summary>
    [TestMethod]
    public void FinishBound_WhenNothingWasAppended_ShouldBindZeroToTheEmptyTreeRoot()
    {
        MerkleTree tree = CreateTree();
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);

        Assert.AreEqual(Hex(tree.BindRoot(tree.ComputeRootOfBlocks(new MemoryStream(), SmallBlock), 0)), Hex(accumulator.FinishBound()));
    }
}
