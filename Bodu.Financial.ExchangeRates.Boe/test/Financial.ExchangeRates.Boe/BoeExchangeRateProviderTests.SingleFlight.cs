// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateProviderTests.SingleFlight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Financial.ExchangeRates.Boe;

public partial class BoeExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that concurrent loads of the same range are coalesced into a single underlying fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenCalledConcurrentlyForSameRange_ShouldFetchOnce()
    {
        BoeExchangeRateOptions options = new() { EnableDiskCache = false };
        GatedBoeExchangeRateTableSource source = new(options);
        BoeExchangeRateProvider provider = new(source, options);
        DateOnly start = new(2023, 1, 1);
        DateOnly end = new(2023, 12, 31);

        Task[] loads =
        [
            provider.LoadRangeAsync(start, end),
            provider.LoadRangeAsync(start, end),
            provider.LoadRangeAsync(start, end),
            provider.LoadRangeAsync(start, end),
        ];

        await source.Entered;
        source.Release();
        await Task.WhenAll(loads);

        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that a load issued after a prior load has completed performs no fetch, because the range is already
    /// covered by the in-memory store.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenRangeAlreadyLoaded_ShouldNotFetchAgain()
    {
        BoeExchangeRateOptions options = new() { EnableDiskCache = false };
        GatedBoeExchangeRateTableSource source = new(options);
        BoeExchangeRateProvider provider = new(source, options);
        DateOnly start = new(2023, 1, 1);
        DateOnly end = new(2023, 12, 31);

        source.Release();
        await provider.LoadRangeAsync(start, end);
        await provider.LoadRangeAsync(start, end);

        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that when one of several concurrent loads of the same range cancels its token, only that caller faults
    /// while the range still loads for the remaining callers from the single shared fetch.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public async Task LoadRangeAsync_WhenOneConcurrentCallerCancels_ShouldStillLoadForOthersAndFetchOnce()
    {
        BoeExchangeRateOptions options = new() { EnableDiskCache = false };
        GatedBoeExchangeRateTableSource source = new(options);
        BoeExchangeRateProvider provider = new(source, options);
        DateOnly start = new(2023, 1, 1);
        DateOnly end = new(2023, 12, 31);

        using CancellationTokenSource cancellingCts = new();

        Task cancelling = provider.LoadRangeAsync(start, end, cancellingCts.Token);
        Task survivor1 = provider.LoadRangeAsync(start, end);
        Task survivor2 = provider.LoadRangeAsync(start, end);

        await source.Entered;
        cancellingCts.Cancel();
        source.Release();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await cancelling);
        await survivor1;
        await survivor2;

        Assert.AreEqual(1, source.CallCount);
    }
}
