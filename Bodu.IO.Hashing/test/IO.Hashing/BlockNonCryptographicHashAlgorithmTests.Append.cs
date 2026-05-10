// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.Append.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{
    /// <summary>
    /// Verifies that when the residual buffer plus the incoming input exactly fill one block, the combined block
    /// is emitted to <c>ProcessBlock</c> and the residual is cleared.
    /// </summary>
    [TestMethod]
    public void Append_WhenResidualPlusInputExactlyFillsBlock_ShouldEmitOneAlignedBlock()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x01, 0x02 });              // residual = 2 bytes
        hasher.Append(new byte[] { 0x03, 0x04 });              // fills the block exactly

        Assert.AreEqual(1, hasher.Blocks.Count);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, hasher.Blocks[0]);
    }

    /// <summary>
    /// Verifies that when the combined residual plus incoming input is smaller than one block, no block is
    /// emitted and the bytes are retained in the residual buffer.
    /// </summary>
    [TestMethod]
    public void Append_WhenInputSmallerThanRemainingCapacity_ShouldAccumulateInResidualOnly()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x01 });
        hasher.Append(new byte[] { 0x02 });

        Assert.AreEqual(0, hasher.Blocks.Count);
    }

    /// <summary>
    /// Verifies that an input consisting of multiple full blocks (with no residual) is emitted as one
    /// <c>ProcessBlock</c> invocation per block, in order.
    /// </summary>
    [TestMethod]
    public void Append_WhenInputSpansMultipleFullBlocks_ShouldEmitEachBlockInOrder()
    {
        RecordingBlockHasher hasher = new();
        byte[] input =
        [
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C,
        ];
        hasher.Append(input);

        Assert.AreEqual(3, hasher.Blocks.Count);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, hasher.Blocks[0]);
        CollectionAssert.AreEqual(new byte[] { 0x05, 0x06, 0x07, 0x08 }, hasher.Blocks[1]);
        CollectionAssert.AreEqual(new byte[] { 0x09, 0x0A, 0x0B, 0x0C }, hasher.Blocks[2]);
    }
}
