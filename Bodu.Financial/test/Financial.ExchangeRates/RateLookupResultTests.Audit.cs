// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateLookupResultTests.Audit.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class RateLookupResultTests
{
    /// <summary>
    /// Verifies that <c>ResolvedDate</c> simply surfaces the inner rate's observation date.
    /// </summary>
    [TestMethod]
    public void ResolvedDate_WhenAccessed_ShouldEqualRateDate()
    {
        DateOnly resolved = new(2024, 1, 3);
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, resolved, 1.5m, "RBA");
        RateLookupResult result = new(rate, new DateOnly(2024, 1, 5), RateDateResolution.PreviousOnOrBefore, 2, RateProvenance.Live(rate.Provider));

        Assert.AreEqual(resolved, result.ResolvedDate);
    }

    /// <summary>
    /// Verifies that <c>SignedOffsetDays</c> is negative when the resolved observation is older than the requested
    /// date (typical PreviousOnOrBefore fallback).
    /// </summary>
    [TestMethod]
    public void SignedOffsetDays_WhenResolvedBeforeRequested_ShouldBeNegative()
    {
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.5m, "RBA");
        RateLookupResult result = new(rate, new DateOnly(2024, 1, 5), RateDateResolution.PreviousOnOrBefore, 2, RateProvenance.Live(rate.Provider));

        Assert.AreEqual(-2, result.SignedOffsetDays);
        Assert.IsTrue(result.IsPreviousDate);
        Assert.IsFalse(result.IsFutureDate);
    }

    /// <summary>
    /// Verifies that <c>SignedOffsetDays</c> is positive when the resolved observation is newer than the requested
    /// date (NextOnOrAfter fallback).
    /// </summary>
    [TestMethod]
    public void SignedOffsetDays_WhenResolvedAfterRequested_ShouldBePositive()
    {
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 7), 1.5m, "RBA");
        RateLookupResult result = new(rate, new DateOnly(2024, 1, 5), RateDateResolution.NextOnOrAfter, 2, RateProvenance.Live(rate.Provider));

        Assert.AreEqual(2, result.SignedOffsetDays);
        Assert.IsTrue(result.IsFutureDate);
        Assert.IsFalse(result.IsPreviousDate);
    }

    /// <summary>
    /// Verifies that exact-date matches report neither previous nor future direction.
    /// </summary>
    [TestMethod]
    public void DirectionFlags_WhenExactDateMatch_ShouldBothBeFalse()
    {
        DateOnly date = new(2024, 1, 5);
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, date, 1.5m, "RBA");
        RateLookupResult result = new(rate, date, RateDateResolution.Exact, 0, RateProvenance.Live(rate.Provider));

        Assert.AreEqual(0, result.SignedOffsetDays);
        Assert.IsFalse(result.IsPreviousDate);
        Assert.IsFalse(result.IsFutureDate);
    }
}
