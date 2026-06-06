// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateProviderTests.Book.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class FixedDatedExchangeRateProviderTests
{
    private static readonly ExchangeRatePair s_usdAud = new("USD", "AUD");

    private static ExchangeRateSeries Series(string provider, decimal rate) =>
        new(s_usdAud, provider, new (DateOnly Date, decimal Rate)[] { (new DateOnly(2024, 1, 1), rate) });

    /// <summary>
    /// Verifies that the book-based constructor rejects a <see langword="null" /> book.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenBookIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new FixedDatedExchangeRateProvider((ExchangeRateBook)null!);
            },
            "book");
    }

    /// <summary>
    /// Verifies that the book-only constructor accepts a book with one provider per pair and resolves rates from it.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenBookHasOneProviderPerPair_ShouldResolveRate()
    {
        ExchangeRateBook book = new(new[] { Series("RBA", 1.5m) });
        FixedDatedExchangeRateProvider provider = new(book);

        var found = provider.TryGetRate("USD", "AUD", new DateOnly(2024, 1, 1), null, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.5m, result.Rate.Rate);
        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that the book-only constructor rejects a book containing two providers for the same pair, because no
    /// priority list was supplied to disambiguate them.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenBookContainsMultipleProvidersForPair_WithoutPriority_ShouldThrowArgumentException()
    {
        ExchangeRateBook book = new(new[] { Series("RBA", 1.5m), Series("ECB", 1.6m) });

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new FixedDatedExchangeRateProvider(book);
            },
            "book");
    }

    /// <summary>
    /// Verifies that the explicit-priority constructor uses the first provider in the priority list when more than one
    /// provider exists for the pair.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenBookContainsMultipleProvidersForPair_WithPriority_ShouldUseFirstListedProvider()
    {
        ExchangeRateBook book = new(new[] { Series("RBA", 1.5m), Series("ECB", 1.6m) });
        FixedDatedExchangeRateProvider provider = new(book, new[] { "ECB", "RBA" });

        var found = provider.TryGetRate("USD", "AUD", new DateOnly(2024, 1, 1), null, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.6m, result.Rate.Rate);
        Assert.AreEqual("ECB", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that the explicit-priority constructor falls back to the next provider when the preferred provider has
    /// no rate available on the requested date.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenPreferredProviderHasNoRateAndFallbackProviderDoes_ShouldUseFallback()
    {
        ExchangeRateSeries rba = new(s_usdAud, "RBA", new (DateOnly, decimal)[] { (new DateOnly(2024, 1, 1), 1.5m) });
        ExchangeRateSeries ecb = new(s_usdAud, "ECB", new (DateOnly, decimal)[] { (new DateOnly(2024, 1, 2), 1.6m) });
        ExchangeRateBook book = new(new[] { rba, ecb });
        FixedDatedExchangeRateProvider provider = new(book, new[] { "RBA", "ECB" });

        // RBA has nothing for 2024-01-02; ECB does. Exact requires the date to match exactly.
        var found = provider.TryGetRate("USD", "AUD", new DateOnly(2024, 1, 2), null, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.6m, result.Rate.Rate);
        Assert.AreEqual("ECB", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that the explicit-priority constructor rejects a <see langword="null" /> priority list.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderPriorityIsNull_ShouldThrowArgumentNullException()
    {
        ExchangeRateBook book = new(new[] { Series("RBA", 1.5m) });

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new FixedDatedExchangeRateProvider(book, (IEnumerable<string>)null!);
            },
            "providerPriority");
    }
}
