// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowInvalidPaddedSequence.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowInvalidPaddedSequence(string, string)"/> throws an
    /// <see cref="ArgumentException"/> whose message identifies the supplied padding scheme and whose
    /// <see cref="ArgumentException.ParamName"/> matches the supplied parameter name.
    /// </summary>
    [TestMethod]
    [DataRow("PKCS#7", "input")]
    [DataRow("ANSI X.923", "input")]
    [DataRow("ISO 10126", "input")]
    [DataRow("ISO/IEC 7816-4", "input")]
    public void ThrowInvalidPaddedSequence_WhenInvoked_ShouldThrowExactly(
        string scheme, string paramName)
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptoHelpers.ThrowInvalidPaddedSequence(scheme, paramName);
        });

        Assert.IsTrue(ex.Message.Contains(scheme, StringComparison.Ordinal));
        Assert.AreEqual(paramName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowInvalidPaddedSequence(string, string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the padding scheme is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowInvalidPaddedSequence_WhenSchemeIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowInvalidPaddedSequence(null!, "input");
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowInvalidPaddedSequence(string, string)"/> accepts a
    /// <see langword="null"/> parameter name; the produced <see cref="ArgumentException.ParamName"/> is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowInvalidPaddedSequence_WhenParamNameIsNull_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptoHelpers.ThrowInvalidPaddedSequence("PKCS#7", null!);
        });

        Assert.IsNull(ex.ParamName);
    }
}
