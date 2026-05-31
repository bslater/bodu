// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowHelpers.cs" company="Bodu Pty. Ltd.". >
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfAssociatedDataNotProcessed(bool)" /> does not throw when the
    /// associated-data flag is <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfAssociatedDataNotProcessed_WhenAlreadyProcessed_ShouldNotThrow()
    {
        CryptoHelpers.ThrowIfAssociatedDataNotProcessed(true);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)" /> does not throw when the
    /// value is a positive multiple of the divisor.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsPositiveMultiple_ShouldNotThrow()
    {
        CryptoHelpers.ThrowIfNotPositiveMultipleOf(16, 8);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)" /> throws
    /// <see cref="CryptographicException" /> when the value is zero.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsZero_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfNotPositiveMultipleOf(0, 8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)" /> throws
    /// <see cref="CryptographicException" /> when the value is negative.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfNotPositiveMultipleOf(-8, 8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)" /> throws
    /// <see cref="CryptographicException" /> when the value is positive but not a multiple of the divisor.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsNotMultiple_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfNotPositiveMultipleOf(10, 8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the divisor is zero.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPositiveMultipleOf_WhenDivisorIsZero_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CryptoHelpers.ThrowIfNotPositiveMultipleOf(16, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the divisor is negative.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotPositiveMultipleOf_WhenDivisorIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CryptoHelpers.ThrowIfNotPositiveMultipleOf(16, -8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)" /> does not
    /// throw when the value length matches the expected block size.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenLengthMatches_ShouldNotThrow()
    {
        var value = new byte[8];
        KeySizes[] legal = new[] { new KeySizes(64, 64, 0) };

        CryptoHelpers.ThrowIfInvalidBlockSize(value, 64, legal);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)" /> throws
    /// <see cref="ArgumentNullException" /> when the value array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenValueIsNull_ShouldThrowExactly()
    {
        KeySizes[] legal = new[] { new KeySizes(64, 64, 0) };

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidBlockSize(null!, 64, legal);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)" /> throws
    /// <see cref="ArgumentNullException" /> when the legal sizes array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenLegalSizesIsNull_ShouldThrowExactly()
    {
        var value = new byte[8];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidBlockSize(value, 64, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)" /> throws
    /// <see cref="CryptographicException" /> when the value length does not equal the expected block size.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenLengthDoesNotMatch_ShouldThrowExactly()
    {
        var value = new byte[6];
        KeySizes[] legal = new[] { new KeySizes(64, 64, 0) };

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidBlockSize(value, 64, legal);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FormatLegalSizes(KeySizes[])" /> returns the empty string when the
    /// array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenNull_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, CryptoHelpers.FormatLegalSizes(null));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FormatLegalSizes(KeySizes[])" /> returns the empty string when the
    /// array is empty.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenEmpty_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, CryptoHelpers.FormatLegalSizes(Array.Empty<KeySizes>()));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FormatLegalSizes(KeySizes[])" /> emits a single, distinct value when
    /// <see cref="KeySizes.SkipSize" /> is zero.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenSkipSizeIsZero_ShouldFormatMinSizeOnly()
    {
        KeySizes[] keySizes = new[] { new KeySizes(128, 256, 0) };

        Assert.AreEqual("128", CryptoHelpers.FormatLegalSizes(keySizes));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FormatLegalSizes(KeySizes[])" /> enumerates each permitted size when a
    /// non-zero <see cref="KeySizes.SkipSize" /> is supplied.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenSkipSizeIsPositive_ShouldFormatAllAllowedSizes()
    {
        KeySizes[] keySizes = new[] { new KeySizes(128, 256, 64) };

        Assert.AreEqual("128, 192, 256", CryptoHelpers.FormatLegalSizes(keySizes));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FormatLegalSizes(KeySizes[])" /> deduplicates and sorts the values
    /// emitted by multiple overlapping <see cref="KeySizes" /> entries.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenMultipleOverlappingEntries_ShouldReturnDistinctOrderedValues()
    {
        KeySizes[] keySizes = new[]
        {
            new KeySizes(256, 256, 0),
            new KeySizes(128, 192, 64),
        };

        Assert.AreEqual("128, 192, 256", CryptoHelpers.FormatLegalSizes(keySizes));
    }
}
