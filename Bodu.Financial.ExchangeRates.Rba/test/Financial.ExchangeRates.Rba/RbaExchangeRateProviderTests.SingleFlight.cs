// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateProviderTests.SingleFlight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

public partial class RbaExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that concurrent loads of the same era are coalesced into a single underlying fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadEraAsync_WhenCalledConcurrentlyForSameEra_ShouldFetchOnce()
    {
        RbaExchangeRateOptions options = new() { EnableDiskCache = false };
        GatedRbaExchangeRateTableSource source = new(options);
        RbaExchangeRateProvider provider = new(source, options);
        RbaEra era = new("2023-current", new DateOnly(2023, 1, 1), null);

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
        RbaExchangeRateOptions options = new() { EnableDiskCache = false };
        GatedRbaExchangeRateTableSource source = new(options);
        RbaExchangeRateProvider provider = new(source, options);
        RbaEra era = new("2023-current", new DateOnly(2023, 1, 1), null);

        source.Release();
        await provider.LoadEraAsync(era);
        await provider.LoadEraAsync(era);

        Assert.AreEqual(1, source.CallCount);
    }
}
