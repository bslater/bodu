// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderTests.LoadRangeCoverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderTests
{
    /// <summary>
    /// Verifies that consecutively loaded adjacent ranges jointly cover their complete inclusive window, so neither
    /// an explicit load nor a subsequent synchronous or asynchronous range read triggers another download.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenAdjacentRangesLoaded_ShouldNotReloadTheirUnion()
    {
        (BoeRateProvider provider, FixtureBoeRateTableSource source) = Create(allowSync: true);
        DateOnly start = new(2023, 1, 1);
        DateOnly end = new(2023, 1, 31);

        await provider.LoadRangeAsync(start, new DateOnly(2023, 1, 15));
        await provider.LoadRangeAsync(new DateOnly(2023, 1, 16), end);
        await provider.LoadRangeAsync(start, end);
        _ = provider.GetRates("GBP", "USD", start, end);
        _ = await provider.GetRatesAsync("GBP", "USD", start, end);

        Assert.AreEqual(2, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that adjacent loads are merged independently of insertion order.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenAdjacentRangesLoadedInReverseOrder_ShouldNotReloadTheirUnion()
    {
        (BoeRateProvider provider, FixtureBoeRateTableSource source) = Create(allowSync: false);
        DateOnly start = new(2023, 2, 1);
        DateOnly end = new(2023, 2, 28);

        await provider.LoadRangeAsync(new DateOnly(2023, 2, 16), end);
        await provider.LoadRangeAsync(start, new DateOnly(2023, 2, 15));
        await provider.LoadRangeAsync(start, end);

        Assert.AreEqual(2, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that partially overlapping loads also cover their combined window without another download.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenOverlappingRangesLoaded_ShouldNotReloadTheirUnion()
    {
        (BoeRateProvider provider, FixtureBoeRateTableSource source) = Create(allowSync: false);
        DateOnly start = new(2023, 3, 1);
        DateOnly end = new(2023, 3, 31);

        await provider.LoadRangeAsync(start, new DateOnly(2023, 3, 20));
        await provider.LoadRangeAsync(new DateOnly(2023, 3, 15), end);
        await provider.LoadRangeAsync(start, end);

        Assert.AreEqual(2, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that two ranges separated by an unloaded day do not incorrectly claim complete coverage.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenLoadedRangesHaveGap_ShouldFetchUncoveredUnion()
    {
        (BoeRateProvider provider, FixtureBoeRateTableSource source) = Create(allowSync: false);
        DateOnly start = new(2023, 4, 1);
        DateOnly end = new(2023, 4, 30);

        await provider.LoadRangeAsync(start, new DateOnly(2023, 4, 14));
        await provider.LoadRangeAsync(new DateOnly(2023, 4, 16), end);
        await provider.LoadRangeAsync(start, end);
        await provider.LoadRangeAsync(start, end);

        Assert.AreEqual(3, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that loading precisely the missing dates bridges two disjoint ranges, after which the full window is
    /// already covered and does not require a fourth download.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenGapSubsequentlyLoaded_ShouldNotReloadCompleteUnion()
    {
        (BoeRateProvider provider, FixtureBoeRateTableSource source) = Create(allowSync: false);
        DateOnly start = new(2023, 5, 1);
        DateOnly end = new(2023, 5, 31);

        await provider.LoadRangeAsync(start, new DateOnly(2023, 5, 10));
        await provider.LoadRangeAsync(new DateOnly(2023, 5, 13), end);
        await provider.LoadRangeAsync(new DateOnly(2023, 5, 11), new DateOnly(2023, 5, 12));
        await provider.LoadRangeAsync(start, end);

        Assert.AreEqual(3, source.GetTableCallCount);
    }
}
