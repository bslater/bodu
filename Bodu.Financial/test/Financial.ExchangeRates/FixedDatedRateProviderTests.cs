// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public partial class FixedDatedRateProviderTests
{
    private static readonly DateOnly s_d1 = new(2024, 1, 3);

    private static ExchangeRate[] SingleRate() =>
    [
        new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_d1, 1.50m, "RBA"),
    ];

    /// <summary>
    /// Verifies that constructing a table from a single rate yields a successful exact lookup.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Constructor_WhenSingleRateSupplied_ShouldResolveExactLookup()
    {
        FixedDatedRateProvider table = new(SingleRate());

        bool found = table.TryGetRate(
            "USD",
            "AUD",
            s_d1,
            RateLookupOptions.Exact,
            out RateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.50m, result.Rate.Rate);
        Assert.AreEqual("RBA", result.Rate.Provider);
        Assert.IsFalse(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> rates enumerable throws <see cref="ArgumentNullException" /> with the
    /// parameter name <c>rates</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRatesIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new FixedDatedRateProvider((IEnumerable<ExchangeRate>)null!);
            },
            "rates");
    }

    /// <summary>
    /// Verifies that two rates for the same pair carrying different provider identifiers cause the constructor to
    /// throw <see cref="ArgumentException" /> with the parameter name <c>rates</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSamePairHasConflictingProviders_ShouldThrowArgumentException()
    {
        ExchangeRate[] rates =
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 2), 1.50m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.51m, "ECB"),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new FixedDatedRateProvider(rates);
            },
            "rates");
    }

    /// <summary>
    /// Verifies that two rates for the same pair on the same date — even with the same provider — cause the
    /// constructor to throw via the underlying series duplicate check.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSamePairHasDuplicateDate_ShouldThrowArgumentException()
    {
        ExchangeRate[] rates =
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_d1, 1.50m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_d1, 1.51m, "RBA"),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new FixedDatedRateProvider(rates);
            },
            "rates");
    }

    /// <summary>
    /// Verifies that <see cref="FixedDatedRateProvider.GetRate" /> throws <see cref="KeyNotFoundException" /> when
    /// no rate is available.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenNoRateAvailable_ShouldThrowKeyNotFoundException()
    {
        FixedDatedRateProvider table = new([]);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
            table.GetRate("USD", "AUD", s_d1, RateLookupOptions.Exact));
    }

    /// <summary>
    /// Verifies that <see cref="FixedDatedRateProvider.GetRate" /> returns the same result that the matching
    /// <see cref="FixedDatedRateProvider.TryGetRate" /> call produces.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenRateAvailable_ShouldReturnSameResultAsTryGetRate()
    {
        FixedDatedRateProvider table = new(SingleRate());

        RateLookupResult viaGet = table.GetRate("USD", "AUD", s_d1, RateLookupOptions.Exact);

        Assert.IsTrue(table.TryGetRate("USD", "AUD", s_d1, RateLookupOptions.Exact, out RateLookupResult viaTry));
        Assert.AreEqual(viaGet, viaTry);
    }

    /// <summary>
    /// Verifies that an invalid source ISO code throws <see cref="ArgumentException" /> with the parameter name
    /// <c>fromIsoCode</c>.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenFromIsoCodeMalformed_ShouldThrowArgumentException()
    {
        FixedDatedRateProvider table = new(SingleRate());

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () => table.TryGetRate("usd", "AUD", s_d1, RateLookupOptions.Exact, out _),
            "fromIsoCode");
    }

    /// <summary>
    /// Verifies that an invalid destination ISO code throws <see cref="ArgumentException" /> with the parameter name
    /// <c>toIsoCode</c>.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenToIsoCodeMalformed_ShouldThrowArgumentException()
    {
        FixedDatedRateProvider table = new(SingleRate());

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () => table.TryGetRate("USD", "aud", s_d1, RateLookupOptions.Exact, out _),
            "toIsoCode");
    }
}
