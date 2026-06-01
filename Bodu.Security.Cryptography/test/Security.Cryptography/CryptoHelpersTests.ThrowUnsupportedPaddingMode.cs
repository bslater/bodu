// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowUnsupportedPaddingMode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ThrowUnsupportedPaddingMode{T}(T)"/> throws a
    /// <see cref="CryptographicException"/> for an undefined <see cref="PaddingMode"/> value.
    /// </summary>
    [TestMethod]
    public void ThrowUnsupportedPaddingMode_WhenPaddingModeIsUndefined_ShouldThrowExactly()
    {
        var undefined = (PaddingMode)999;

        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptographyHelper.ThrowUnsupportedPaddingMode(undefined);
        });

        Assert.IsTrue(ex.Message.Contains("999", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ThrowUnsupportedPaddingMode{T}(T)"/> throws a
    /// <see cref="CryptographicException"/> for an undefined <see cref="PaddingModeKind"/> value.
    /// </summary>
    [TestMethod]
    public void ThrowUnsupportedPaddingMode_WhenBlockPaddingModeIsUndefined_ShouldThrowExactly()
    {
        var undefined = (PaddingModeKind)999;

        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptographyHelper.ThrowUnsupportedPaddingMode(undefined);
        });

        Assert.IsTrue(ex.Message.Contains("999", StringComparison.Ordinal));
    }
}
