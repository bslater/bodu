// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ConstantTimeDifference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ConstantTimeDifference" /> returns zero for identical spans,
    /// including the empty span.
    /// </summary>
    [TestMethod]
    public void ConstantTimeDifference_WhenSpansAreEqual_ShouldReturnZero()
    {
        byte[] left = [0x00, 0x7F, 0x80, 0xFF];
        byte[] right = (byte[])left.Clone();

        Assert.AreEqual(0, CryptographyHelper.ConstantTimeDifference(left, right));
        Assert.AreEqual(0, CryptographyHelper.ConstantTimeDifference(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ConstantTimeDifference" /> returns a non-zero accumulator when a
    /// single byte differs, wherever that byte sits — first, middle, or last position — so no position is exempt from
    /// the comparison.
    /// </summary>
    [TestMethod]
    [DataRow("first", 0)]
    [DataRow("middle", 7)]
    [DataRow("last", 15)]
    public void ConstantTimeDifference_WhenSingleByteDiffers_ShouldReturnNonZero(string testName, int index)
    {
        byte[] left = new byte[16];
        byte[] right = new byte[16];
        right[index] ^= 0x01;

        Assert.AreNotEqual(0, CryptographyHelper.ConstantTimeDifference(left, right),
            $"A difference at the {testName} byte must produce a non-zero accumulator.");
    }

    /// <summary>
    /// Verifies that the accumulator is the OR of the byte-wise XOR differences, so multiple differing bytes fold
    /// together rather than cancelling out.
    /// </summary>
    [TestMethod]
    public void ConstantTimeDifference_WhenMultipleBytesDiffer_ShouldAccumulateXorDifferences()
    {
        byte[] left = [0x0F, 0x00, 0xF0];
        byte[] right = [0x00, 0x00, 0x00];

        Assert.AreEqual(0xFF, CryptographyHelper.ConstantTimeDifference(left, right));
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ConstantTimeDifference" /> throws
    /// <see cref="ArgumentException" /> when the spans differ in length — a length mismatch is a caller error, not a
    /// comparison outcome.
    /// </summary>
    [TestMethod]
    public void ConstantTimeDifference_WhenLengthsDiffer_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = CryptographyHelper.ConstantTimeDifference(new byte[16], new byte[15]);
        });
    }
}
