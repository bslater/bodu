// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryRateCacheZeroCopyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the all-fresh zero-copy read path of <see cref="InMemoryRateCache" />: a fully fresh stored list is served
/// by reference, is immutable to callers, and is never changed by later writes; a partially stale list still serves a
/// filtered copy.
/// </summary>
[TestClass]
public sealed class InMemoryRateCacheZeroCopyTests
{
    /// <summary>The fixed evaluation instant.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The caching duration applied by every test.</summary>
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>The pair under test.</summary>
    private static readonly CurrencyPair Pair = new(CurrencyCode.USD, CurrencyCode.AUD);

    /// <summary>
    /// Builds a fresh cached row for a June 2026 date.
    /// </summary>
    /// <param name="day">The day-of-month the row observes.</param>
    /// <param name="ageHours">The row's age in hours relative to <see cref="Now" />.</param>
    /// <returns>The cached row.</returns>
    private static CachedRate Row(int day, int ageHours = 1) =>
        new(new DateOnly(2026, 6, day), 1.5m, Now - TimeSpan.FromHours(ageHours));

    /// <summary>
    /// Verifies that an all-fresh read cannot be cast back to a mutable array or list, so a caller cannot corrupt
    /// cache state through the shared reference.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenAllFresh_ShouldNotExposeMutableBackingCollection()
    {
        var cache = new InMemoryRateCache("test");
        cache.Store(Pair, new[] { Row(1), Row(2) }, Duration, Now);

        IReadOnlyList<CachedRate> served = cache.GetRates(Pair, Duration, Now);

        Assert.IsNotInstanceOfType<CachedRate[]>(served, "the served list must not be the raw backing array");
        Assert.IsNotInstanceOfType<List<CachedRate>>(served, "the served list must not be a mutable list");
    }

    /// <summary>
    /// Verifies that a previously served all-fresh list is unchanged by a subsequent store, because every write
    /// installs a fresh snapshot.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredAfterServe_ShouldNotChangeThePreviouslyServedList()
    {
        var cache = new InMemoryRateCache("test");
        cache.Store(Pair, new[] { Row(1) }, Duration, Now);

        IReadOnlyList<CachedRate> served = cache.GetRates(Pair, Duration, Now);
        cache.Store(Pair, new[] { Row(2) }, Duration, Now);

        Assert.HasCount(1, served, "the earlier serve must be an immutable snapshot");
        Assert.AreEqual(new DateOnly(2026, 6, 1), served[0].Date);
    }

    /// <summary>
    /// Verifies that the all-fresh serve is element-wise identical to the filtered copy the rules would build.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenAllFresh_ShouldEqualSelectFreshOutput()
    {
        var cache = new InMemoryRateCache("test");
        cache.Store(Pair, new[] { Row(1), Row(2), Row(3) }, Duration, Now);

        IReadOnlyList<CachedRate> served = cache.GetRates(Pair, Duration, Now);
        List<CachedRate> filtered = RateCacheRules.SelectFresh(served, Duration, Now);

        CollectionAssert.AreEqual(filtered, served.ToList());
    }

    /// <summary>
    /// Verifies that a store containing a row that later goes stale still serves only the fresh subset, via the
    /// filtered-copy path.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenSomeRowsStale_ShouldServeFilteredCopy()
    {
        var cache = new InMemoryRateCache("test");
        cache.Store(Pair, new[] { Row(1, ageHours: 20), Row(2, ageHours: 1) }, Duration, Now);

        // Four hours later the first row (now 24h old) is stale under the strict less-than freshness rule.
        DateTimeOffset later = Now.AddHours(4);
        IReadOnlyList<CachedRate> served = cache.GetRates(Pair, Duration, later);

        Assert.HasCount(1, served);
        Assert.AreEqual(new DateOnly(2026, 6, 2), served[0].Date);
    }
}
