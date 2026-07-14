// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.ServedInstants.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Pins the served-instant semantics of a single-date cache hit: the provenance cache-write instant and the restored
/// upstream fetch instant come from the row that actually resolved the lookup — exact, nearest, or inverse — with the
/// oldest-candidate fallback reserved for the same-currency identity serve.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>The AUD/USD pair used by the served-instant tests.</summary>
    private static readonly CurrencyPair ServedPair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>An inner provider that must never be consulted; every test serves from the seeded cache.</summary>
    private static CountingDatedRateProvider EmptyInner() => InnerWith();

    /// <summary>
    /// Seeds the cache with rows carrying explicit per-row cache-write and upstream fetch instants.
    /// </summary>
    /// <param name="pair">The pair to seed.</param>
    /// <param name="rows">The rows, each with its own instants.</param>
    private void SeedStamped(CurrencyPair pair, params (DateOnly Date, decimal Rate, DateTimeOffset CachedAt, DateTimeOffset? ObservedAt)[] rows) =>
        _cache.Store(
            pair,
            rows.Select(r => new CachedRate(r.Date, r.Rate, r.CachedAt, r.ObservedAt)).ToArray(),
            Duration,
            _clock.GetUtcNow());

    /// <summary>
    /// Verifies that an exact-date hit reports the matched row's cache-write instant in the provenance and restores
    /// that row's upstream fetch instant onto the served rate.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenExactDateHit_ShouldServeMatchedRowInstants()
    {
        DateTimeOffset cachedA = Now.AddHours(-3);
        DateTimeOffset cachedB = Now.AddHours(-1);
        DateTimeOffset observedB = Now.AddHours(-2);
        SeedStamped(ServedPair,
            (new DateOnly(2023, 1, 3), 0.50m, cachedA, Now.AddHours(-4)),
            (new DateOnly(2023, 1, 6), 0.51m, cachedB, observedB));
        CachingRateProvider sut = CreateDecorator(EmptyInner());

        RateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 6), RateLookupOptions.Exact);

        Assert.AreEqual(cachedB, result.Provenance.CachedAtUtc);
        Assert.AreEqual(observedB, result.Rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that a nearest-date serve reports the resolved (nearest) row's instants, not the requested date's or
    /// any other row's.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenNearestDateServe_ShouldServeResolvedRowInstants()
    {
        DateTimeOffset cachedNear = Now.AddHours(-1);
        DateTimeOffset observedNear = Now.AddHours(-5);
        SeedStamped(ServedPair,
            (new DateOnly(2023, 1, 3), 0.50m, Now.AddHours(-6), Now.AddHours(-7)),
            (new DateOnly(2023, 1, 6), 0.51m, cachedNear, observedNear));
        CachingRateProvider sut = CreateDecorator(EmptyInner());

        var nearest = new RateLookupOptions(RateDateResolution.Nearest, toleranceDays: 2);
        RateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 7), nearest);

        Assert.AreEqual(new DateOnly(2023, 1, 6), result.Rate.Date);
        Assert.AreEqual(cachedNear, result.Provenance.CachedAtUtc);
        Assert.AreEqual(observedNear, result.Rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that a serve satisfied from the inverse pair's rows reports the inverse row's instants.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenInverseServe_ShouldServeInverseRowInstants()
    {
        DateTimeOffset cachedInverse = Now.AddHours(-2);
        DateTimeOffset observedInverse = Now.AddHours(-3);
        SeedStamped(ServedPair.Inverse(), (new DateOnly(2023, 1, 6), 2.0m, cachedInverse, observedInverse));
        CachingRateProvider sut = CreateDecorator(EmptyInner());

        RateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 6), RateLookupOptions.Exact);

        Assert.AreEqual(cachedInverse, result.Provenance.CachedAtUtc);
        Assert.AreEqual(observedInverse, result.Rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies the identity serve's legacy fallback: with same-currency rows cached at other dates, the synthetic
    /// identity rate reports the oldest candidate row's instants, because no row matches the resolved date.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenIdentityServeWithUnrelatedRows_ShouldReportOldestCandidateInstants()
    {
        var audAud = new CurrencyPair(CurrencyCode.AUD, CurrencyCode.AUD);
        DateTimeOffset oldestCached = Now.AddHours(-9);
        DateTimeOffset oldestObserved = Now.AddHours(-10);
        SeedStamped(audAud,
            (new DateOnly(2023, 1, 2), 1m, oldestCached, oldestObserved),
            (new DateOnly(2023, 1, 3), 1m, Now.AddHours(-1), Now.AddHours(-2)));
        CachingRateProvider sut = CreateDecorator(EmptyInner());

        RateLookupResult result = sut.GetRate("AUD", "AUD", new DateOnly(2023, 1, 9), RateLookupOptions.Exact);

        Assert.AreEqual(1m, result.Rate.Rate);
        Assert.AreEqual(oldestCached, result.Provenance.CachedAtUtc);
        Assert.AreEqual(oldestObserved, result.Rate.FetchedAtUtc);
    }
}
