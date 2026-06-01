// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.FormatLegalSizes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.FormatLegalSizes(KeySizes[])"/> returns an empty string when
    /// the supplied array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenArrayIsNull_ShouldReturnEmptyString()
    {
        var result = CryptographyHelper.FormatLegalSizes(null);

        Assert.AreEqual(string.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.FormatLegalSizes(KeySizes[])"/> returns an empty string when
    /// the supplied array is empty.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenArrayIsEmpty_ShouldReturnEmptyString()
    {
        var result = CryptographyHelper.FormatLegalSizes([]);

        Assert.AreEqual(string.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.FormatLegalSizes(KeySizes[])"/> returns a single size value when
    /// the <see cref="KeySizes.SkipSize"/> is zero.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenSkipSizeIsZero_ShouldReturnSingleSize()
    {
        KeySizes[] sizes = new[] { new KeySizes(128, 128, 0) };

        var result = CryptographyHelper.FormatLegalSizes(sizes);

        Assert.AreEqual("128", result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.FormatLegalSizes(KeySizes[])"/> enumerates each value in the range
    /// using the supplied skip step.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenRangeWithSkip_ShouldReturnAllSteps()
    {
        KeySizes[] sizes = new[] { new KeySizes(128, 256, 64) };

        var result = CryptographyHelper.FormatLegalSizes(sizes);

        Assert.AreEqual("128, 192, 256", result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.FormatLegalSizes(KeySizes[])"/> merges values from multiple ranges,
    /// removes duplicates, and orders them in ascending order.
    /// </summary>
    [TestMethod]
    public void FormatLegalSizes_WhenMultipleRanges_ShouldDeduplicateAndOrderAscending()
    {
        KeySizes[] sizes = new[]
        {
            new KeySizes(256, 256, 0),
            new KeySizes(128, 192, 64)
        };

        var result = CryptographyHelper.FormatLegalSizes(sizes);

        Assert.AreEqual("128, 192, 256", result);
    }
}
