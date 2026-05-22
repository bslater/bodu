// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfCiphertextTooShort.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfCiphertextTooShort(ReadOnlySpan{byte}, int, string)"/>
    /// does not throw when the input length equals the required tag size.
    /// </summary>
    [TestMethod]
    public void ThrowIfCiphertextTooShort_WhenLengthEqualsTagSize_ShouldNotThrow()
    {
        ReadOnlySpan<byte> input = stackalloc byte[16];
        CryptoHelpers.ThrowIfCiphertextTooShort(input, 16);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfCiphertextTooShort(ReadOnlySpan{byte}, int, string)"/>
    /// does not throw when the input length exceeds the required tag size.
    /// </summary>
    [TestMethod]
    public void ThrowIfCiphertextTooShort_WhenLengthExceedsTagSize_ShouldNotThrow()
    {
        ReadOnlySpan<byte> input = stackalloc byte[32];
        CryptoHelpers.ThrowIfCiphertextTooShort(input, 16);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfCiphertextTooShort(ReadOnlySpan{byte}, int, string)"/>
    /// throws an <see cref="ArgumentException"/> when the input cannot accommodate the authentication tag.
    /// </summary>
    [TestMethod]
    public void ThrowIfCiphertextTooShort_WhenLengthLessThanTagSize_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpan<byte> input = stackalloc byte[4];
            CryptoHelpers.ThrowIfCiphertextTooShort(input, 16);
        });

        Assert.AreEqual("input", ex.ParamName);
        Assert.IsTrue(ex.Message.Contains("16", StringComparison.Ordinal));
    }
}
