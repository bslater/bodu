// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public partial class RateSeriesTests
{
    private static readonly CurrencyPair s_usdAud = new(CurrencyCode.USD, CurrencyCode.AUD);

    private static (DateOnly Date, decimal Rate)[] SampleRates() =>
    [
        (new DateOnly(2024, 1, 2), 1.50m),
        (new DateOnly(2024, 1, 3), 1.51m),
        (new DateOnly(2024, 1, 5), 1.52m),
    ];

    /// <summary>
    /// Verifies that the constructor stores the supplied pair, provider, and count.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Constructor_WhenInputsValid_ShouldStorePairProviderAndCount()
    {
        RateSeries series = new(s_usdAud, "RBA", SampleRates());

        Assert.AreEqual(s_usdAud, series.Pair);
        Assert.AreEqual("RBA", series.Provider);
        Assert.AreEqual(3, series.Count);
    }

    /// <summary>
    /// Verifies that the constructor sorts unsorted input so subsequent lookups resolve correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenInputDatesUnsorted_ShouldSortAndResolveCorrectly()
    {
        (DateOnly Date, decimal Rate)[] unsorted =
        [
            (new DateOnly(2024, 1, 5), 1.52m),
            (new DateOnly(2024, 1, 2), 1.50m),
            (new DateOnly(2024, 1, 3), 1.51m),
        ];

        RateSeries series = new(s_usdAud, "RBA", unsorted);

        bool found = series.TryGetRate(
            new DateOnly(2024, 1, 3),
            RateLookupOptions.Exact,
            out _,
            out decimal rate);

        Assert.IsTrue(found);
        Assert.AreEqual(1.51m, rate);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> rate enumerable throws <see cref="ArgumentNullException" /> with the
    /// parameter name <c>rates</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRatesIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new RateSeries(s_usdAud, "RBA", (IEnumerable<(DateOnly, decimal)>)null!);
            },
            "rates");
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> provider throws <see cref="ArgumentNullException" /> with the parameter
    /// name <c>provider</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new RateSeries(s_usdAud, null!, SampleRates());
            },
            "provider");
    }

    /// <summary>
    /// Verifies that an empty rate enumerable throws <see cref="ArgumentException" /> with the parameter name <c>rates</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRatesIsEmpty_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new RateSeries(s_usdAud, "RBA", (IEnumerable<(DateOnly, decimal)>)[]);
            },
            "rates");
    }

    /// <summary>
    /// Verifies that a zero rate throws <see cref="ArgumentOutOfRangeException" /> with the parameter name <c>rates</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRateIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new RateSeries(s_usdAud, "RBA", [(new DateOnly(2024, 1, 1), 0m)]);
            },
            "rates");
    }

    /// <summary>
    /// Verifies that a negative rate throws <see cref="ArgumentOutOfRangeException" /> with the parameter name <c>rates</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRateIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new RateSeries(s_usdAud, "RBA", [(new DateOnly(2024, 1, 1), -1m)]);
            },
            "rates");
    }

    /// <summary>
    /// Verifies that duplicate dates throw <see cref="ArgumentException" /> with the parameter name <c>rates</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDatesContainDuplicate_ShouldThrowArgumentException()
    {
        (DateOnly Date, decimal Rate)[] duplicates =
        [
            (new DateOnly(2024, 1, 2), 1.50m),
            (new DateOnly(2024, 1, 2), 1.55m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new RateSeries(s_usdAud, "RBA", duplicates);
            },
            "rates");
    }

    /// <summary>
    /// Verifies that an empty provider throws <see cref="ArgumentException" /> with the parameter name <c>provider</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderIsEmpty_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new RateSeries(s_usdAud, "   ", SampleRates());
            },
            "provider");
    }

    /// <summary>
    /// Verifies that the tuple-overload constructor rejects a <see langword="default" /> pair, which bypasses the
    /// pair's own constructor validation and carries null ISO codes.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPairIsDefault_ForTuples_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new RateSeries(default, "RBA", SampleRates());
            },
            "pair");
    }

    /// <summary>
    /// Verifies that the observation-overload constructor rejects a <see langword="default" /> pair, which bypasses
    /// the pair's own constructor validation and carries null ISO codes.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPairIsDefault_ForObservations_ShouldThrowArgumentException()
    {
        RateObservation[] observations =
        [
            new(new DateOnly(2024, 1, 2), 1.50m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new RateSeries(default, "RBA", observations);
            },
            "pair");
    }
}
