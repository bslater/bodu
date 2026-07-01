// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateProviderTests.LoadPairAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that warming a pair through the shared <see cref="WebExchangeRateProvider.LoadPairAsync" /> surface loads
    /// the eras covering the window, so RBA exposes the same warm-up verb as the pair-based providers.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWarmed_ShouldResolvePublishedRate()
    {
        (RbaExchangeRateProvider provider, FixtureRbaExchangeRateTableSource source) = Create(allowSync: false);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual(RbaExchangeRateProvider.ProviderName, result.Rate.Provider);
        Assert.AreEqual(1, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that a pair the RBA feed does not serve — neither side being AUD — is rejected before any download.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenNeitherSideIsAud_ShouldThrowExchangeRateSeriesNotFoundException()
    {
        (RbaExchangeRateProvider provider, FixtureRbaExchangeRateTableSource source) = Create(allowSync: false);

        _ = await Assert.ThrowsExactlyAsync<ExchangeRateSeriesNotFoundException>(async () =>
        {
            await provider.LoadPairAsync("USD", "EUR", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));
        });

        Assert.AreEqual(0, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that a pair warmed through <c>LoadPairAsync</c> is surfaced through the shared
    /// <see cref="WebExchangeRateProvider.GetLoadedPairs" /> discovery surface.
    /// </summary>
    [TestMethod]
    public async Task GetLoadedPairs_AfterLoadPair_ShouldIncludeAudUsd()
    {
        (RbaExchangeRateProvider provider, _) = Create(allowSync: false);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));

        CollectionAssert.Contains(provider.GetLoadedPairs().ToList(), new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD));
    }
}
