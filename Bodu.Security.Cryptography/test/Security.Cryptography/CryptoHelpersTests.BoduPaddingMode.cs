// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.BoduPaddingMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock(PaddingModeKind, int, byte[], int, int)" /> with
    /// <see cref="PaddingModeKind.ISO7816_4" /> produces the expected one-and-zeros padded output.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenIso78164_ShouldAppendOneAndZeros_UsingByteArray()
    {
        byte[] input = Convert.FromHexString("102030");

        byte[] result = CryptographyHelper.PadBlock(PaddingModeKind.ISO7816_4, 64, input, 0, input.Length);

        Assert.HasCount(8, result);
        Assert.AreEqual(0x10, result[0]);
        Assert.AreEqual(0x20, result[1]);
        Assert.AreEqual(0x30, result[2]);
        Assert.AreEqual(0x80, result[3]);

        for (int i = 4; i < result.Length; i++)
            Assert.AreEqual(0x00, result[i]);
    }

    /// <summary>
    /// Provides all <see cref="PaddingModeKind" /> values that mirror a framework <see cref="PaddingMode" /> value.
    /// </summary>
    /// <returns>
    /// A sequence of test rows containing the Bodu padding mode and its corresponding framework padding mode.
    /// </returns>
    public static IEnumerable<object[]> MirroredPaddingModeData()
    {
        foreach (PaddingModeKind boduMode in Enum.GetValues<PaddingModeKind>())
        {
            if (boduMode == PaddingModeKind.ISO7816_4)
                continue;

            PaddingMode frameworkMode = Enum.TryParse(boduMode.ToString(), out PaddingMode parsedMode)
                ? parsedMode
                : (PaddingMode)(-1);

            yield return new object[] { boduMode, frameworkMode };
        }
    }

    /// <summary>
    /// Gets the display name for mirrored padding mode compatibility test rows.
    /// </summary>
    /// <param name="methodInfo">The test method being executed.</param>
    /// <param name="data">The dynamic data row containing the Bodu and framework padding modes.</param>
    /// <returns>A display name showing the mirrored padding mode mapping.</returns>
    public static string GetMirroredPaddingModeDisplayName(MethodInfo methodInfo, object[] data)
    {
        var boduMode = (PaddingModeKind)data[0];
        var frameworkMode = (PaddingMode)data[1];

        return $"{methodInfo.Name}({boduMode} -> {frameworkMode})";
    }

    /// <summary>
    /// Gets valid source bytes for comparing mirrored Bodu and framework padding modes.
    /// </summary>
    /// <param name="mode">The Bodu padding mode under test.</param>
    /// <returns>
    /// A block-aligned input for <see cref="PaddingModeKind.None" />; otherwise, a seven-byte input that produces
    /// deterministic one-byte padding for all mirrored padding modes.
    /// </returns>
    private static byte[] GetValidMirroredPaddingInput(PaddingModeKind mode) =>
        mode == PaddingModeKind.None
            ? Convert.FromHexString("1020304050607080")
            : Convert.FromHexString("10203040506070");

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock(PaddingModeKind, int, byte[], int, int)" /> with
    /// <see cref="PaddingModeKind.ISO7816_4" /> throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>blockSizeBytes</c> is not positive.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenIso78164AndBlockSizeIsZero_ShouldThrowExactly()
    {
        byte[] input = Convert.FromHexString("01020304");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CryptographyHelper.PadBlock(PaddingModeKind.ISO7816_4, 0, input, 0, input.Length);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock(PaddingModeKind, int, byte[], int, int)" /> produces
    /// the same output as the mirrored framework <see cref="PaddingMode" /> overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(MirroredPaddingModeData), DynamicDataDisplayName = nameof(GetMirroredPaddingModeDisplayName))]
    public void PadBlock_BoduPaddingMode_WhenMirroredMode_ShouldMatchFrameworkMode_UsingByteArray(
        PaddingModeKind boduMode,
        PaddingMode frameworkMode)
    {
        byte[] input = GetValidMirroredPaddingInput(boduMode);

        byte[] bodu = CryptographyHelper.PadBlock(boduMode, 64, input, 0, input.Length);
        byte[] framework = CryptographyHelper.PadBlock(frameworkMode, 64, input, 0, input.Length);

        CollectionAssert.AreEqual(framework, bodu);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// with <see cref="PaddingModeKind.ISO7816_4" /> writes the expected one-and-zeros pad bytes into the destination.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenIso78164_ShouldAppendOneAndZeros_UsingSpan()
    {
        byte[] input = Convert.FromHexString("102030");
        Span<byte> destination = stackalloc byte[8];

        int written = CryptographyHelper.PadBlock(PaddingModeKind.ISO7816_4, 64, input, destination);

        Assert.AreEqual(8, written);
        Assert.AreEqual(0x80, destination[3]);

        for (int i = 4; i < destination.Length; i++)
            Assert.AreEqual(0x00, destination[i]);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination span is too small to hold the padded output.
    /// </summary>
    [TestMethod]
    public void PadBlock_BoduPaddingMode_WhenIso78164AndDestinationTooSmall_ShouldThrowExactly()
    {
        byte[] input = Convert.FromHexString("102030");

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> destination = stackalloc byte[4];
            _ = CryptographyHelper.PadBlock(PaddingModeKind.ISO7816_4, 64, input, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.PadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// produces the same output as the mirrored framework <see cref="PaddingMode" /> overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(MirroredPaddingModeData), DynamicDataDisplayName = nameof(GetMirroredPaddingModeDisplayName))]
    public void PadBlock_BoduPaddingMode_WhenMirroredMode_ShouldMatchFrameworkMode_UsingSpan(
        PaddingModeKind boduMode,
        PaddingMode frameworkMode)
    {
        byte[] input = GetValidMirroredPaddingInput(boduMode);
        Span<byte> bodu = stackalloc byte[16];
        Span<byte> framework = stackalloc byte[16];

        int boduWritten = CryptographyHelper.PadBlock(boduMode, 64, input, bodu);
        int frameworkWritten = CryptographyHelper.PadBlock(frameworkMode, 64, input, framework);

        Assert.AreEqual(frameworkWritten, boduWritten);
        Assert.IsTrue(framework.Slice(0, frameworkWritten).SequenceEqual(bodu.Slice(0, boduWritten)));
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.DepadBlock(PaddingModeKind, int, byte[], int, int)" /> with
    /// <see cref="PaddingModeKind.ISO7816_4" /> removes the one-and-zeros padding and returns the original bytes.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164_ShouldRemovePadding_UsingByteArray()
    {
        byte[] padded = Convert.FromHexString("1020308000000000");

        byte[] result = CryptographyHelper.DepadBlock(PaddingModeKind.ISO7816_4, 64, padded, 0, padded.Length);

        CollectionAssert.AreEqual(Convert.FromHexString("102030"), result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.DepadBlock(PaddingModeKind, int, byte[], int, int)" /> with
    /// <see cref="PaddingModeKind.ISO7816_4" /> throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>blockSizeBytes</c> is not positive.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164AndBlockSizeIsZero_ShouldThrowExactly()
    {
        byte[] padded = Convert.FromHexString("1020308000000000");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CryptographyHelper.DepadBlock(PaddingModeKind.ISO7816_4, 0, padded, 0, padded.Length);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.DepadBlock(PaddingModeKind, int, byte[], int, int)" /> produces
    /// the same output as the mirrored framework <see cref="PaddingMode" /> overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(MirroredPaddingModeData), DynamicDataDisplayName = nameof(GetMirroredPaddingModeDisplayName))]
    public void DepadBlock_BoduPaddingMode_WhenMirroredMode_ShouldMatchFrameworkMode_UsingByteArray(
        PaddingModeKind boduMode,
        PaddingMode frameworkMode)
    {
        byte[] input = GetValidMirroredPaddingInput(boduMode);
        byte[] padded = CryptographyHelper.PadBlock(frameworkMode, 64, input, 0, input.Length);

        byte[] bodu = CryptographyHelper.DepadBlock(boduMode, 64, padded, 0, padded.Length);
        byte[] framework = CryptographyHelper.DepadBlock(frameworkMode, 64, padded, 0, padded.Length);

        CollectionAssert.AreEqual(framework, bodu);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.DepadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// with <see cref="PaddingModeKind.ISO7816_4" /> writes the unpadded bytes into the destination span.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164_ShouldRemovePadding_UsingSpan()
    {
        byte[] padded = Convert.FromHexString("1020308000000000");
        Span<byte> destination = stackalloc byte[8];

        int written = CryptographyHelper.DepadBlock(PaddingModeKind.ISO7816_4, 64, padded, destination);

        Assert.AreEqual(3, written);
        Assert.IsTrue(destination.Slice(0, written).SequenceEqual(Convert.FromHexString("102030")));
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.DepadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// throws <see cref="CryptographicException" /> when the source is not a positive multiple of the block size.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164AndSourceNotBlockAligned_ShouldThrowExactly()
    {
        byte[] padded = Convert.FromHexString("10203080");

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            Span<byte> destination = stackalloc byte[8];
            _ = CryptographyHelper.DepadBlock(PaddingModeKind.ISO7816_4, 64, padded, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.DepadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination span is too small to hold the depadded bytes.
    /// </summary>
    [TestMethod]
    public void DepadBlock_BoduPaddingMode_WhenIso78164AndDestinationTooSmall_ShouldThrowExactly()
    {
        byte[] padded = Convert.FromHexString("1020304050608000");

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> destination = stackalloc byte[2];
            _ = CryptographyHelper.DepadBlock(PaddingModeKind.ISO7816_4, 64, padded, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.DepadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte})" />
    /// produces the same output as the mirrored framework <see cref="PaddingMode" /> overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(MirroredPaddingModeData), DynamicDataDisplayName = nameof(GetMirroredPaddingModeDisplayName))]
    public void DepadBlock_BoduPaddingMode_WhenMirroredMode_ShouldMatchFrameworkMode_UsingSpan(
        PaddingModeKind boduMode,
        PaddingMode frameworkMode)
    {
        byte[] input = GetValidMirroredPaddingInput(boduMode);
        byte[] padded = CryptographyHelper.PadBlock(frameworkMode, 64, input, 0, input.Length);

        Span<byte> bodu = stackalloc byte[16];
        Span<byte> framework = stackalloc byte[16];

        int boduWritten = CryptographyHelper.DepadBlock(boduMode, 64, padded, bodu);
        int frameworkWritten = CryptographyHelper.DepadBlock(frameworkMode, 64, padded, framework);

        Assert.AreEqual(frameworkWritten, boduWritten);
        Assert.IsTrue(framework.Slice(0, frameworkWritten).SequenceEqual(bodu.Slice(0, boduWritten)));
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryPadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// with <see cref="PaddingModeKind.ISO7816_4" /> returns <see langword="true" /> for valid input and writes the
    /// expected number of bytes.
    /// </summary>
    [TestMethod]
    public void TryPadBlock_BoduPaddingMode_WhenIso78164_ShouldReturnTrue()
    {
        byte[] input = Convert.FromHexString("102030");
        Span<byte> destination = stackalloc byte[8];

        bool result = CryptographyHelper.TryPadBlock(PaddingModeKind.ISO7816_4, 64, input, destination, out int written);

        Assert.IsTrue(result);
        Assert.AreEqual(8, written);
        Assert.AreEqual(0x80, destination[3]);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryPadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// returns <see langword="false" /> and writes zero bytes when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryPadBlock_BoduPaddingMode_WhenIso78164AndDestinationTooSmall_ShouldReturnFalse()
    {
        byte[] input = Convert.FromHexString("102030");
        Span<byte> destination = stackalloc byte[4];

        bool result = CryptographyHelper.TryPadBlock(PaddingModeKind.ISO7816_4, 64, input, destination, out int written);

        Assert.IsFalse(result);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryPadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// produces the same result and output as the mirrored framework <see cref="PaddingMode" /> overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(MirroredPaddingModeData), DynamicDataDisplayName = nameof(GetMirroredPaddingModeDisplayName))]
    public void TryPadBlock_BoduPaddingMode_WhenMirroredMode_ShouldMatchFrameworkMode(
        PaddingModeKind boduMode,
        PaddingMode frameworkMode)
    {
        byte[] input = GetValidMirroredPaddingInput(boduMode);
        Span<byte> bodu = stackalloc byte[16];
        Span<byte> framework = stackalloc byte[16];

        bool boduResult = CryptographyHelper.TryPadBlock(boduMode, 64, input, bodu, out int boduWritten);
        bool frameworkResult = CryptographyHelper.TryPadBlock(frameworkMode, 64, input, framework, out int frameworkWritten);

        Assert.AreEqual(frameworkResult, boduResult);
        Assert.AreEqual(frameworkWritten, boduWritten);
        Assert.IsTrue(framework.Slice(0, frameworkWritten).SequenceEqual(bodu.Slice(0, boduWritten)));
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryDepadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// with <see cref="PaddingModeKind.ISO7816_4" /> returns <see langword="true" /> and the original byte count.
    /// </summary>
    [TestMethod]
    public void TryDepadBlock_BoduPaddingMode_WhenIso78164_ShouldReturnTrue()
    {
        byte[] padded = Convert.FromHexString("1020308000000000");
        Span<byte> destination = stackalloc byte[8];

        bool result = CryptographyHelper.TryDepadBlock(PaddingModeKind.ISO7816_4, 64, padded, destination, out int written);

        Assert.IsTrue(result);
        Assert.AreEqual(3, written);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryDepadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// returns <see langword="false" /> and writes zero when the source span is not block-aligned.
    /// </summary>
    [TestMethod]
    public void TryDepadBlock_BoduPaddingMode_WhenIso78164AndSourceNotBlockAligned_ShouldReturnFalse()
    {
        byte[] padded = Convert.FromHexString("10203080");
        Span<byte> destination = stackalloc byte[8];

        bool result = CryptographyHelper.TryDepadBlock(PaddingModeKind.ISO7816_4, 64, padded, destination, out int written);

        Assert.IsFalse(result);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.TryDepadBlock(PaddingModeKind, int, System.ReadOnlySpan{byte}, System.Span{byte}, out int)" />
    /// produces the same result and output as the mirrored framework <see cref="PaddingMode" /> overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(MirroredPaddingModeData), DynamicDataDisplayName = nameof(GetMirroredPaddingModeDisplayName))]
    public void TryDepadBlock_BoduPaddingMode_WhenMirroredMode_ShouldMatchFrameworkMode(
        PaddingModeKind boduMode,
        PaddingMode frameworkMode)
    {
        byte[] input = GetValidMirroredPaddingInput(boduMode);
        byte[] padded = CryptographyHelper.PadBlock(frameworkMode, 64, input, 0, input.Length);

        Span<byte> bodu = stackalloc byte[16];
        Span<byte> framework = stackalloc byte[16];

        bool boduResult = CryptographyHelper.TryDepadBlock(boduMode, 64, padded, bodu, out int boduWritten);
        bool frameworkResult = CryptographyHelper.TryDepadBlock(frameworkMode, 64, padded, framework, out int frameworkWritten);

        Assert.AreEqual(frameworkResult, boduResult);
        Assert.AreEqual(frameworkWritten, boduWritten);
        Assert.IsTrue(framework.Slice(0, frameworkWritten).SequenceEqual(bodu.Slice(0, boduWritten)));
    }
}
