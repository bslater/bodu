// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.GetCurrentHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// Verifies that when <c>ShouldPadFinalBlock</c> is <see langword="true" /> and
    /// <c>AllowUnalignedFinalBlock</c> is <see langword="false" />, <c>GetCurrentHashCore</c> slices the padded
    /// output into <see cref="BlockNonCryptographicHashAlgorithm{T}.BlockSizeBytes" />-sized chunks and invokes
    /// <c>ProcessBlock</c> once per chunk.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenShouldPadAndAlignedFinalBlock_ShouldSlicePaddedOutputIntoBlocks()
    {
        PaddingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x11, 0x22 });  // residual = 2 bytes — no blocks emitted yet
        Assert.AreEqual(0, hasher.Blocks.Count);

        _ = hasher.GetCurrentHash();

        // PadBlock returns two blocks of four bytes each; the padded payload must be sliced into exactly two
        // ProcessBlock calls on the cloned instance. Blocks is shared with the clone so the outer instance
        // witnesses the invocations.
        Assert.AreEqual(2, hasher.Blocks.Count);
        CollectionAssert.AreEqual(new byte[] { 0x11, 0x22, 0x00, 0x00 }, hasher.Blocks[0]);
        Assert.AreEqual(4, hasher.Blocks[1].Length);
    }

    /// <summary>
    /// Verifies that when <c>AllowUnalignedFinalBlock</c> is <see langword="true" />, <c>GetCurrentHashCore</c>
    /// forwards the full padded buffer (whose length is not necessarily a multiple of
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}.BlockSizeBytes" />) to <c>ProcessBlock</c> in a single
    /// call — the cloned instance must not throw despite the unaligned length.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenShouldPadAndUnalignedFinalBlockIsAllowed_ShouldForwardPaddedBufferWholesale()
    {
        UnalignedPaddingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x11, 0x22 });

        // The padded buffer returned by PadBlock is 7 bytes, which is not a multiple of the 4-byte block size.
        // With AllowUnalignedFinalBlock = true this must still succeed without throwing.
        var digest = hasher.GetCurrentHash();

        Assert.IsNotNull(digest);
        Assert.AreEqual(4, digest.Length);
    }

}
