// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowInvalidPadding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowInvalidPadding(string)"/> throws a
    /// <see cref="CryptographicException"/> whose message identifies the supplied padding scheme.
    /// </summary>
    [TestMethod]
    [DataRow("PKCS#7")]
    [DataRow("ANSI X.923")]
    [DataRow("ISO 10126")]
    [DataRow("ISO/IEC 7816-4")]
    public void ThrowInvalidPadding_WhenInvoked_ShouldThrowCryptographicExceptionWithScheme(string scheme)
    {
        var ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowInvalidPadding(scheme);
        });

        Assert.IsTrue(ex.Message.Contains(scheme, StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowInvalidPadding(string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the padding scheme is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowInvalidPadding_WhenSchemeIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowInvalidPadding(null!);
        });
    }
}
