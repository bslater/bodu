// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagTests.DatedConvert.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyBagTests
{
    /// <summary>
    /// The shared as-of date for the dated-conversion fixture.
    /// </summary>
    private static readonly DateOnly s_asOf = new(2024, 6, 30);

    /// <summary>
    /// Builds a fixture provider with EUR/USD = 1.10, JPY/USD = 0.0067, GBP/USD = 1.25 observed on <see cref="s_asOf" />.
    /// </summary>
    /// <returns>The provider.</returns>
    private static IDatedExchangeRateProvider BuildDatedProvider() => new FixedDatedExchangeRateTable(new[]
    {
        new ExchangeRate("EUR", "USD", s_asOf, 1.10m, "RBA"),
        new ExchangeRate("JPY", "USD", s_asOf, 0.0067m, "RBA"),
        new ExchangeRate("GBP", "USD", s_asOf, 1.25m, "RBA"),
    });

    /// <summary>
    /// Verifies that the dated <see cref="MoneyBag.ConvertTo{TTarget}(IDatedExchangeRateProvider, DateOnly, ExchangeRateLookupOptions)" />
    /// overload aggregates a multi-currency bag at the supplied valuation date.
    /// </summary>
    [TestMethod]
    public void DatedConvertTo_WhenBagContainsMultipleCurrencies_ShouldAggregateToTargetUsingProvider()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money<USD>(100m))
            .Add(new Money<EUR>(50m))
            .Add(new Money<JPY>(10_000m));

        Money<USD> total = bag.ConvertTo<USD>(BuildDatedProvider(), s_asOf, ExchangeRateLookupOptions.Exact);

        // 100 + 50×1.10 + 10000×0.0067 = 100 + 55 + 67 = 222
        Assert.AreEqual(new Money<USD>(222m), total);
    }

    /// <summary>
    /// Verifies that the dated convert path honours <see cref="MoneyBagConversionRoundingPolicy.RoundEachCurrencyThenSum" />
    /// for ledger workflows where each per-currency line must settle before aggregation.
    /// </summary>
    [TestMethod]
    [DataRow(MoneyBagConversionRoundingPolicy.SumRawThenRound, 0.01)]
    [DataRow(MoneyBagConversionRoundingPolicy.RoundEachCurrencyThenSum, 0.00)]
    public void DatedConvertTo_WhenTwoSubMinorUnitLinesAggregate_ShouldHonourPolicy(MoneyBagConversionRoundingPolicy policy, double expected)
    {
        // Use unregistered currency tags (valid ISO shape, not in the catalogue) so MoneyValue preserves the
        // sub-cent source precision — same trick as the timeless-rate test.
        IDatedExchangeRateProvider rates = new FixedDatedExchangeRateTable(new[]
        {
            new ExchangeRate("XQT", "USD", s_asOf, 1m, "Test"),
            new ExchangeRate("XQU", "USD", s_asOf, 1m, "Test"),
        });

        MoneyBag bag = MoneyBag.Empty
            .Add(new MoneyValue(0.005m, "XQT"))
            .Add(new MoneyValue(0.005m, "XQU"));

        Money<USD> total = bag.ConvertTo<USD>(rates, s_asOf, ExchangeRateLookupOptions.Exact, policy);

        Assert.AreEqual(new Money<USD>((decimal)expected), total);
    }

    /// <summary>
    /// Verifies that the dated overload uses the lookup options — e.g. <see cref="ExchangeRateLookupOptions.PreviousWithin(int)" />
    /// — to resolve fall-back dates within the requested tolerance.
    /// </summary>
    [TestMethod]
    public void DatedConvertTo_WhenResolutionPolicyAppliesFallback_ShouldUseEarlierObservation()
    {
        IDatedExchangeRateProvider rates = new FixedDatedExchangeRateTable(new[]
        {
            new ExchangeRate("EUR", "USD", new DateOnly(2024, 6, 28), 1.10m, "RBA"),
        });

        MoneyBag bag = MoneyBag.Empty.Add(new Money<EUR>(100m));

        Money<USD> total = bag.ConvertTo<USD>(rates, s_asOf, ExchangeRateLookupOptions.PreviousWithin(5));

        Assert.AreEqual(new Money<USD>(110m), total);
    }

    /// <summary>
    /// Verifies that the dated overload propagates <see cref="KeyNotFoundException" /> when a rate is unavailable
    /// for a currency held in the bag.
    /// </summary>
    [TestMethod]
    public void DatedConvertTo_WhenRateUnavailable_ShouldThrowKeyNotFoundException()
    {
        IDatedExchangeRateProvider rates = new FixedDatedExchangeRateTable(new[]
        {
            new ExchangeRate("EUR", "USD", s_asOf, 1.10m, "RBA"),
        });

        MoneyBag bag = MoneyBag.Empty
            .Add(new Money<EUR>(100m))
            .Add(new Money<GBP>(50m));   // no GBP/USD rate

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = bag.ConvertTo<USD>(rates, s_asOf, ExchangeRateLookupOptions.Exact);
        });
    }

    /// <summary>
    /// Verifies that the dated overload rejects a <see langword="null" /> provider.
    /// </summary>
    [TestMethod]
    public void DatedConvertTo_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money<EUR>(100m));

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = bag.ConvertTo<USD>((IDatedExchangeRateProvider)null!, s_asOf, ExchangeRateLookupOptions.Exact);
        });
    }

    /// <summary>
    /// Verifies that a same-currency target balance bypasses the rate lookup when
    /// <see cref="ExchangeRateLookupOptions.AllowSameCurrencyIdentityRate" /> is true (the default).
    /// </summary>
    [TestMethod]
    public void DatedConvertTo_WhenBagAlreadyContainsTargetCurrency_ShouldPassThroughWithoutRateLookup()
    {
        // No USD/USD rate in the provider — proves the same-currency identity is taking effect.
        IDatedExchangeRateProvider rates = BuildDatedProvider();

        MoneyBag bag = MoneyBag.Empty.Add(new Money<USD>(75m));

        Money<USD> total = bag.ConvertTo<USD>(rates, s_asOf, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(new Money<USD>(75m), total);
    }

    /// <summary>
    /// Verifies that the dated convert path returns audit metadata when the audit-bearing overload is used.
    /// </summary>
    [TestMethod]
    public void DatedConvertToWithAudit_WhenBagContainsMultipleCurrencies_ShouldReturnPerLineLookupMetadata()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money<EUR>(100m))
            .Add(new Money<JPY>(10_000m));

        MoneyBagConversionAudit<USD> audit = bag.ConvertToWithAudit<USD>(
            BuildDatedProvider(),
            s_asOf,
            ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(2, audit.Lines.Count);
        // Total: 100 × 1.10 + 10000 × 0.0067 = 110 + 67 = 177
        Assert.AreEqual(new Money<USD>(177m), audit.Total);

        // Per-line metadata in ISO-lexicographic order (matches bag enumeration).
        Assert.AreEqual("EUR", audit.Lines[0].SourceIsoCode);
        Assert.AreEqual(1.10m, audit.Lines[0].Rate!.Value.Rate.Rate);
        Assert.AreEqual("RBA", audit.Lines[0].Rate!.Value.Rate.Provider);
        Assert.IsFalse(audit.Lines[0].Rate!.Value.Rate.IsInverted);

        Assert.AreEqual("JPY", audit.Lines[1].SourceIsoCode);
        Assert.AreEqual(0.0067m, audit.Lines[1].Rate!.Value.Rate.Rate);
    }

    /// <summary>
    /// Verifies that an audit line for a target-currency balance is reported as a synthetic identity (Rate = null).
    /// </summary>
    [TestMethod]
    public void DatedConvertToWithAudit_WhenLineMatchesTargetCurrency_ShouldReportAsIdentityWithNullRate()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money<USD>(100m))
            .Add(new Money<EUR>(50m));

        MoneyBagConversionAudit<USD> audit = bag.ConvertToWithAudit<USD>(
            BuildDatedProvider(),
            s_asOf,
            ExchangeRateLookupOptions.Exact);

        // USD line: no rate lookup (identity pass-through).
        MoneyBagConversionLine usdLine = audit.Lines.Single(l => l.SourceIsoCode == "USD");
        Assert.IsNull(usdLine.Rate);
        Assert.AreEqual(100m, usdLine.SourceAmount);
        Assert.AreEqual(100m, usdLine.RawConvertedAmount);
    }
}
