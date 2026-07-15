// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceTests.Warm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

public sealed partial class CachingNotableDateServiceTests
{
    /// <summary>
    /// Verifies that warming a territory over a span of years populates the cache so every later in-span resolve is a
    /// hit with no further inner computations.
    /// </summary>
    [TestMethod]
    public void Warm_WhenSpanWarmed_ShouldServeLaterResolvesFromCache()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out CountingNotableDateService inner, out _);

        int warmed = service.Warm(new[] { "US" }, 2026, 2028);
        int computesAfterWarm = inner.ResolveCount;
        _ = service.Resolve(new DateOnly(2026, 6, 15), "US");
        _ = service.Resolve(new DateOnly(2027, 1, 1), "US");
        _ = service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2028, 12, 31)), "US");

        Assert.AreEqual(1, warmed, "the territory must report warmed");
        Assert.AreEqual(3, computesAfterWarm, "the warm-up must compute each cold year once");
        Assert.AreEqual(computesAfterWarm, inner.ResolveCount, "later in-span resolves must not recompute");
    }

    /// <summary>
    /// Verifies that warming several territories returns the count of territories warmed.
    /// </summary>
    [TestMethod]
    public void Warm_WhenSeveralTerritoriesWarmed_ShouldReturnTerritoryCount()
    {
        var time = new MutableTimeProvider(Now);
        CachingNotableDateService service = BuildService(time, TimeSpan.FromDays(30), out CountingNotableDateService inner, out _);

        int warmed = service.Warm(new[] { "US", "CA", "AU" }, 2026, 2026);

        Assert.AreEqual(3, warmed);
        Assert.AreEqual(3, inner.ResolveCount, "each territory's year must have been computed once");
    }

    /// <summary>
    /// Verifies that one failing territory is swallowed and skipped while the remaining territories still warm, with
    /// the failure excluded from the returned count.
    /// </summary>
    [TestMethod]
    public void Warm_WhenOneTerritoryFails_ShouldWarmRemainingTerritories()
    {
        var time = new MutableTimeProvider(Now);
        var gated = new GatedNotableDateService(throwOnFirstCall: true);
        var service = new CachingNotableDateService(
            gated,
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "fixed" },
            timeProvider: time);
        gated.Open();

        int warmed = service.Warm(new[] { "XX", "US" }, 2026, 2026);

        Assert.AreEqual(1, warmed, "only the healthy territory may count as warmed");
        Assert.AreEqual(2, gated.ResolveCount, "the healthy territory must still have been computed");
        Assert.HasCount(1, service.Resolve(new DateOnly(2026, 1, 1), "US"), "the healthy territory's year must be cached");
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> territory sequence is rejected.
    /// </summary>
    [TestMethod]
    public void Warm_WhenTerritoriesIsNull_ShouldThrowArgumentNullException()
    {
        CachingNotableDateService service = BuildService(new MutableTimeProvider(Now), TimeSpan.FromDays(30), out _, out _);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = service.Warm(null!, 2026, 2026);
        });

        Assert.AreEqual("territories", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an inverted year span is rejected with the last-year parameter name.
    /// </summary>
    [TestMethod]
    public void Warm_WhenLastYearPrecedesFirstYear_ShouldThrowArgumentException()
    {
        CachingNotableDateService service = BuildService(new MutableTimeProvider(Now), TimeSpan.FromDays(30), out _, out _);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = service.Warm(new[] { "US" }, 2027, 2026);
        });

        Assert.AreEqual("lastYear", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a year outside the range representable by <see cref="DateOnly" /> is rejected.
    /// </summary>
    [TestMethod]
    public void Warm_WhenFirstYearOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        CachingNotableDateService service = BuildService(new MutableTimeProvider(Now), TimeSpan.FromDays(30), out _, out _);

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = service.Warm(new[] { "US" }, 0, 2026);
        });

        Assert.AreEqual("firstYear", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty territory sequence warms nothing and returns zero without touching the inner service.
    /// </summary>
    [TestMethod]
    public void Warm_WhenTerritoriesIsEmpty_ShouldReturnZero()
    {
        CachingNotableDateService service = BuildService(new MutableTimeProvider(Now), TimeSpan.FromDays(30), out CountingNotableDateService inner, out _);

        int warmed = service.Warm(Array.Empty<string>(), 2026, 2026);

        Assert.AreEqual(0, warmed);
        Assert.AreEqual(0, inner.ResolveCount);
    }
}
