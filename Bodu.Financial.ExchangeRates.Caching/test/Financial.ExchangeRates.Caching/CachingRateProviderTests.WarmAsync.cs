// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.WarmAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// Verifies that warming a set of pairs populates the cache so later range lookups of the window are hits with no
    /// further inner fetches, and that the warmed-pair count is returned.
    /// </summary>
    [TestMethod]
    public async Task WarmAsync_WhenPairsWarmed_ShouldServeLaterLookupsFromCache()
    {
        var start = new DateOnly(2023, 1, 3);
        var end = new DateOnly(2023, 1, 5);
        CountingDatedRateProvider inner = InnerWith(
            ("AUD", "USD", start, 0.5m),
            ("EUR", "USD", start, 1.1m));
        CachingRateProvider sut = CreateDecorator(inner);

        int warmed = await sut.WarmAsync(new[] { ("AUD", "USD"), ("EUR", "USD") }, start, end);
        int fetchesAfterWarm = inner.GetRatesAsyncCallCount;
        RateRangeResult audUsd = sut.GetRates("AUD", "USD", start, end);
        RateRangeResult eurUsd = await sut.GetRatesAsync("EUR", "USD", start, end);

        Assert.AreEqual(2, warmed, "both pairs must report warmed");
        Assert.AreEqual(2, fetchesAfterWarm, "the warm-up must fetch each pair's window once");
        Assert.AreEqual(fetchesAfterWarm, inner.GetRatesAsyncCallCount, "later lookups of the warmed window must not refetch");
        Assert.HasCount(1, audUsd.Rates);
        Assert.HasCount(1, eurUsd.Rates);
    }

    /// <summary>
    /// Verifies that warming an already covered pair costs no inner fetch and still counts the pair as warmed.
    /// </summary>
    [TestMethod]
    public async Task WarmAsync_WhenWindowAlreadyCovered_ShouldNotRefetch()
    {
        var start = new DateOnly(2023, 1, 3);
        var end = new DateOnly(2023, 1, 4);
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", start, 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);
        var pair = new CurrencyPair(Currencies.CurrencyCode.AUD, Currencies.CurrencyCode.USD);
        SeedCache(pair, (start, 0.5m));
        SeedCoverage(pair, start, end);

        int warmed = await sut.WarmAsync(new[] { ("AUD", "USD") }, start, end);

        Assert.AreEqual(1, warmed);
        Assert.AreEqual(0, inner.GetRatesAsyncCallCount, "a covered pair must warm from the cache alone");
    }

    /// <summary>
    /// Verifies that one failing pair is swallowed and skipped while the remaining pairs still warm, with the failure
    /// excluded from the returned count.
    /// </summary>
    [TestMethod]
    public async Task WarmAsync_WhenOnePairFails_ShouldWarmRemainingPairs()
    {
        var start = new DateOnly(2023, 1, 3);
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", start, 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);

        // "XX" is not a parseable ISO 4217 code, so that pair's fetch throws and must be swallowed.
        int warmed = await sut.WarmAsync(new[] { ("XX", "USD"), ("AUD", "USD") }, start, start);

        Assert.AreEqual(1, warmed, "only the healthy pair may count as warmed");
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount, "the healthy pair must still have been fetched");
        Assert.HasCount(1, sut.GetRates("AUD", "USD", start, start).Rates, "the healthy pair's window must be cached");
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> pair sequence is rejected.
    /// </summary>
    [TestMethod]
    public async Task WarmAsync_WhenPairsIsNull_ShouldThrowArgumentNullException()
    {
        CachingRateProvider sut = CreateDecorator(InnerWith());

        ArgumentNullException ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await sut.WarmAsync(null!, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));
        });

        Assert.AreEqual("pairs", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an inverted date window is rejected with the end-date parameter name, matching the range lookup
    /// surfaces.
    /// </summary>
    [TestMethod]
    public async Task WarmAsync_WhenEndDatePrecedesStartDate_ShouldThrowArgumentException()
    {
        CachingRateProvider sut = CreateDecorator(InnerWith());

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await sut.WarmAsync(new[] { ("AUD", "USD") }, new DateOnly(2023, 1, 4), new DateOnly(2023, 1, 3));
        });

        Assert.AreEqual("endDate", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a disposed provider rejects warm-up requests.
    /// </summary>
    [TestMethod]
    public async Task WarmAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        CachingRateProvider sut = CreateDecorator(InnerWith());
        sut.Dispose();

        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
        {
            _ = await sut.WarmAsync(new[] { ("AUD", "USD") }, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));
        });
    }

    /// <summary>
    /// Verifies that a pre-cancelled token aborts the warm-up with a cancellation exception rather than being
    /// swallowed as failed pairs.
    /// </summary>
    [TestMethod]
    public async Task WarmAsync_WhenTokenAlreadyCancelled_ShouldThrowTaskCanceledException()
    {
        CachingRateProvider sut = CreateDecorator(InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m)));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            _ = await sut.WarmAsync(new[] { ("AUD", "USD") }, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4), cts.Token);
        });
    }

    /// <summary>
    /// Verifies that an empty pair sequence warms nothing and returns zero without touching the inner provider.
    /// </summary>
    [TestMethod]
    public async Task WarmAsync_WhenPairsIsEmpty_ShouldReturnZero()
    {
        CountingDatedRateProvider inner = InnerWith();
        CachingRateProvider sut = CreateDecorator(inner);

        int warmed = await sut.WarmAsync(Array.Empty<(string, string)>(), new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));

        Assert.AreEqual(0, warmed);
        Assert.AreEqual(0, inner.GetRatesAsyncCallCount);
    }
}
