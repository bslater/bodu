// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheWarmupOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the defaults and validation contract of <see cref="RateCacheWarmupOptions" />.
/// </summary>
[TestClass]
public sealed class RateCacheWarmupOptionsTests
{
    /// <summary>
    /// Verifies the working defaults: an empty pair list, a 30-day rolling look-back, and no fixed bounds.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaulted_ShouldCarryWorkingDefaults()
    {
        var options = new RateCacheWarmupOptions();

        Assert.IsEmpty(options.Pairs);
        Assert.AreEqual(30, options.LookbackDays);
        Assert.IsNull(options.StartDate);
        Assert.IsNull(options.EndDate);
        Assert.IsEmpty(options.Providers);
    }

    /// <summary>
    /// Verifies that a well-formed configuration validates successfully.
    /// </summary>
    [TestMethod]
    public void Validate_WhenWellFormed_ShouldNotThrow()
    {
        var options = new RateCacheWarmupOptions { LookbackDays = 0 };
        options.Pairs.Add("AUD/USD");
        options.Pairs.Add("EUR/GBP");

        options.Validate();

        Assert.IsTrue(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that an empty pair list is rejected, so an unconfigured warm-up cannot register silently.
    /// </summary>
    [TestMethod]
    public void Validate_WhenPairsEmpty_ShouldThrowArgumentException()
    {
        var options = new RateCacheWarmupOptions();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(RateCacheWarmupOptions.Pairs), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out string? error));
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a pair not in the <c>XXX/YYY</c> shape is rejected with the pair list's parameter name.
    /// </summary>
    /// <param name="pair">The malformed pair.</param>
    [TestMethod]
    [DataRow("AUDUSD")]
    [DataRow("AUD/US")]
    [DataRow("AUD-USD")]
    [DataRow("AUD/USDX")]
    [DataRow("A1D/USD")]
    [DataRow("")]
    public void Validate_WhenPairMalformed_ShouldThrowArgumentException(string pair)
    {
        var options = new RateCacheWarmupOptions();
        options.Pairs.Add(pair);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(RateCacheWarmupOptions.Pairs), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that a negative look-back is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenLookbackNegative_ShouldThrowArgumentException()
    {
        var options = new RateCacheWarmupOptions { LookbackDays = -1 };
        options.Pairs.Add("AUD/USD");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(RateCacheWarmupOptions.LookbackDays), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that fixed bounds set in inverted order are rejected with the end-date parameter name.
    /// </summary>
    [TestMethod]
    public void Validate_WhenFixedWindowInverted_ShouldThrowArgumentException()
    {
        var options = new RateCacheWarmupOptions
        {
            StartDate = new DateOnly(2023, 1, 4),
            EndDate = new DateOnly(2023, 1, 3),
        };
        options.Pairs.Add("AUD/USD");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(RateCacheWarmupOptions.EndDate), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that a single fixed bound validates on its own, because each bound replaces its rolling default
    /// independently.
    /// </summary>
    [TestMethod]
    public void Validate_WhenOnlyOneFixedBoundSet_ShouldNotThrow()
    {
        var options = new RateCacheWarmupOptions { StartDate = new DateOnly(2023, 1, 3) };
        options.Pairs.Add("AUD/USD");

        options.Validate();

        Assert.IsTrue(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that pair parsing splits and upper-cases a well-formed pair and rejects other shapes.
    /// </summary>
    [TestMethod]
    public void TryParsePair_WhenWellFormed_ShouldSplitAndUpperCase()
    {
        Assert.IsTrue(RateCacheWarmupOptions.TryParsePair("aud/usd", out (string From, string To) pair));
        Assert.AreEqual("AUD", pair.From);
        Assert.AreEqual("USD", pair.To);

        Assert.IsFalse(RateCacheWarmupOptions.TryParsePair(null, out _));
        Assert.IsFalse(RateCacheWarmupOptions.TryParsePair("AUD USD", out _));
    }
}
