// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.BoduPaddingMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.PadBlock(BoduPaddingMode, int, byte[], int, int)" /> with
    /// <see cref="BoduPaddingMode.ISO7816_4" /> produces the expected one-and-zeros padded output.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenIso78164_ShouldAppendOneAndZeros_UsingByteArray()
    {
        var input = Convert.FromHexString("102030");

        var result = CryptoHelpers.PadBlock(BoduPaddingMode.ISO7816_4, 8, input, 0, input.Length);

        Assert.AreEqual(8, result.Length);
        Assert.AreEqual(0x10, result[0]);
        Assert.AreEqual(0x20, result[1]);
        Assert.AreEqual(0x30, result[2]);
        Assert.AreEqual(0x80, result[3]);
        for (var i = 4; i < result.Length; i++)
            Assert.AreEqual(0x00, result[i]);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.PadBlock(BoduPaddingMode, int, byte[], int, int)" /> with
    /// <see cref="BoduPaddingMode.ISO7816_4" /> throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>blockSizeBytes</c> is not positive.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenIso78164AndBlockSizeIsZero_ShouldThrowExactly_UsingByteArray()
    {
        var input = Convert.FromHexString("01020304");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CryptoHelpers.PadBlock(BoduPaddingMode.ISO7816_4, 0, input, 0, input.Length);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.PadBlock(BoduPaddingMode, int, byte[], int, int)" /> falls through
    /// to the framework <see cref="PaddingMode" /> implementation when the mode is not <see cref="BoduPaddingMode.ISO7816_4" />.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenPkcs7_ShouldDelegateToFrameworkMode_UsingByteArray()
    {
        var input = Convert.FromHexString("102030");

        var bodu = CryptoHelpers.PadBlock(BoduPaddingMode.PKCS7, 8, input, 0, input.Length);
        var framework = CryptoHelpers.PadBlock(PaddingMode.PKCS7, 8, input, 0, input.Length);

        CollectionAssert.AreEqual(framework, bodu);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.PadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// with <see cref="BoduPaddingMode.ISO7816_4" /> writes the expected one-and-zeros pad bytes into the destination.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenIso78164_ShouldAppendOneAndZeros_UsingSpan()
    {
        var input = Convert.FromHexString("102030");
        Span<byte> destination = stackalloc byte[8];

        var written = CryptoHelpers.PadBlock(BoduPaddingMode.ISO7816_4, 8, input, destination);

        Assert.AreEqual(8, written);
        Assert.AreEqual(0x80, destination[3]);
        for (var i = 4; i < destination.Length; i++)
            Assert.AreEqual(0x00, destination[i]);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.PadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination span is too small to hold the padded output.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenIso78164AndDestinationTooSmall_ShouldThrowExactly_UsingSpan()
    {
        var input = Convert.FromHexString("102030");

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> destination = stackalloc byte[4];
            _ = CryptoHelpers.PadBlock(BoduPaddingMode.ISO7816_4, 8, input, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.PadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// falls through to the framework <see cref="PaddingMode" /> implementation when the mode is not
    /// <see cref="BoduPaddingMode.ISO7816_4" />.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenPkcs7_ShouldDelegateToFrameworkMode_UsingSpan()
    {
        var input = Convert.FromHexString("102030");
        Span<byte> bodu = stackalloc byte[8];
        Span<byte> framework = stackalloc byte[8];

        var boduWritten = CryptoHelpers.PadBlock(BoduPaddingMode.PKCS7, 8, input, bodu);
        var frameworkWritten = CryptoHelpers.PadBlock(PaddingMode.PKCS7, 8, input, framework);

        Assert.AreEqual(frameworkWritten, boduWritten);
        Assert.IsTrue(framework.SequenceEqual(bodu));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.DepadBlock(BoduPaddingMode, int, byte[], int, int)" /> with
    /// <see cref="BoduPaddingMode.ISO7816_4" /> removes the one-and-zeros padding and returns the original bytes.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164_ShouldRemovePadding_UsingByteArray()
    {
        var padded = Convert.FromHexString("1020308000000000");

        var result = CryptoHelpers.DepadBlock(BoduPaddingMode.ISO7816_4, 8, padded, 0, padded.Length);

        CollectionAssert.AreEqual(Convert.FromHexString("102030"), result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.DepadBlock(BoduPaddingMode, int, byte[], int, int)" /> with
    /// <see cref="BoduPaddingMode.ISO7816_4" /> throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>blockSizeBytes</c> is not positive.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164AndBlockSizeIsZero_ShouldThrowExactly_UsingByteArray()
    {
        var padded = Convert.FromHexString("1020308000000000");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CryptoHelpers.DepadBlock(BoduPaddingMode.ISO7816_4, 0, padded, 0, padded.Length);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.DepadBlock(BoduPaddingMode, int, byte[], int, int)" /> delegates to
    /// the framework <see cref="PaddingMode" /> implementation when the mode is not <see cref="BoduPaddingMode.ISO7816_4" />.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenPkcs7_ShouldDelegateToFrameworkMode_UsingByteArray()
    {
        var padded = Convert.FromHexString("1020300505050505");

        var bodu = CryptoHelpers.DepadBlock(BoduPaddingMode.PKCS7, 8, padded, 0, padded.Length);
        var framework = CryptoHelpers.DepadBlock(PaddingMode.PKCS7, 8, padded, 0, padded.Length);

        CollectionAssert.AreEqual(framework, bodu);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.DepadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// with <see cref="BoduPaddingMode.ISO7816_4" /> writes the unpadded bytes into the destination span.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164_ShouldRemovePadding_UsingSpan()
    {
        var padded = Convert.FromHexString("1020308000000000");
        Span<byte> destination = stackalloc byte[8];

        var written = CryptoHelpers.DepadBlock(BoduPaddingMode.ISO7816_4, 8, padded, destination);

        Assert.AreEqual(3, written);
        Assert.IsTrue(destination.Slice(0, written).SequenceEqual(Convert.FromHexString("102030")));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.DepadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// throws <see cref="ArgumentOutOfRangeException" /> when the block size is not positive.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164AndBlockSizeIsZero_ShouldThrowExactly_UsingSpan()
    {
        var padded = Convert.FromHexString("1020308000000000");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Span<byte> destination = stackalloc byte[8];
            _ = CryptoHelpers.DepadBlock(BoduPaddingMode.ISO7816_4, 0, padded, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.DepadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// throws <see cref="CryptographicException" /> when the source is not a positive multiple of the block size.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164AndSourceNotBlockAligned_ShouldThrowExactly_UsingSpan()
    {
        var padded = Convert.FromHexString("10203080");

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            Span<byte> destination = stackalloc byte[8];
            _ = CryptoHelpers.DepadBlock(BoduPaddingMode.ISO7816_4, 8, padded, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.DepadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination span is too small to hold the depadded bytes.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164AndDestinationTooSmall_ShouldThrowExactly_UsingSpan()
    {
        var padded = Convert.FromHexString("1020304050608000");

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> destination = stackalloc byte[2];
            _ = CryptoHelpers.DepadBlock(BoduPaddingMode.ISO7816_4, 8, padded, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.DepadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// falls through to the framework <see cref="PaddingMode" /> implementation when the mode is not
    /// <see cref="BoduPaddingMode.ISO7816_4" />.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenPkcs7_ShouldDelegateToFrameworkMode_UsingSpan()
    {
        var padded = Convert.FromHexString("1020300505050505");
        Span<byte> bodu = stackalloc byte[8];
        Span<byte> framework = stackalloc byte[8];

        var boduWritten = CryptoHelpers.DepadBlock(BoduPaddingMode.PKCS7, 8, padded, bodu);
        var frameworkWritten = CryptoHelpers.DepadBlock(PaddingMode.PKCS7, 8, padded, framework);

        Assert.AreEqual(frameworkWritten, boduWritten);
        Assert.IsTrue(framework.Slice(0, frameworkWritten).SequenceEqual(bodu.Slice(0, boduWritten)));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryPadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// with <see cref="BoduPaddingMode.ISO7816_4" /> returns <see langword="true" /> for valid input and writes the
    /// expected number of bytes.
    /// </summary>
    [TestMethod]
    public void TryPadBlock_BoduPaddingMode_WhenIso78164_ShouldReturnTrue()
    {
        var input = Convert.FromHexString("102030");
        Span<byte> destination = stackalloc byte[8];

        var result = CryptoHelpers.TryPadBlock(BoduPaddingMode.ISO7816_4, 8, input, destination, out var written);

        Assert.IsTrue(result);
        Assert.AreEqual(8, written);
        Assert.AreEqual(0x80, destination[3]);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryPadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// returns <see langword="false" /> and writes zero bytes when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryPadBlock_BoduPaddingMode_WhenIso78164AndDestinationTooSmall_ShouldReturnFalse()
    {
        var input = Convert.FromHexString("102030");
        Span<byte> destination = stackalloc byte[4];

        var result = CryptoHelpers.TryPadBlock(BoduPaddingMode.ISO7816_4, 8, input, destination, out var written);

        Assert.IsFalse(result);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryPadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// delegates to the framework path and succeeds for non-ISO modes.
    /// </summary>
    [TestMethod]
    public void TryPadBlock_BoduPaddingMode_WhenPkcs7_ShouldReturnTrue()
    {
        var input = Convert.FromHexString("102030");
        Span<byte> destination = stackalloc byte[8];

        var result = CryptoHelpers.TryPadBlock(BoduPaddingMode.PKCS7, 8, input, destination, out var written);

        Assert.IsTrue(result);
        Assert.AreEqual(8, written);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryDepadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// with <see cref="BoduPaddingMode.ISO7816_4" /> returns <see langword="true" /> and the original byte count.
    /// </summary>
    [TestMethod]
    public void TryDepadBlock_BoduPaddingMode_WhenIso78164_ShouldReturnTrue()
    {
        var padded = Convert.FromHexString("1020308000000000");
        Span<byte> destination = stackalloc byte[8];

        var result = CryptoHelpers.TryDepadBlock(BoduPaddingMode.ISO7816_4, 8, padded, destination, out var written);

        Assert.IsTrue(result);
        Assert.AreEqual(3, written);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryDepadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// returns <see langword="false" /> and writes zero when the source span is not block-aligned.
    /// </summary>
    [TestMethod]
    public void TryDepadBlock_BoduPaddingMode_WhenIso78164AndSourceNotBlockAligned_ShouldReturnFalse()
    {
        var padded = Convert.FromHexString("10203080");
        Span<byte> destination = stackalloc byte[8];

        var result = CryptoHelpers.TryDepadBlock(BoduPaddingMode.ISO7816_4, 8, padded, destination, out var written);

        Assert.IsFalse(result);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryDepadBlock(BoduPaddingMode, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// delegates to the framework path for non-ISO modes and reports success.
    /// </summary>
    [TestMethod]
    public void TryDepadBlock_BoduPaddingMode_WhenPkcs7_ShouldReturnTrue()
    {
        var padded = Convert.FromHexString("1020300505050505");
        Span<byte> destination = stackalloc byte[8];

        var result = CryptoHelpers.TryDepadBlock(BoduPaddingMode.PKCS7, 8, padded, destination, out var written);

        Assert.IsTrue(result);
        Assert.AreEqual(3, written);
    }
}
