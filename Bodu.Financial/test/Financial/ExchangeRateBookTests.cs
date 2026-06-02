// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateBookTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateBookTests
{
    private static readonly ExchangeRatePair s_usdAud = new("USD", "AUD");
    private static readonly ExchangeRatePair s_eurAud = new("EUR", "AUD");

    private static ExchangeRateSeries BuildSeries(ExchangeRatePair pair, string provider, decimal rate) =>
        new(pair, provider, new (DateOnly, decimal)[] { (new DateOnly(2024, 1, 1), rate) });

    /// <summary>
    /// Verifies that the smoke-tier happy path constructs a book from a single series and exposes it via the
    /// pair/provider key.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Book_WhenConstructedFromSingleSeries_ShouldExposeSeriesByKey()
    {
        ExchangeRateSeries series = BuildSeries(s_usdAud, "RBA", 1.5m);
        ExchangeRateBook book = new(new[] { series });

        bool found = book.TryGetSeries(s_usdAud, "RBA", out ExchangeRateSeries? resolved);

        Assert.IsTrue(found);
        Assert.AreSame(series, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateBook.TryGetSeries" /> returns <see langword="false" /> when no series
    /// matches the supplied pair/provider key.
    /// </summary>
    [TestMethod]
    public void TryGetSeries_WhenSeriesMissing_ShouldReturnFalse()
    {
        ExchangeRateBook book = new(new[] { BuildSeries(s_usdAud, "RBA", 1.5m) });

        bool found = book.TryGetSeries(s_usdAud, "ECB", out ExchangeRateSeries? resolved);

        Assert.IsFalse(found);
        Assert.IsNull(resolved);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateBook.GetSeries(ExchangeRatePair)" /> returns every series matching the
    /// pair regardless of provider.
    /// </summary>
    [TestMethod]
    public void GetSeries_WhenMultipleProvidersForSamePair_ShouldReturnAllProviders()
    {
        ExchangeRateSeries rba = BuildSeries(s_usdAud, "RBA", 1.5m);
        ExchangeRateSeries ecb = BuildSeries(s_usdAud, "ECB", 1.6m);
        ExchangeRateSeries other = BuildSeries(s_eurAud, "RBA", 1.7m);
        ExchangeRateBook book = new(new[] { rba, ecb, other });

        HashSet<string> providers = book.GetSeries(s_usdAud).Select(s => s.Provider).ToHashSet();

        Assert.AreEqual(2, providers.Count);
        Assert.IsTrue(providers.Contains("RBA"));
        Assert.IsTrue(providers.Contains("ECB"));
    }

    /// <summary>
    /// Verifies that the constructor rejects a series list with two entries sharing the same pair/provider key.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesListContainsDuplicateKey_ShouldThrowArgumentException()
    {
        ExchangeRateSeries first = BuildSeries(s_usdAud, "RBA", 1.5m);
        ExchangeRateSeries duplicate = BuildSeries(s_usdAud, "RBA", 1.6m);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new ExchangeRateBook(new[] { first, duplicate });
            },
            "series");
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> enumerable.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRateBook((IEnumerable<ExchangeRateSeries>)null!);
            },
            "series");
    }

    /// <summary>
    /// Verifies that the constructor rejects a series enumerable containing a <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesContainsNullElement_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRateBook(new ExchangeRateSeries?[] { null }!);
            },
            "series");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateBook.Count" /> reports the number of distinct keys held.
    /// </summary>
    [TestMethod]
    public void Count_WhenMultipleSeries_ShouldReturnNumberOfDistinctKeys()
    {
        ExchangeRateBook book = new(new[]
        {
            BuildSeries(s_usdAud, "RBA", 1.5m),
            BuildSeries(s_usdAud, "ECB", 1.6m),
            BuildSeries(s_eurAud, "RBA", 1.7m),
        });

        Assert.AreEqual(3, book.Count);
    }
}
