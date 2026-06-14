// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingDatedExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Caching.Contracts;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the caching behaviour of <see cref="CachingDatedExchangeRateProvider" /> over counting inner sources and an
/// in-memory cache with a controllable clock.
/// </summary>
[TestClass]
public sealed partial class CachingDatedExchangeRateProviderTests
{
    /// <summary>
    /// The default source name used by the single-source tests.
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
    /// The cache shared by the decorator under test.
    /// </summary>
    private InMemoryExchangeRateCache _cache = null!;

    /// <summary>
    /// The controllable clock driving freshness and expiry.
    /// </summary>
    private MutableTimeProvider _clock = null!;

    /// <summary>
    /// The options carrying the default and per-source expiry policy.
    /// </summary>
    private CachingExchangeRateOptions _options = null!;

    /// <summary>
    /// Creates a fresh cache, clock, and options before each test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _cache = new InMemoryExchangeRateCache();
        _clock = new MutableTimeProvider(Now);
        _options = new CachingExchangeRateOptions { DefaultExpiry = Duration };
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> sources collection is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSourcesIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CachingDatedExchangeRateProvider(null!, _cache, _options, _clock);
        });

        Assert.AreEqual("sources", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> cache is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCacheIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CachingDatedExchangeRateProvider(new[] { Source(Provider, InnerWith()) }, null!, _options, _clock);
        });

        Assert.AreEqual("cache", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> options are rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CachingDatedExchangeRateProvider(new[] { Source(Provider, InnerWith()) }, _cache, null!, _clock);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty sources collection is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSourcesIsEmpty_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new CachingDatedExchangeRateProvider(Array.Empty<KeyValuePair<string, IDatedExchangeRateProvider>>(), _cache, _options, _clock);
        });

        Assert.AreEqual("sources", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank source name is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSourceNameIsBlank_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new CachingDatedExchangeRateProvider(new[] { Source("  ", InnerWith()) }, _cache, _options, _clock);
        });

        Assert.AreEqual("sources", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a duplicate source name is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSourceNameIsDuplicated_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new CachingDatedExchangeRateProvider(
                new[] { Source("Dup", InnerWith()), Source("Dup", InnerWith()) },
                _cache,
                _options,
                _clock);
        });

        Assert.AreEqual("sources", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a non-positive default expiry is rejected through option validation.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultExpiryNotPositive_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new CachingDatedExchangeRateProvider(
                new[] { Source(Provider, InnerWith()) },
                _cache,
                new CachingExchangeRateOptions { DefaultExpiry = TimeSpan.Zero },
                _clock);
        });

        Assert.AreEqual("DefaultExpiry", ex.ParamName);
    }

    /// <summary>
    /// Creates a decorator over a single source named <see cref="Provider" />.
    /// </summary>
    /// <param name="inner">The inner source to wrap.</param>
    /// <returns>A new decorator instance.</returns>
    private CachingDatedExchangeRateProvider CreateDecorator(IDatedExchangeRateProvider inner) =>
        new(new[] { Source(Provider, inner) }, _cache, _options, _clock);

    /// <summary>
    /// Creates a decorator over the supplied named sources.
    /// </summary>
    /// <param name="sources">The named sources, in priority order.</param>
    /// <returns>A new decorator instance.</returns>
    private CachingDatedExchangeRateProvider CreateDecorator(params KeyValuePair<string, IDatedExchangeRateProvider>[] sources) =>
        new(sources, _cache, _options, _clock);

    /// <summary>
    /// Builds a named source entry.
    /// </summary>
    /// <param name="name">The source name.</param>
    /// <param name="provider">The source provider.</param>
    /// <returns>The named source entry.</returns>
    private static KeyValuePair<string, IDatedExchangeRateProvider> Source(string name, IDatedExchangeRateProvider provider) =>
        new(name, provider);

    /// <summary>
    /// Builds a counting inner source from the supplied observation rows.
    /// </summary>
    /// <param name="rows">The observation rows, each a from/to/date/rate tuple.</param>
    /// <returns>A new counting inner source.</returns>
    private static CountingDatedExchangeRateProvider InnerWith(params (string From, string To, DateOnly Date, decimal Rate)[] rows) =>
        new(rows.Select(static r => new ExchangeRate(r.From, r.To, r.Date, r.Rate, "Test")));

    /// <summary>
    /// Seeds the cache with fresh rows for the supplied pair under <see cref="Provider" />, stamped at the current clock
    /// instant.
    /// </summary>
    /// <param name="pair">The pair to seed.</param>
    /// <param name="rows">The date/rate rows to store.</param>
    private void SeedCache(ExchangeRatePair pair, params (DateOnly Date, decimal Rate)[] rows) =>
        _cache.Store(
            Provider,
            pair,
            rows.Select(r => new CachedExchangeRate(r.Date, r.Rate, _clock.GetUtcNow())).ToArray(),
            Duration,
            _clock.GetUtcNow());
}
