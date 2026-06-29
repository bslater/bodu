// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheFileLayoutTests.Sanitize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class ExchangeRateCacheFileLayoutTests
{
    /// <summary>
    /// Verifies that a segment with no illegal characters is returned unchanged.
    /// </summary>
    [TestMethod]
    public void Sanitize_WhenSegmentIsClean_ShouldReturnUnchanged() =>
        Assert.AreEqual("AUDUSD", ExchangeRateCacheFileLayout.Sanitize("AUDUSD"));

    /// <summary>
    /// Verifies that each path-illegal character in a segment is replaced with an underscore.
    /// </summary>
    [TestMethod]
    public void Sanitize_WhenSegmentHasIllegalCharacters_ShouldReplaceWithUnderscore() =>
        Assert.AreEqual("a_b", ExchangeRateCacheFileLayout.Sanitize("a" + new string(Path.GetInvalidFileNameChars()[0], 1) + "b"));

    /// <summary>
    /// Verifies that sanitizing a <see langword="null" /> segment throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Sanitize_WhenSegmentIsNull_ShouldThrowArgumentNullException() =>
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ExchangeRateCacheFileLayout.Sanitize(null!);
        });
}
