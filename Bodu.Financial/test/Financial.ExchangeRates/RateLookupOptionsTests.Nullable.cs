// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateLookupOptionsTests.Nullable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class RateLookupOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="RateLookupOptions" /> is a reference type after the sealed-class conversion,
    /// so that <see langword="default" /><c>(RateLookupOptions)</c> evaluates to <see langword="null" />
    /// rather than a footgun struct whose <see cref="RateLookupOptions.AllowInverse" /> and
    /// <see cref="RateLookupOptions.AllowSameCurrencyIdentityRate" /> flags are silently <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Default_WhenEvaluated_ShouldBeNull()
    {
        RateLookupOptions? options = default;

        Assert.IsNull(options);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeries.TryGetRate" /> accepts <see langword="null" /> options and
    /// substitutes the <see cref="RateLookupOptions.Exact" /> default.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenOptionsIsNull_ForSeries_ShouldFallBackToExact()
    {
        CurrencyPair pair = new(CurrencyCode.USD, CurrencyCode.AUD);
        RateSeries series = new(
            pair,
            "RBA",
            [
                (new DateOnly(2024, 1, 1), 1.5m),
            ]);

        bool found = series.TryGetRate(new DateOnly(2024, 1, 1), options: null, out DateOnly resolvedDate, out decimal rate);

        Assert.IsTrue(found);
        Assert.AreEqual(new DateOnly(2024, 1, 1), resolvedDate);
        Assert.AreEqual(1.5m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeries.TryGetRate" /> returns <see langword="false" /> when null options
    /// (resolved to <see cref="RateLookupOptions.Exact" />) cannot find a rate on the requested date.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenOptionsIsNullAndDateMissing_ForSeries_ShouldReturnFalse()
    {
        CurrencyPair pair = new(CurrencyCode.USD, CurrencyCode.AUD);
        RateSeries series = new(
            pair,
            "RBA",
            [
                (new DateOnly(2024, 1, 1), 1.5m),
            ]);

        bool found = series.TryGetRate(new DateOnly(2024, 1, 2), options: null, out _, out _);

        Assert.IsFalse(found);
    }
}
