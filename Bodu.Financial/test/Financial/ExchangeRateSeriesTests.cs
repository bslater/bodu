// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateSeriesTests
{
    private static readonly ExchangeRatePair s_usdAud = new("USD", "AUD");

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
        ExchangeRateSeries series = new(s_usdAud, "RBA", SampleRates());

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

        ExchangeRateSeries series = new(s_usdAud, "RBA", unsorted);

        var found = series.TryGetRate(
            new DateOnly(2024, 1, 3),
            ExchangeRateLookupOptions.Exact,
            out _,
            out var rate);

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
                _ = new ExchangeRateSeries(s_usdAud, "RBA", null!);
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
                _ = new ExchangeRateSeries(s_usdAud, null!, SampleRates());
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
                _ = new ExchangeRateSeries(s_usdAud, "RBA", []);
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
                _ = new ExchangeRateSeries(s_usdAud, "RBA", [(new DateOnly(2024, 1, 1), 0m)]);
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
                _ = new ExchangeRateSeries(s_usdAud, "RBA", [(new DateOnly(2024, 1, 1), -1m)]);
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
                _ = new ExchangeRateSeries(s_usdAud, "RBA", duplicates);
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
                _ = new ExchangeRateSeries(s_usdAud, "   ", SampleRates());
            },
            "provider");
    }
}
