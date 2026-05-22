// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfInvalidIVForMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    private static readonly KeySizes[] LegalBlockSizes = new[] { new KeySizes(128, 128, 0) };

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidIVForMode(byte[], CipherModeKind, int, KeySizes[], string)"/>
    /// does not throw when the IV byte length matches the block size in bits and the mode requires an IV.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidIVForMode_WhenIVIsValid_ShouldNotThrow()
    {
        var iv = new byte[16];
        CryptoHelpers.ThrowIfInvalidIVForMode(iv, CipherModeKind.CBC, 128, LegalBlockSizes);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidIVForMode(byte[], CipherModeKind, int, KeySizes[], string)"/>
    /// does not throw for <see cref="CipherModeKind.ECB"/> even when no IV is provided.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidIVForMode_WhenIVNullAndModeIsECB_ShouldNotThrow()
    {
        CryptoHelpers.ThrowIfInvalidIVForMode(null, CipherModeKind.ECB, 128, LegalBlockSizes);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidIVForMode(byte[], CipherModeKind, int, KeySizes[], string)"/>
    /// throws a <see cref="CryptographicException"/> when the IV is <see langword="null"/> and the mode requires one.
    /// </summary>
    [TestMethod]
    [DataRow(CipherModeKind.CBC)]
    [DataRow(CipherModeKind.CFB)]
    [DataRow(CipherModeKind.OFB)]
    [DataRow(CipherModeKind.CTR)]
    public void ThrowIfInvalidIVForMode_WhenIVNullAndModeRequiresIV_ShouldThrowExactly(CipherModeKind mode)
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidIVForMode(null, mode, 128, LegalBlockSizes);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidIVForMode(byte[], CipherModeKind, int, KeySizes[], string)"/>
    /// throws a <see cref="CryptographicException"/> when the IV byte length does not match the block size in bits.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidIVForMode_WhenIVHasWrongLength_ShouldThrowExactly()
    {
        var iv = new byte[8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidIVForMode(iv, CipherModeKind.CBC, 128, LegalBlockSizes);
        });
    }
}
