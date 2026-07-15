// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheWarmupOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the defaults and validation contract of <see cref="NotableDateCacheWarmupOptions" />.
/// </summary>
[TestClass]
public sealed class NotableDateCacheWarmupOptionsTests
{
    /// <summary>
    /// Verifies the working defaults: an empty territory list and a rolling window of the current and next civil
    /// year.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaulted_ShouldCarryWorkingDefaults()
    {
        var options = new NotableDateCacheWarmupOptions();

        Assert.IsEmpty(options.Territories);
        Assert.AreEqual(0, options.YearsBehind);
        Assert.AreEqual(1, options.YearsAhead);
        Assert.IsNull(options.FirstYear);
        Assert.IsNull(options.LastYear);
    }

    /// <summary>
    /// Verifies that a well-formed configuration validates successfully.
    /// </summary>
    [TestMethod]
    public void Validate_WhenWellFormed_ShouldNotThrow()
    {
        var options = new NotableDateCacheWarmupOptions { YearsBehind = 1, YearsAhead = 2 };
        options.Territories.Add("US");
        options.Territories.Add("US-CA");

        options.Validate();

        Assert.IsTrue(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that an empty territory list is rejected, so an unconfigured warm-up cannot register silently.
    /// </summary>
    [TestMethod]
    public void Validate_WhenTerritoriesEmpty_ShouldThrowArgumentException()
    {
        var options = new NotableDateCacheWarmupOptions();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(NotableDateCacheWarmupOptions.Territories), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out string? error));
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a blank territory code is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenTerritoryBlank_ShouldThrowArgumentException()
    {
        var options = new NotableDateCacheWarmupOptions();
        options.Territories.Add("  ");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(NotableDateCacheWarmupOptions.Territories), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that a negative relative span is rejected with the offending parameter name.
    /// </summary>
    [TestMethod]
    public void Validate_WhenYearsBehindNegative_ShouldThrowArgumentException()
    {
        var options = new NotableDateCacheWarmupOptions { YearsBehind = -1 };
        options.Territories.Add("US");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(NotableDateCacheWarmupOptions.YearsBehind), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that a fixed year outside the representable calendar range is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenFixedYearOutOfRange_ShouldThrowArgumentException()
    {
        var options = new NotableDateCacheWarmupOptions { FirstYear = 0 };
        options.Territories.Add("US");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(NotableDateCacheWarmupOptions.FirstYear), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that fixed years set in inverted order are rejected with the last-year parameter name.
    /// </summary>
    [TestMethod]
    public void Validate_WhenFixedYearsInverted_ShouldThrowArgumentException()
    {
        var options = new NotableDateCacheWarmupOptions { FirstYear = 2027, LastYear = 2026 };
        options.Territories.Add("US");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(NotableDateCacheWarmupOptions.LastYear), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out _));
    }
}
