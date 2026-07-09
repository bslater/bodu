// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooRateProviderTests.SingleFlight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

public partial class YahooRateProviderTests
{
    /// <summary>
    /// Verifies that concurrent loads of the same pair and window are coalesced into a single underlying fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenCalledConcurrentlyForSamePairAndWindow_ShouldFetchOnce()
    {
        YahooRateProviderOptions options = new();
        GatedYahooRateSource source = new(options);
        YahooRateProvider provider = new(source, options);
        DateOnly start = new(2023, 1, 1);
        DateOnly end = new(2023, 1, 31);

        Task[] loads =
        [
            provider.LoadPairAsync("AUD", "USD", start, end),
            provider.LoadPairAsync("AUD", "USD", start, end),
            provider.LoadPairAsync("AUD", "USD", start, end),
            provider.LoadPairAsync("AUD", "USD", start, end),
        ];

        await source.Entered;
        source.Release();
        await Task.WhenAll(loads);

        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that a load issued after a prior load has completed performs no fetch, because the window is already
    /// present in the pair's coverage set.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWindowAlreadyLoaded_ShouldNotFetchAgain()
    {
        YahooRateProviderOptions options = new();
        GatedYahooRateSource source = new(options);
        YahooRateProvider provider = new(source, options);
        DateOnly start = new(2023, 1, 1);
        DateOnly end = new(2023, 1, 31);

        source.Release();
        await provider.LoadPairAsync("AUD", "USD", start, end);
        await provider.LoadPairAsync("AUD", "USD", start, end);

        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that when one of several concurrent loads of the same pair and window cancels its token, only that
    /// caller faults while the window still loads for the remaining callers from the single shared fetch.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public async Task LoadPairAsync_WhenOneConcurrentCallerCancels_ShouldStillLoadForOthersAndFetchOnce()
    {
        YahooRateProviderOptions options = new();
        GatedYahooRateSource source = new(options);
        YahooRateProvider provider = new(source, options);
        DateOnly start = new(2023, 1, 1);
        DateOnly end = new(2023, 1, 31);

        using CancellationTokenSource cancellingCts = new();

        Task cancelling = provider.LoadPairAsync("AUD", "USD", start, end, cancellingCts.Token);
        Task survivor1 = provider.LoadPairAsync("AUD", "USD", start, end);
        Task survivor2 = provider.LoadPairAsync("AUD", "USD", start, end);

        await source.Entered;
        cancellingCts.Cancel();
        source.Release();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await cancelling);
        await survivor1;
        await survivor2;

        Assert.AreEqual(1, source.CallCount);
    }
}
