// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExchangeRateExtensionsTests.ConvertToWithRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial;

public partial class MoneyExchangeRateExtensionsTests
{

    /// <summary>
    /// Verifies that the audit-bearing overload returns the lookup metadata alongside the converted amount.
    /// </summary>
    [TestMethod]
    public void ConvertToWithRate_WhenRateAvailable_ShouldReturnAmountAndAuditMetadata()
    {
        Money source = new(100m, CurrencyCode.EUR);

        (Money target, RateLookupResult lookup) = source.ConvertToWithRate(
            BuildProvider(),
            "USD",
            s_asOf,
            RateLookupOptions.Exact);

        Assert.AreEqual(new Money(110m, CurrencyCode.USD), target);
        Assert.AreEqual(1.10m, lookup.Rate.Rate);
        Assert.AreEqual("RBA", lookup.Rate.Provider);
        Assert.IsFalse(lookup.Rate.IsInverted);
        Assert.AreEqual(0, lookup.OffsetDays);
    }

    /// <summary>
    /// Verifies that the inverse-direction path correctly flips the rate and marks the result as inverted.
    /// </summary>
    [TestMethod]
    public void ConvertToWithRate_WhenOnlyInverseRateAvailable_ShouldFlagInversion()
    {
        // Only EUR/USD is in the table; converting USD → EUR uses the inverse.
        Money source = new(110m, CurrencyCode.USD);

        (Money target, RateLookupResult lookup) = source.ConvertToWithRate(
            BuildProvider(),
            "EUR",
            s_asOf,
            RateLookupOptions.Exact);

        Assert.AreEqual(CurrencyCode.EUR, target.Code);
        Assert.IsTrue(lookup.Rate.IsInverted);
        Assert.AreEqual("RBA", lookup.Rate.Provider);
    }
}
