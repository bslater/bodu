// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowInvalidPadding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ThrowInvalidPadding(string)"/> throws a
    /// <see cref="CryptographicException"/> whose message identifies the supplied padding scheme.
    /// </summary>
    [TestMethod]
    [DataRow("PKCS#7")]
    [DataRow("ANSI X.923")]
    [DataRow("ISO 10126")]
    [DataRow("ISO/IEC 7816-4")]
    public void ThrowInvalidPadding_WhenInvoked_ShouldThrowExactly(string scheme)
    {
        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptographyHelper.ThrowInvalidPadding(scheme);
        });

        Assert.IsTrue(ex.Message.Contains(scheme, StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.ThrowInvalidPadding(string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the padding scheme is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowInvalidPadding_WhenSchemeIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptographyHelper.ThrowInvalidPadding(null!);
        });
    }
}
