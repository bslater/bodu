// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockAccumulatorTests.Finish.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class MerkleBlockAccumulatorTests
{
    /// <summary>
    /// Verifies that finishing with nothing appended yields the empty tree's root, the hash of zero bytes.
    /// </summary>
    [TestMethod]
    public void Finish_WhenNothingWasAppended_ShouldReturnTheEmptyTreeRoot()
    {
        using MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock);

        byte[] root = accumulator.Finish();

        Assert.AreEqual(Hex(SHA256.HashData([])), Hex(root));
        Assert.IsTrue(accumulator.IsFinished);
        Assert.AreEqual(0L, accumulator.LeafCount);
    }

    /// <summary>
    /// Verifies that repeated finishes return the same root without re-hashing, as separate arrays.
    /// </summary>
    [TestMethod]
    public void Finish_WhenCalledTwice_ShouldReturnTheSameRootAsADistinctArray()
    {
        using MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock);
        accumulator.Append(SeededInput(SmallBlock + 3));

        byte[] first = accumulator.Finish();
        byte[] second = accumulator.Finish();

        Assert.AreEqual(Hex(first), Hex(second));
        Assert.AreNotSame(first, second);
        Assert.AreEqual(2L, accumulator.LeafCount, "the tail is hashed once");
    }

    /// <summary>
    /// Verifies that a partial final block is hashed at its actual length: an input one byte short of a block and the
    /// same input zero-extended to a full block produce different roots.
    /// </summary>
    [TestMethod]
    public void Finish_WhenFinalBlockIsShort_ShouldHashItAtItsActualLength()
    {
        MerkleTree tree = CreateTree();
        byte[] shortInput = SeededInput(SmallBlock - 1);
        byte[] padded = new byte[SmallBlock];
        shortInput.CopyTo(padded, 0);

        using MerkleBlockAccumulator first = tree.CreateBlockAccumulator(SmallBlock);
        using MerkleBlockAccumulator second = tree.CreateBlockAccumulator(SmallBlock);
        first.Append(shortInput);
        second.Append(padded);

        Assert.AreNotEqual(Hex(first.Finish()), Hex(second.Finish()));
    }

    /// <summary>
    /// Verifies that a supplied recorder receives a trace that validates and ends at the finished root.
    /// </summary>
    [TestMethod]
    public void Finish_WhenDiagnosticsAreSupplied_ShouldRecordAValidatingTraceEndingAtTheRoot()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock, diagnostics: diagnostics);
        AppendInChunks(accumulator, SeededInput((5 * SmallBlock) + 2), 3);

        byte[] root = accumulator.Finish();

        Assert.IsTrue(diagnostics.Validate(SHA256.Create, out IReadOnlyList<string> errors), string.Join("; ", errors));
        Assert.IsNotNull(diagnostics.Root);
        Assert.AreEqual(Hex(root), Hex(diagnostics.Root.Hash));
        Assert.AreEqual(6, diagnostics.GetLevel(0).Count);
    }
}
