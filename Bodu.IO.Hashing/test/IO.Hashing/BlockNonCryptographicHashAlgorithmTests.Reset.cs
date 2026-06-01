// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.Reset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// Verifies that <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Reset" /> clears the residual
    /// buffer accumulated by <c>ProcessBlocks</c> so that the next append cycle starts from an empty residual.
    /// </summary>
    [TestMethod]
    public void Reset_AfterAppend_ShouldClearResidualBuffer()
    {
        RecordingBlockHasher hasher = new();
        hasher.Append(new byte[] { 0x01, 0x02, 0x03 });  // residual = 3 bytes
        hasher.Reset();

        // After Reset, a subsequent 4-byte append should produce exactly one aligned block consisting of the new
        // bytes only (no carry-over from the discarded residual).
        hasher.Append(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD });

        Assert.AreEqual(1, hasher.Blocks.Count);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, hasher.Blocks[0]);
    }

}
