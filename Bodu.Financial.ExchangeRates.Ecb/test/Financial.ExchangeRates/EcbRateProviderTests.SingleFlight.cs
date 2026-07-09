// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderTests.SingleFlight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderTests
{
    /// <summary>
    /// Verifies that concurrent loads of the same feed are coalesced into a single underlying fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadFeedAsync_WhenCalledConcurrentlyForSameFeed_ShouldFetchOnce()
    {
        EcbRateProviderOptions options = new() { EnableDiskCache = false };
        GatedEcbRateTableSource source = new(options);
        EcbRateProvider provider = new(source, options);
        EcbRateFeed feed = new("hist", "eurofxref-hist.xml", null);

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
        EcbRateProviderOptions options = new() { EnableDiskCache = false };
        GatedEcbRateTableSource source = new(options);
        EcbRateProvider provider = new(source, options);
        EcbRateFeed feed = new("hist", "eurofxref-hist.xml", null);

        source.Release();
        await provider.LoadFeedAsync(feed);
        await provider.LoadFeedAsync(feed);

        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that when one of several concurrent loads of the same feed cancels its token, only that caller faults
    /// while the feed still loads for the remaining callers from the single shared fetch.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public async Task LoadFeedAsync_WhenOneConcurrentCallerCancels_ShouldStillLoadForOthersAndFetchOnce()
    {
        EcbRateProviderOptions options = new() { EnableDiskCache = false };
        GatedEcbRateTableSource source = new(options);
        EcbRateProvider provider = new(source, options);
        EcbRateFeed feed = new("hist", "eurofxref-hist.xml", null);

        using CancellationTokenSource cancellingCts = new();

        Task cancelling = provider.LoadFeedAsync(feed, cancellingCts.Token);
        Task survivor1 = provider.LoadFeedAsync(feed);
        Task survivor2 = provider.LoadFeedAsync(feed);

        await source.Entered;
        cancellingCts.Cancel();
        source.Release();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await cancelling);
        await survivor1;
        await survivor2;

        Assert.AreEqual(1, source.CallCount);
    }
}
