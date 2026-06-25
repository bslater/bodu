// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.PadBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Provides valid input blocks for padding scenarios with expected padded outputs.
    /// </summary>
    public static IEnumerable<object[]> ValidPaddingCases() =>
    [
        // PaddingMode, InputHex, BlockSize, ExpectedHex Typical valid cases
        [PaddingMode.PKCS7, "102030", 8, "1020300505050505"],
        [PaddingMode.ANSIX923, "102030", 8, "1020300000000005"],
        [PaddingMode.ISO10126, "102030", 8, "102030????????05"],
        [PaddingMode.Zeros, "102030", 8, "1020300000000000"],
        [PaddingMode.None, "1020304050607080", 8, "1020304050607080"],

        // Full block input (PKCS7 adds full block of 8)
        [PaddingMode.PKCS7, "1122334455667788", 8, "11223344556677880808080808080808"],

        // Full block input (ANSIX923 adds full block of 8)
        [PaddingMode.ANSIX923, "1122334455667788", 8, "11223344556677880000000000000008"],

        // Full block input (ISO10126 random 7 + 8)
        [PaddingMode.ISO10126, "1122334455667788", 8, "1122334455667788??????????????08"],

        // Aligned input (Zeros) – no extra padding applied
        [PaddingMode.Zeros, "0001020304050607", 8, "0001020304050607"],

        // Aligned input (None)
        [PaddingMode.None, "0001020304050607", 8, "0001020304050607"],

        // Minimum block size of 1 (PKCS7)
        [PaddingMode.PKCS7, string.Empty, 1, "01"],

        // Non-power-of-two block size (7)
        [PaddingMode.PKCS7, "01", 7, "01060606060606"],

        // Empty input
        [PaddingMode.PKCS7, string.Empty, 8, "0808080808080808"]
    ];

    /// <summary>
    /// Provides invalid input configurations for padding logic that are expected to throw exceptions.
    /// </summary>
    public static IEnumerable<object[]> InvalidPaddingCases() =>
    [
        // PaddingMode, InputHex, BlockSize, ExpectedExceptionType, DestinationSize PaddingMode.None but input not aligned
        [PaddingMode.None, "01020304", 8, typeof(CryptographicException)],

        // Invalid padding mode (enum not defined)
        [(PaddingMode)999, "0102", 8, typeof(CryptographicException)],

        // Invalid block size (0)
        [PaddingMode.PKCS7, "010203", 0, typeof(ArgumentOutOfRangeException)],

        // Invalid block size (negative)
        [PaddingMode.PKCS7, "010203", -4 , typeof(ArgumentOutOfRangeException),8],

        // Destination span too small for expected padded length
        [PaddingMode.PKCS7, "010203", 8, typeof(ArgumentException), 4],

        // Destination too small even though input is aligned
        [PaddingMode.Zeros, "01020304", 8, typeof(ArgumentException), 4]
    ];

    /// <summary>
    /// Verifies that PadBlock applies the expected padding bytes and structure for valid input using the byte array overload. For
    /// ISO10126, only the last padding byte is validated due to randomness.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidPaddingCases))]
    public void PadBlock_WhenValidInput_ShouldApplyCorrectPadding_UsingByteArray(
        PaddingMode padding, string inputHex, int blockSizeBytes, string expectedHex)
    {
        byte[] input = Convert.FromHexString(inputHex);
        byte[] result = CryptographyHelper.PadBlock(padding, blockSizeBytes * 8, input, 0, input.Length);

        // If expectedHex is fully specified (not ISO10126 with '?'), match entire byte array Else validate last byte is pad count
        if (!expectedHex.Contains('?'))
        {
            byte[] expected = Convert.FromHexString(expectedHex);
            CollectionAssert.AreEqual(expected, result);
        }
        else
        {
            byte[] expected = Convert.FromHexString(expectedHex[^2..]);
            Assert.AreEqual(result[^1], expected[0]);
        }
    }

    /// <summary>
    /// Verifies that PadBlock applies the expected padding bytes and structure for valid input using the span-based overload. For
    /// ISO10126, only the last padding byte is validated due to randomness.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidPaddingCases))]
    public void PadBlock_WhenValidInput_ShouldApplyCorrectPadding_UsingSpan(
        PaddingMode padding, string inputHex, int blockSizeBytes, string expectedHex)
    {
        byte[] input = Convert.FromHexString(inputHex);
        int expectedLength = expectedHex.Length / 2;

        Span<byte> destination = expectedLength <= 128
            ? stackalloc byte[expectedLength]
            : new byte[expectedLength];

        _ = CryptographyHelper.PadBlock(padding, blockSizeBytes * 8, input, destination);

        // If expectedHex is fully specified (not ISO10126 with '?'), match entire byte array Else validate last byte is pad count
        if (!expectedHex.Contains('?'))
        {
            byte[] expected = Convert.FromHexString(expectedHex);
            Assert.IsTrue(expected.AsSpan().SequenceEqual(destination));
        }
        else
        {
            byte[] expected = Convert.FromHexString(expectedHex[^2..]);
            Assert.AreEqual(destination[^1], expected[0]);
        }
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock" /> returns the expected total output length after padding is applied when using the byte array overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidPaddingCases))]
    public void PadBlock_WhenValidInput_ShouldReturnExpectedLength_UsingByteArray(
        PaddingMode padding, string inputHex, int blockSizeBytes, string expectedHex)
    {
        byte[] input = Convert.FromHexString(inputHex);
        int expectedLength = expectedHex.Length / 2;
        byte[] result = CryptographyHelper.PadBlock(padding, blockSizeBytes * 8, input, 0, input.Length);

        Assert.HasCount(expectedLength, result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock" /> returns the expected total output length after padding is applied when using the span-based overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidPaddingCases))]
    public void PadBlock_WhenValidInput_ShouldReturnExpectedLength_UsingSpan(
        PaddingMode padding, string inputHex, int blockSizeBytes, string expectedHex)
    {
        byte[] input = Convert.FromHexString(inputHex);
        int expectedLength = expectedHex.Length / 2;

        Span<byte> destination = expectedLength <= 128
            ? stackalloc byte[expectedLength]
            : new byte[expectedLength];

        int result = CryptographyHelper.PadBlock(padding, blockSizeBytes * 8, input, destination);

        Assert.AreEqual(expectedLength, result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock" /> preserves the original bytes at the beginning of the padded block when using the byte array overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidPaddingCases))]
    public void PadBlock_WhenValidInput_ShouldPreserveOriginalBytes_UsingByteArray(
        PaddingMode padding, string inputHex, int blockSizeBytes, string expectedHex)
    {
        byte[] input = Convert.FromHexString(inputHex);
        byte[] result = CryptographyHelper.PadBlock(padding, blockSizeBytes * 8, input, 0, input.Length);

        CollectionAssert.AreEqual(input, result.Take(input.Length).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock" /> preserves the original bytes at the beginning of the padded block when using the span-based overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.ValidPaddingCases))]
    public void PadBlock_WhenValidInput_ShouldPreserveOriginalBytes_UsingSpan(
        PaddingMode padding, string inputHex, int blockSizeBytes, string expectedHex)
    {
        byte[] input = Convert.FromHexString(inputHex);
        int expectedLength = expectedHex.Length / 2;

        Span<byte> destination = expectedLength <= 128
            ? stackalloc byte[expectedLength]
            : new byte[expectedLength];

        _ = CryptographyHelper.PadBlock(padding, blockSizeBytes * 8, input, destination);

        Assert.IsTrue(destination.Slice(0, input.Length).SequenceEqual(input));
    }

    /// <summary>
    /// Verifies that PadBlock throws an exception for invalid input such as misaligned data, unsupported padding mode, or invalid block
    /// size when using the byte array overload. Skips tests that require a custom destination length.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.InvalidPaddingCases))]
    public void PadBlock_WhenInvalidInput_WithArray_ShouldThrowExactly(
        PaddingMode padding, string inputHex, int blockSizeBytes, Type exceptionType, int? destinationLength = null)
    {
        byte[] input = Convert.FromHexString(inputHex);

        if (destinationLength.HasValue) return; // cannot test for given destination buffers

        try
        {
            _ = CryptographyHelper.PadBlock(padding, blockSizeBytes * 8, input, 0, input.Length);
            Assert.Fail($"Expected {exceptionType.Name} was not thrown.");
        }
        catch (Exception ex)
        {
            Assert.IsInstanceOfType(ex, exceptionType);
        }
    }

    /// <summary>
    /// Verifies that PadBlock throws an exception for invalid input such as misaligned data, unsupported padding mode, or invalid block
    /// size when using the span-based overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CryptoHelpersTests.InvalidPaddingCases))]
    public void PadBlock_WhenInvalidInput_WithSpan_ShouldThrowExactly(
        PaddingMode padding, string inputHex, int blockSizeBytes, Type exceptionType, int? destinationLength = null)
    {
        byte[] input = Convert.FromHexString(inputHex);
        destinationLength ??= (blockSizeBytes * 2);

        Span<byte> destination = destinationLength.Value <= 128
            ? stackalloc byte[destinationLength.Value]
            : new byte[destinationLength.Value];

        try
        {
            _ = CryptographyHelper.PadBlock(padding, blockSizeBytes * 8, input, destination);
            Assert.Fail($"Expected {exceptionType.Name} was not thrown.");
        }
        catch (Exception ex)
        {
            Assert.IsInstanceOfType(ex, exceptionType);
        }
    }
}
