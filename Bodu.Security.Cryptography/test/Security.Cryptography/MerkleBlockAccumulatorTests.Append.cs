// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockAccumulatorTests.Append.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MerkleBlockAccumulatorTests
{
    /// <summary>The chunk sizes the boundary sweep appends with, straddling the block size from both sides.</summary>
    private static readonly int[] ChunkSizes = [1, SmallBlock - 1, SmallBlock, SmallBlock + 1, int.MaxValue];

    /// <summary>
    /// Verifies that the root is independent of how the input is split across <c>Append</c> calls, and equals the
    /// pull-style block root, for every input length up to two blocks and a tail and every chunking that straddles a
    /// block boundary.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Append_WhenChunkedAcrossEveryBlockBoundary_ShouldReproduceTheBlockRoot()
    {
        MerkleTree tree = CreateTree();
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);

        for (int length = 0; length <= (2 * SmallBlock) + 3; length++)
        {
            byte[] data = SeededInput(length, seed: length);
            string expected = Hex(ExpectedRoot(tree, data, SmallBlock));

            foreach (int chunkSize in ChunkSizes)
            {
                accumulator.Reset();
                AppendInChunks(accumulator, data, chunkSize);

                Assert.AreEqual(expected, Hex(accumulator.Finish()), $"length {length}, chunk {chunkSize}");
                Assert.AreEqual(length, accumulator.Length);
            }
        }
    }

    /// <summary>
    /// Verifies that byte-at-a-time and whole-input appends produce the same root as the pull-style block root for a
    /// two-block-and-a-tail input.
    /// </summary>
    /// <param name="chunkSize">The number of bytes per <c>Append</c> call.</param>
    [TestMethod]
    [TestCategory("Smoke")]
    [DataRow(1)]
    [DataRow(SmallBlock + 1)]
    [DataRow(int.MaxValue)]
    public void Append_WhenChunked_ShouldReproduceTheBlockRoot(int chunkSize)
    {
        MerkleTree tree = CreateTree();
        byte[] data = SeededInput((2 * SmallBlock) + 3);
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);

        AppendInChunks(accumulator, data, chunkSize);

        Assert.AreEqual(Hex(ExpectedRoot(tree, data, SmallBlock)), Hex(accumulator.Finish()));
    }

    /// <summary>
    /// Verifies that <c>Length</c> counts every appended byte while <c>LeafCount</c> advances only as blocks complete,
    /// and that a partial block is not counted as a leaf until the accumulator is finished.
    /// </summary>
    [TestMethod]
    public void Append_WhenBytesArrive_ShouldAdvanceLengthImmediatelyAndLeafCountPerCompletedBlock()
    {
        using MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock);
        byte[] data = SeededInput((2 * SmallBlock) + 3);

        accumulator.Append(data.AsSpan(0, SmallBlock - 1));
        Assert.AreEqual(SmallBlock - 1, accumulator.Length);
        Assert.AreEqual(0L, accumulator.LeafCount);

        accumulator.Append(data.AsSpan(SmallBlock - 1, 1));
        Assert.AreEqual(SmallBlock, accumulator.Length);
        Assert.AreEqual(1L, accumulator.LeafCount);

        accumulator.Append(data.AsSpan(SmallBlock));
        Assert.AreEqual(data.Length, accumulator.Length);
        Assert.AreEqual(2L, accumulator.LeafCount, "the three-byte tail is not a leaf until Finish");

        _ = accumulator.Finish();
        Assert.AreEqual(3L, accumulator.LeafCount);
    }

    /// <summary>
    /// Verifies that appending an empty span is accepted and changes nothing.
    /// </summary>
    [TestMethod]
    public void Append_WhenSpanIsEmpty_ShouldChangeNothing()
    {
        MerkleTree tree = CreateTree();
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);

        accumulator.Append([]);
        accumulator.Append(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0L, accumulator.Length);
        Assert.AreEqual(Hex(ExpectedRoot(tree, [], SmallBlock)), Hex(accumulator.Finish()));
    }

    /// <summary>
    /// Verifies that an input ending exactly on a block boundary produces no empty trailing leaf.
    /// </summary>
    [TestMethod]
    public void Append_WhenInputEndsOnABlockBoundary_ShouldNotEmitAnEmptyLeaf()
    {
        MerkleTree tree = CreateTree();
        byte[] data = SeededInput(3 * SmallBlock);
        using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);

        accumulator.Append(data);
        byte[] root = accumulator.Finish();

        Assert.AreEqual(3L, accumulator.LeafCount);
        Assert.AreEqual(Hex(ExpectedRoot(tree, data, SmallBlock)), Hex(root));
    }

    /// <summary>
    /// Verifies that appending after a finish throws <see cref="InvalidOperationException" /> and leaves the finished
    /// root unchanged.
    /// </summary>
    [TestMethod]
    public void Append_WhenFinished_ShouldThrowInvalidOperationException()
    {
        using MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock);
        accumulator.Append(SeededInput(5));
        byte[] root = accumulator.Finish();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            accumulator.Append(new byte[] { 0x01 });
        });

        Assert.AreEqual(Hex(root), Hex(accumulator.Finish()));
        Assert.AreEqual(5L, accumulator.Length);
    }
}
