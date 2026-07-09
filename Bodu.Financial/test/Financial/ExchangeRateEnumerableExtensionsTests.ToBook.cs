// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateEnumerableExtensionsTests.ToBook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.Extensions;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateEnumerableExtensionsTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> sequence throws <see cref="ArgumentNullException" /> with the parameter
    /// name <c>rates</c>.
    /// </summary>
    [TestMethod]
    public void ToBook_WhenRatesIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = ((IEnumerable<ExchangeRate>)null!).ToBook();
            },
            "rates");
    }

    /// <summary>
    /// Verifies that an empty sequence materializes to an empty book.
    /// </summary>
    [TestMethod]
    public void ToBook_WhenRatesIsEmpty_ShouldReturnEmptyBook()
    {
        ExchangeRateBook book = Array.Empty<ExchangeRate>().ToBook();

        Assert.AreEqual(0, book.Count);
    }

    /// <summary>
    /// Verifies that rates for the same pair from different providers become separate series, unlike the
    /// single-provider-per-pair <see cref="FixedDatedExchangeRateProvider(IEnumerable{ExchangeRate})" /> constructor.
    /// </summary>
    [TestMethod]
    public void ToBook_WhenSamePairHasMultipleProviders_ShouldKeepOneSeriesPerProvider()
    {
        ExchangeRate[] rates =
        [
            Rate("AUD", "USD", D1, 0.68m, "RBA"),
            Rate("AUD", "USD", D1, 0.69m, "ECB"),
        ];

        ExchangeRateBook book = rates.ToBook();

        Assert.AreEqual(2, book.Count);
        Assert.IsTrue(book.TryGetSeries(AudUsd, "RBA", out ExchangeRateSeries? rba));
        Assert.AreEqual(0.68m, rba!.GetObservations().Single().Rate);
        Assert.IsTrue(book.TryGetSeries(AudUsd, "ECB", out ExchangeRateSeries? ecb));
        Assert.AreEqual(0.69m, ecb!.GetObservations().Single().Rate);
    }

    /// <summary>
    /// Verifies that duplicate (pair, provider, date) observations resolve by last-write-wins, so re-materializing
    /// overlapping fetches never throws.
    /// </summary>
    [TestMethod]
    public void ToBook_WhenDuplicateDate_ShouldKeepLastObservation()
    {
        ExchangeRate[] rates =
        [
            Rate("AUD", "USD", D1, 0.68m, "RBA"),
            Rate("AUD", "USD", D1, 0.70m, "RBA"),
        ];

        ExchangeRateBook book = rates.ToBook();

        Assert.IsTrue(book.TryGetSeries(AudUsd, "RBA", out ExchangeRateSeries? series));
        Assert.AreEqual(0.70m, series!.GetObservations().Single().Rate);
    }

    /// <summary>
    /// Verifies that each series' fetch instant is the latest non-null instant among its rows, independent of input
    /// order.
    /// </summary>
    [TestMethod]
    public void ToBook_WithFetchInstants_ShouldKeepLatestPerSeries()
    {
        DateTimeOffset earlier = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset later = new(2024, 1, 5, 0, 0, 0, TimeSpan.Zero);
        ExchangeRate[] rates =
        [
            Rate("AUD", "USD", D1, 0.68m, "RBA", later),
            Rate("AUD", "USD", D1.AddDays(1), 0.69m, "RBA", earlier),
        ];

        ExchangeRateBook book = rates.ToBook();

        Assert.IsTrue(book.TryGetSeries(AudUsd, "RBA", out ExchangeRateSeries? series));
        Assert.AreEqual(later, series!.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that an inverse-resolved row is stored under its natively quoted pair with the originally observed
    /// rate, rather than baking the derived reciprocal into the book, and that the resulting book still resolves the
    /// derived direction through inverse fallback.
    /// </summary>
    [TestMethod]
    public void ToBook_WithInvertedRates_ShouldStoreNativeQuoteDirection()
    {
        // A USD->AUD row derived from an AUD/USD observation of 0.68: Rate is the pre-rounded reciprocal.
        ExchangeRate inverted = new(CurrencyCode.USD, CurrencyCode.AUD, D1, 1m / 0.68m, "RBA", isInverted: true);

        ExchangeRateBook book = new[] { inverted }.ToBook();

        Assert.AreEqual(1, book.Count);
        Assert.IsTrue(book.TryGetSeries(AudUsd, "RBA", out ExchangeRateSeries? series), "the natively quoted AUD/USD series holds the observation");
        Assert.AreEqual(0.68m, series!.GetObservations().Single().Rate);

        FixedDatedExchangeRateProvider provider = new(book);
        Assert.IsTrue(provider.TryGetRate("USD", "AUD", D1, null, out ExchangeRateLookupResult result), "the derived direction still resolves via inverse fallback");
        Assert.IsTrue(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that the rows a provider's range lookup returns round-trip into a book resolving the same rates.
    /// </summary>
    [TestMethod]
    public void ToBook_WhenFedGetRatesRows_ShouldRoundTrip()
    {
        FixedDatedExchangeRateProvider source = new(new[] { Rate("AUD", "USD", D1, 0.6828m, "RBA") });

        ExchangeRateRangeResult fetched = source.GetRates("AUD", "USD", D1, D1);
        ExchangeRateBook book = fetched.ToBook();

        Assert.IsTrue(book.TryGetSeries(AudUsd, "RBA", out ExchangeRateSeries? series));
        Assert.AreEqual(0.6828m, series!.GetObservations().Single().Rate);
    }
}
