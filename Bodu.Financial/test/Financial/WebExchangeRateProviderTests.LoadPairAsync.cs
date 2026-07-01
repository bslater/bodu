// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebExchangeRateProviderTests.LoadPairAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class WebExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that warming a pair through the base <c>LoadPairAsync</c> makes the seeded known date resolve, proving a
    /// bulk feed that is not built on <see cref="PairWebExchangeRateProvider{TSeries}" /> gains the same warm-up verb.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWarmed_ShouldResolveKnownDate()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", Known, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual("TESTBULK", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a reverse-direction request whose destination is the base currency loads, so the base-currency
    /// check accepts the pair from either side.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBaseCurrencyIsDestination_ShouldLoad()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        await provider.LoadPairAsync("USD", "AUD", RangeStart, RangeEnd);

        Assert.AreEqual(1, provider.LoadCount);
    }

    /// <summary>
    /// Verifies that re-warming an already-covered pair and window is idempotent, fetching from the feed only once.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWindowAlreadyCovered_ShouldFetchOnce()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);
        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        Assert.AreEqual(1, provider.LoadCount);
    }

    /// <summary>
    /// Verifies that an inverted range throws an <see cref="ArgumentException" /> before any fetch is attempted.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRangeInverted_ShouldThrowArgumentException()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        _ = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await provider.LoadPairAsync("AUD", "USD", RangeEnd, RangeStart);
        });

        Assert.AreEqual(0, provider.LoadCount);
    }

    /// <summary>
    /// Verifies that a null ISO code throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenFromIsoCodeIsNull_ShouldThrowArgumentNullException()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await provider.LoadPairAsync(null!, "USD", RangeStart, RangeEnd);
        });
    }

    /// <summary>
    /// Verifies that a pair the feed does not serve — neither side being the base currency — is rejected through the
    /// provider's <see cref="WebExchangeRateProvider.ValidateRangeRequest" /> hook before any fetch is attempted.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenNeitherSideIsBaseCurrency_ShouldThrowExchangeRateSeriesNotFoundException()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        _ = await Assert.ThrowsExactlyAsync<ExchangeRateSeriesNotFoundException>(async () =>
        {
            await provider.LoadPairAsync("USD", "EUR", RangeStart, RangeEnd);
        });

        Assert.AreEqual(0, provider.LoadCount);
    }
}
