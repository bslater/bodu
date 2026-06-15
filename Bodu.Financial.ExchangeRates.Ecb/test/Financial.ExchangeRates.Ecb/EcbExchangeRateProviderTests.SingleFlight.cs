// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateProviderTests.SingleFlight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

public partial class EcbExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that concurrent loads of the same feed are coalesced into a single underlying fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadFeedAsync_WhenCalledConcurrentlyForSameFeed_ShouldFetchOnce()
    {
        EcbExchangeRateOptions options = new() { EnableDiskCache = false };
        GatedEcbExchangeRateTableSource source = new(options);
        EcbExchangeRateProvider provider = new(source, options);
        EcbExchangeRateFeed feed = new("hist", "eurofxref-hist.xml", null);

        Task[] loads =
        [
            provider.LoadFeedAsync(feed),
            provider.LoadFeedAsync(feed),
            provider.LoadFeedAsync(feed),
            provider.LoadFeedAsync(feed),
        ];

        await source.Entered;
        source.Release();
        await Task.WhenAll(loads);

        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that a load issued after a prior load has completed performs no fetch, because the feed is already
    /// present in the in-memory store.
    /// </summary>
    [TestMethod]
    public async Task LoadFeedAsync_WhenFeedAlreadyLoaded_ShouldNotFetchAgain()
    {
        EcbExchangeRateOptions options = new() { EnableDiskCache = false };
        GatedEcbExchangeRateTableSource source = new(options);
        EcbExchangeRateProvider provider = new(source, options);
        EcbExchangeRateFeed feed = new("hist", "eurofxref-hist.xml", null);

        source.Release();
        await provider.LoadFeedAsync(feed);
        await provider.LoadFeedAsync(feed);

        Assert.AreEqual(1, source.CallCount);
    }
}
