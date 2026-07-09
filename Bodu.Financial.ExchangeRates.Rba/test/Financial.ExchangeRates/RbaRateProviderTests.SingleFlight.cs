// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.SingleFlight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>
    /// Verifies that concurrent loads of the same era are coalesced into a single underlying fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadEraAsync_WhenCalledConcurrentlyForSameEra_ShouldFetchOnce()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        GatedRbaRateTableSource source = new(options);
        RbaRateProvider provider = new(source, options);
        RbaEraWorkbook era = new("2023-current", new DateOnly(2023, 1, 1), null);

        Task[] loads =
        [
            provider.LoadEraAsync(era),
            provider.LoadEraAsync(era),
            provider.LoadEraAsync(era),
            provider.LoadEraAsync(era),
        ];

        await source.Entered;
        source.Release();
        await Task.WhenAll(loads);

        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that a load issued after a prior load has completed performs no fetch, because the era is already
    /// present in the in-memory store.
    /// </summary>
    [TestMethod]
    public async Task LoadEraAsync_WhenEraAlreadyLoaded_ShouldNotFetchAgain()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        GatedRbaRateTableSource source = new(options);
        RbaRateProvider provider = new(source, options);
        RbaEraWorkbook era = new("2023-current", new DateOnly(2023, 1, 1), null);

        source.Release();
        await provider.LoadEraAsync(era);
        await provider.LoadEraAsync(era);

        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that when one of several concurrent loads of the same era cancels its token, only that caller faults
    /// while the era still loads for the remaining callers from the single shared fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadEraAsync_WhenOneConcurrentCallerCancels_ShouldStillLoadForOthersAndFetchOnce()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false };
        GatedRbaRateTableSource source = new(options);
        RbaRateProvider provider = new(source, options);
        RbaEraWorkbook era = new("2023-current", new DateOnly(2023, 1, 1), null);

        using CancellationTokenSource cancellingCts = new();

        // One caller cancels its wait; the other two await the same shared fetch under uncancellable tokens.
        Task cancelling = provider.LoadEraAsync(era, cancellingCts.Token);
        Task survivor1 = provider.LoadEraAsync(era);
        Task survivor2 = provider.LoadEraAsync(era);

        // Let the race form, cancel one caller's wait, then release the shared fetch so the survivors complete.
        await source.Entered;
        cancellingCts.Cancel();
        source.Release();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await cancelling);
        await survivor1;
        await survivor2;

        Assert.AreEqual(1, source.CallCount);
        Assert.IsTrue(provider.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), null, out _));
    }
}
