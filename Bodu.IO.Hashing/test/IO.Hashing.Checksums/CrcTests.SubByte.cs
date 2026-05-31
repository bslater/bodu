// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.SubByte.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcTests
{

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[])" /> on a sub-byte-width CRC produces the same
    /// digest as a single-shot hash of the concatenated input, exercising the bit-wise processing path
    /// (<c>hashSizeBits &lt; 8</c>) under resumption.
    /// </summary>
    /// <param name="standardId">A sub-byte CRC catalogue entry under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(CrcStandards.CRC3_GSM)]
    [DataRow(CrcStandards.CRC3_ROHC)]
    [DataRow(CrcStandards.CRC4_G704)]
    [DataRow(CrcStandards.CRC4_INTERLAKEN)]
    [DataRow(CrcStandards.CRC5_EPCC1G2)]
    [DataRow(CrcStandards.CRC5_G704)]
    [DataRow(CrcStandards.CRC5_USB)]
    [DataRow(CrcStandards.CRC6_CDMA2000A)]
    [DataRow(CrcStandards.CRC6_CDMA2000B)]
    [DataRow(CrcStandards.CRC6_DARC)]
    [DataRow(CrcStandards.CRC6_G704)]
    [DataRow(CrcStandards.CRC6_GSM)]
    [DataRow(CrcStandards.CRC7_MMC)]
    [DataRow(CrcStandards.CRC7_ROHC)]
    [DataRow(CrcStandards.CRC7_UMTS)]
    public void ComputeHashFrom_WhenSubByteWidthAndSplitInput_ShouldEqualOneShot(CrcStandards standardId)
    {
        CrcStandard standard = CrcStandard.Get(standardId);

        ReadOnlySpan<byte> input = s_revEngCheckInput;
        byte[] first = input.Slice(0, 4).ToArray();
        byte[] rest = input.Slice(4).ToArray();

        var firstHash = new Crc(standard).ComputeHash(first);
        var resumed = new Crc(standard).ComputeHashFrom(firstHash, rest);

        var oneShot = new Crc(standard).ComputeHash(input);
        CollectionAssert.AreEqual(oneShot, resumed);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetCurrentHash" /> on a sub-byte-width CRC is
    /// non-destructive: calling it between appends returns the snapshot digest for the data processed so far and
    /// leaves the accumulator intact so that a subsequent append continues from the same position.
    /// </summary>
    /// <param name="standardId">A sub-byte CRC catalogue entry under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(CrcStandards.CRC3_GSM)]
    [DataRow(CrcStandards.CRC4_G704)]
    [DataRow(CrcStandards.CRC5_USB)]
    [DataRow(CrcStandards.CRC6_GSM)]
    [DataRow(CrcStandards.CRC7_MMC)]
    public void GetCurrentHash_WhenSubByteWidth_ShouldBeNonDestructive(CrcStandards standardId)
    {
        CrcStandard standard = CrcStandard.Get(standardId);

        ReadOnlySpan<byte> input = s_revEngCheckInput;

        Crc subject = new(standard);
        subject.Append(input.Slice(0, 4));
        var snapshotAfterFirstHalf = subject.GetCurrentHash();

        Crc baselineFirstHalf = new(standard);
        var expectedFirstHalf = baselineFirstHalf.ComputeHash(input.Slice(0, 4));
        CollectionAssert.AreEqual(expectedFirstHalf, snapshotAfterFirstHalf);

        // Snapshot must not have mutated the accumulator — continuing to append the rest still produces the full
        // catalogue digest.
        subject.Append(input.Slice(4));
        var finalDigest = subject.GetCurrentHash();

        var expectedFull = new Crc(standard).ComputeHash(input);
        CollectionAssert.AreEqual(expectedFull, finalDigest);
    }

}
