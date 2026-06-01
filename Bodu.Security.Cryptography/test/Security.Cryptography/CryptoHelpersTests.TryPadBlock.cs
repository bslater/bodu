// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.TryPadBlock.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that TryPadBlock returns <c>true</c> when provided with valid input, indicating successful padding application across
    /// supported padding modes.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidPaddingCases))]
    public void TryPadBlock_WhenValidInput_ShouldReturnTrue(
        PaddingMode padding, string inputHex, int blockSizeBytes, string expectedHex)
    {
        var input = Convert.FromHexString(inputHex);
        var expectedLength = expectedHex.Length / 2;
        Span<byte> destination = new byte[expectedLength];

        var result = CryptographyHelper.TryPadBlock(padding, blockSizeBytes * 8, input, destination, out var written);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that TryPadBlock writes the expected number of padded bytes to the destination span for valid input. The result length
    /// must match the expected padded length derived from the input and padding mode.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidPaddingCases))]
    public void TryPadBlock_WhenValidInput_ShouldReturnExpectedLength(
        PaddingMode padding, string inputHex, int blockSizeBytes, string expectedHex)
    {
        var input = Convert.FromHexString(inputHex);
        var expectedLength = expectedHex.Length / 2;
        Span<byte> destination = new byte[expectedLength];

        var result = CryptographyHelper.TryPadBlock(padding, blockSizeBytes * 8, input, destination, out var written);

        Assert.AreEqual(expectedLength, written);
    }

    /// <summary>
    /// Verifies that TryPadBlock preserves the original input bytes in the output span for all valid padding modes, ensuring that
    /// padding is only appended and not destructive to source data.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidPaddingCases))]
    public void TryPadBlock_WhenValidInput_ShouldPreserveOriginalBytes(
        PaddingMode padding, string inputHex, int blockSizeBytes, string expectedHex)
    {
        var input = Convert.FromHexString(inputHex);
        var expectedLength = expectedHex.Length / 2;
        Span<byte> destination = new byte[expectedLength];

        CryptographyHelper.TryPadBlock(padding, blockSizeBytes * 8, input, destination, out var written);

        Assert.IsTrue(destination.Slice(0, input.Length).SequenceEqual(input));
    }

    /// <summary>
    /// Verifies that TryPadBlock returns <c>false</c> when input conditions are invalid, including unsupported padding modes, invalid
    /// block sizes, or insufficient destination buffer length.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.InvalidPaddingCases))]
    public void TryPadBlock_WhenInvalidInput_ShouldReturnFalse(
        PaddingMode padding, string inputHex, int blockSizeBytes, Type exceptionType, int? destinationLength = null)
    {
        var input = Convert.FromHexString(inputHex);
        destinationLength ??= (blockSizeBytes * 2);

        Span<byte> destination = destinationLength.Value <= 128
            ? stackalloc byte[destinationLength.Value]
            : new byte[destinationLength.Value];

        var result = CryptographyHelper.TryPadBlock(padding, blockSizeBytes * 8, input, destination, out var written);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that TryPadBlock writes <c>0</c> bytes to the destination when input is invalid or padding fails due to unsupported
    /// configuration, aligning with a <c>false</c> return hashValue.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.InvalidPaddingCases))]
    public void TryPadBlock_WhenInvalidInput_ShouldReturnZeroWritten(
        PaddingMode padding, string inputHex, int blockSizeBytes, Type exceptionType, int? destinationLength = null)
    {
        var input = Convert.FromHexString(inputHex);
        destinationLength ??= (blockSizeBytes * 2);

        Span<byte> destination = destinationLength.Value <= 128
            ? stackalloc byte[destinationLength.Value]
            : new byte[destinationLength.Value];

        _ = CryptographyHelper.TryPadBlock(padding, blockSizeBytes * 8, input, destination, out var written);

        Assert.AreEqual(0, written);
    }
}
