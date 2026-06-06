// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupOptionsTests.Nullable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateLookupOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupOptions" /> is a reference type after the sealed-class conversion,
    /// so that <see langword="default" /><c>(ExchangeRateLookupOptions)</c> evaluates to <see langword="null" />
    /// rather than a footgun struct whose <see cref="ExchangeRateLookupOptions.AllowInverse" /> and
    /// <see cref="ExchangeRateLookupOptions.AllowSameCurrencyIdentityRate" /> flags are silently <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Default_WhenEvaluated_ShouldBeNull()
    {
        ExchangeRateLookupOptions? options = default;

        Assert.IsNull(options);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.TryGetRate" /> accepts <see langword="null" /> options and
    /// substitutes the <see cref="ExchangeRateLookupOptions.Exact" /> default.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenOptionsIsNull_ForSeries_ShouldFallBackToExact()
    {
        ExchangeRatePair pair = new("USD", "AUD");
        ExchangeRateSeries series = new(
            pair,
            "RBA",
            new (DateOnly Date, decimal Rate)[]
            {
                (new DateOnly(2024, 1, 1), 1.5m),
            });

        var found = series.TryGetRate(new DateOnly(2024, 1, 1), options: null, out var resolvedDate, out var rate);

        Assert.IsTrue(found);
        Assert.AreEqual(new DateOnly(2024, 1, 1), resolvedDate);
        Assert.AreEqual(1.5m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.TryGetRate" /> returns <see langword="false" /> when null options
    /// (resolved to <see cref="ExchangeRateLookupOptions.Exact" />) cannot find a rate on the requested date.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenOptionsIsNullAndDateMissing_ForSeries_ShouldReturnFalse()
    {
        ExchangeRatePair pair = new("USD", "AUD");
        ExchangeRateSeries series = new(
            pair,
            "RBA",
            new (DateOnly Date, decimal Rate)[]
            {
                (new DateOnly(2024, 1, 1), 1.5m),
            });

        var found = series.TryGetRate(new DateOnly(2024, 1, 2), options: null, out _, out _);

        Assert.IsFalse(found);
    }
}
