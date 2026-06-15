// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateProviderTests.SingleFlight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
