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
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidIVForMode(byte[], CipherBlockMode, int, KeySizes[], string)"/>
    /// does not throw when the IV length equals the block size and the mode requires an IV.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidIVForMode_WhenIVIsValid_ShouldNotThrow()
    {
        var iv = new byte[16];
        CryptoHelpers.ThrowIfInvalidIVForMode(iv, CipherBlockMode.CBC, 16, LegalBlockSizes);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidIVForMode(byte[], CipherBlockMode, int, KeySizes[], string)"/>
    /// does not throw for <see cref="CipherBlockMode.ECB"/> even when no IV is provided.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidIVForMode_WhenIVNullAndModeIsECB_ShouldNotThrow()
    {
        CryptoHelpers.ThrowIfInvalidIVForMode(null, CipherBlockMode.ECB, 16, LegalBlockSizes);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidIVForMode(byte[], CipherBlockMode, int, KeySizes[], string)"/>
    /// throws a <see cref="CryptographicException"/> when the IV is <see langword="null"/> and the mode requires one.
    /// </summary>
    [TestMethod]
    [DataRow(CipherBlockMode.CBC)]
    [DataRow(CipherBlockMode.CFB)]
    [DataRow(CipherBlockMode.OFB)]
    [DataRow(CipherBlockMode.CTR)]
    public void ThrowIfInvalidIVForMode_WhenIVNullAndModeRequiresIV_ShouldThrowCryptographicException(CipherBlockMode mode)
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidIVForMode(null, mode, 16, LegalBlockSizes);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidIVForMode(byte[], CipherBlockMode, int, KeySizes[], string)"/>
    /// throws a <see cref="CryptographicException"/> when the IV length does not match the block size.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidIVForMode_WhenIVHasWrongLength_ShouldThrowCryptographicException()
    {
        var iv = new byte[8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidIVForMode(iv, CipherBlockMode.CBC, 16, LegalBlockSizes);
        });
    }
}
