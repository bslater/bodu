// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the caching behaviour of <see cref="CachingExchangeRateProvider" /> over a counting inner source and an
/// in-memory cache with a controllable clock.
/// </summary>
[TestClass]
public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// The provider name the cache and decorator are bound to.
    /// </summary>
    private const string Provider = "Test";

    /// <summary>
    /// The fixed evaluation instant used by the tests.
    /// </summary>
    private static readonly DateTimeOffset Now = new(2023, 1, 10, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The default caching duration used by the tests.
    /// </summary>
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>
    /// The single-provider cache shared by the decorator under test.
    /// </summary>
    private InMemoryExchangeRateCache _cache = null!;

    /// <summary>
    /// The controllable clock driving freshness and expiry.
    /// </summary>
    private MutableTimeProvider _clock = null!;

    /// <summary>
    /// The options carrying the default and per-provider expiry policy.
    /// </summary>
    private CachingExchangeRateOptions _options = null!;

    /// <summary>
    /// Creates a fresh cache, clock, and options before each test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _cache = new InMemoryExchangeRateCache(Provider);
        _clock = new MutableTimeProvider(Now);
        _options = new CachingExchangeRateOptions { DefaultExpiry = Duration };
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> inner provider is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenInnerIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CachingExchangeRateProvider(null!, _cache, _options, _clock);
        });

        Assert.AreEqual("inner", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> cache is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCacheIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CachingExchangeRateProvider(InnerWith(), null!, _options, _clock);
        });

        Assert.AreEqual("cache", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> options are rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CachingExchangeRateProvider(InnerWith(), _cache, null!, _clock);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a non-positive default expiry is rejected through option validation.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultExpiryNotPositive_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new CachingExchangeRateProvider(InnerWith(), _cache, new CachingExchangeRateOptions { DefaultExpiry = TimeSpan.Zero }, _clock);
        });

        Assert.AreEqual("DefaultExpiry", ex.ParamName);
    }

    /// <summary>
    /// Creates a decorator over the supplied inner source and the shared in-memory cache.
    /// </summary>
    /// <param name="inner">The inner source to wrap.</param>
    /// <returns>A new decorator instance.</returns>
    private CachingExchangeRateProvider CreateDecorator(IDatedExchangeRateProvider inner) =>
        new(inner, _cache, _options, _clock);

    /// <summary>
    /// Builds a counting inner source from the supplied observation rows.
    /// </summary>
    /// <param name="rows">The observation rows, each a from/to/date/rate tuple.</param>
    /// <returns>A new counting inner source.</returns>
    private static CountingDatedExchangeRateProvider InnerWith(params (string From, string To, DateOnly Date, decimal Rate)[] rows) =>
        new(rows.Select(static r => new ExchangeRate(CurrencyInfo.ParseCurrencyCode(r.From), CurrencyInfo.ParseCurrencyCode(r.To), r.Date, r.Rate, "Test")));

    /// <summary>
    /// Seeds the cache with fresh rows for the supplied pair, stamped at the current clock instant.
    /// </summary>
    /// <param name="pair">The pair to seed.</param>
    /// <param name="rows">The date/rate rows to store.</param>
    private void SeedCache(ExchangeRatePair pair, params (DateOnly Date, decimal Rate)[] rows) =>
        _cache.Store(
            pair,
            rows.Select(r => new CachedExchangeRate(r.Date, r.Rate, _clock.GetUtcNow())).ToArray(),
            Duration,
            _clock.GetUtcNow());

    /// <summary>
    /// Records a fresh coverage window for the supplied pair, stamped at the current clock instant, so a range lookup of
    /// that window is treated as cached.
    /// </summary>
    /// <param name="pair">The pair to record coverage for.</param>
    /// <param name="start">The inclusive first date of the covered window.</param>
    /// <param name="end">The inclusive last date of the covered window.</param>
    private void SeedCoverage(ExchangeRatePair pair, DateOnly start, DateOnly end) =>
        _cache.RecordCoverage(pair, start, end, Duration, _clock.GetUtcNow());
}
