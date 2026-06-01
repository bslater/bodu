// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.TryDepadBlock.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryDepadBlock" /> returns <c>true</c> for valid input input across modes.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidDepaddingCases))]
    public void TryDepadBlock_WhenValidPadding_ShouldReturnTrue(
        PaddingMode padding, string inputHex, string expectedHex)
    {
        var input = Convert.FromHexString(inputHex);
        Span<byte> destination = new byte[input.Length];

        var result = CryptographyHelper.TryDepadBlock(padding, input.Length * 8, input, destination, out var written);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryDepadBlock" /> returns the correct uninput byte count for valid input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidDepaddingCases))]
    public void TryDepadBlock_WhenValidPadding_ShouldReturnExpectedLength(
        PaddingMode padding, string inputHex, string expectedHex)
    {
        var input = Convert.FromHexString(inputHex);
        var expected = Convert.FromHexString(expectedHex);
        var expectedLength = expected.Length;
        Span<byte> destination = new byte[input.Length];

        CryptographyHelper.TryDepadBlock(padding, input.Length * 8, input, destination, out var written);

        Assert.AreEqual(expectedLength, written);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryDepadBlock" /> returns the correct uninput content for valid input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidDepaddingCases))]
    public void TryDepadBlock_WhenValidPadding_ShouldReturnExpectedResult(
        PaddingMode padding, string inputHex, string expectedHex)
    {
        var input = Convert.FromHexString(inputHex);
        var expected = Convert.FromHexString(expectedHex);
        var expectedLength = expected.Length;
        Span<byte> destination = new byte[input.Length];

        CryptographyHelper.TryDepadBlock(padding, input.Length * 8, input, destination, out var written);

        CollectionAssert.AreEqual(input.Take(expectedLength).ToArray(), destination.Slice(0, written).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryDepadBlock" /> returns <c>false</c> for invalid input input across supported modes.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.InvalidDepaddingCases))]
    public void TryDepadBlock_WhenInvalidPadding_ShouldReturnFalse(PaddingMode padding, string inputHex, int blockSizeBytes, Type exceptionType)
    {
        var input = Convert.FromHexString(inputHex);
        Span<byte> destination = new byte[input.Length];

        var result = CryptographyHelper.TryDepadBlock(padding, blockSizeBytes * 8, input, destination, out var written);

        Assert.IsFalse(result);
        Assert.AreEqual(0, written);
    }
}
