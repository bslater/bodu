// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the shared caching mechanism for a provider that wraps one or more named
/// <see cref="IDatedExchangeRateProvider" /> sources: it serves fresh rates from an <see cref="IExchangeRateCache" />
/// and delegates to a source only on a miss, caching what the source returns. Derived types supply the wrapped sources.
/// </summary>
/// <remarks>
/// <para>
/// The provider implements the same <see cref="IDatedExchangeRateProvider" /> contract the caller resolves, so it can
/// be inserted transparently. Each source has its own caching duration, taken from
/// <see cref="CachingExchangeRateOptions.ProviderExpiry" /> when present and otherwise
/// <see cref="CachingExchangeRateOptions.DefaultExpiry" />.
/// </para>
/// <para>
/// Sources are consulted in the order returned by <see cref="Sources" />; the first to satisfy a request wins.
/// Single-date lookups serve per-row fresh observations and cache the resolved row on a miss. Range lookups serve from
/// the cache only when the cached fresh rows for the source span the requested window; otherwise the whole range is
/// refetched from the source and re-cached.
/// </para>
/// </remarks>
public abstract class CachingExchangeRateProviderBase
    : IDatedExchangeRateProvider
{
    /// <summary>
    /// The cache that serves fresh rates and stores resolved observations.
    /// </summary>
    private readonly IExchangeRateCache _cache;

    /// <summary>
    /// The options carrying the per-source and default caching durations.
    /// </summary>
    private readonly CachingExchangeRateOptions _options;

    /// <summary>
    /// The time source used to evaluate freshness and stamp newly cached rows.
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// The logger that records cache hits, misses, and refetches.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingExchangeRateProviderBase" /> class.
    /// </summary>
    /// <param name="cache">The cache that serves fresh rates and stores resolved observations.</param>
    /// <param name="options">The options carrying the caching durations.</param>
    /// <param name="timeProvider">
    /// The time source used to evaluate freshness and stamp newly cached rows. <see langword="null" /> selects
    /// <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">
    /// The logger that records cache hits, misses, and refetches. <see langword="null" /> selects
    /// <see cref="NullLogger.Instance" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected CachingExchangeRateProviderBase(IExchangeRateCache cache, CachingExchangeRateOptions options, TimeProvider? timeProvider, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(cache);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _cache = cache;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Gets the wrapped sources, in priority order, paired with the name each is cached under.
    /// </summary>
    /// <returns>The wrapped named sources.</returns>
    protected abstract IReadOnlyList<KeyValuePair<string, IDatedExchangeRateProvider>> Sources { get; }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.IO_KeyNotFound_ExchangeRate, fromIsoCode, toIsoCode, date));
    }

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result)
    {
        var now = _timeProvider.GetUtcNow();

        foreach (KeyValuePair<string, IDatedExchangeRateProvider> source in Sources)
        {
            var duration = _options.GetExpiry(source.Key);

            if (TryServeFromCache(source.Key, duration, fromIsoCode, toIsoCode, date, options, now, out result))
            {
                Log.CacheHit(_logger, _options.CacheHitLogLevel, source.Key, fromIsoCode, toIsoCode, date);
                return true;
            }

            if (source.Value.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
            {
                StoreResult(source.Key, duration, fromIsoCode, toIsoCode, result, now);
                Log.CacheMissStored(_logger, _options.CacheMissLogLevel, source.Key, fromIsoCode, toIsoCode, date);
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ExchangeRate>> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_RangeInverted, nameof(endDate));

        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);
        var now = _timeProvider.GetUtcNow();

        foreach (KeyValuePair<string, IDatedExchangeRateProvider> source in Sources)
        {
            var duration = _options.GetExpiry(source.Key);

            if (TryServeRangeFromCache(source.Key, duration, pair, startDate, endDate, now, out IReadOnlyList<ExchangeRate> cached))
            {
                Log.RangeCacheHit(_logger, _options.CacheRangeHitLogLevel, source.Key, fromIsoCode, toIsoCode);
                return cached;
            }

            IReadOnlyList<ExchangeRate> fetched =
                await source.Value.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken).ConfigureAwait(false);

            if (fetched.Count > 0)
            {
                StoreRange(source.Key, duration, pair, fetched, now);
                Log.RangeRefetched(_logger, _options.CacheRangeRefetchLogLevel, source.Key, fromIsoCode, toIsoCode, fetched.Count);
                return fetched;
            }
        }

        return Array.Empty<ExchangeRate>();
    }

    /// <summary>
    /// Attempts to resolve a single-date request from the fresh cached rows for a source (and its inverse, when
    /// inversion is permitted) by delegating to a <see cref="FixedDatedExchangeRateProvider" /> built from them.
    /// </summary>
    /// <param name="provider">The source name the rows are cached under.</param>
    /// <param name="duration">The duration cached rows for the source stay fresh.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="now">The instant against which cached rows are evaluated for freshness.</param>
    /// <param name="result">When this method returns <see langword="true" />, the resolved result.</param>
    /// <returns>
    /// <see langword="true" /> when the request was satisfied from the cache; otherwise <see langword="false" />.
    /// </returns>
    private bool TryServeFromCache(string provider, TimeSpan duration, string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, DateTimeOffset now, out ExchangeRateLookupResult result)
    {
        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);

        IReadOnlyList<CachedExchangeRate> direct = _cache.GetRates(provider, pair, duration, now);

        var allowInverse = options?.AllowInverse ?? ExchangeRateLookupOptions.Exact.AllowInverse;
        IReadOnlyList<CachedExchangeRate> inverse = allowInverse
            ? _cache.GetRates(provider, pair.Inverse(), duration, now)
            : Array.Empty<CachedExchangeRate>();

        if (direct.Count == 0 && inverse.Count == 0)
        {
            result = default;
            return false;
        }

        List<ExchangeRate> rates = new(direct.Count + inverse.Count);
        foreach (CachedExchangeRate rate in direct)
            rates.Add(new ExchangeRate(fromIsoCode, toIsoCode, rate.Date, rate.Rate, provider));
        foreach (CachedExchangeRate rate in inverse)
            rates.Add(new ExchangeRate(toIsoCode, fromIsoCode, rate.Date, rate.Rate, provider));

        FixedDatedExchangeRateProvider snapshot = new(rates);
        return snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);
    }

    /// <summary>
    /// Attempts to serve a range request from the cache, treating the source's fresh cached rows as covering the
    /// request only when their earliest and latest dates span the requested window.
    /// </summary>
    /// <param name="provider">The source name the rows are cached under.</param>
    /// <param name="duration">The duration cached rows for the source stay fresh.</param>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="now">The instant against which cached rows are evaluated for freshness.</param>
    /// <param name="result">When this method returns <see langword="true" />, the rates within the range.</param>
    /// <returns>
    /// <see langword="true" /> when the range was satisfied from the cache; otherwise <see langword="false" />.
    /// </returns>
    private bool TryServeRangeFromCache(string provider, TimeSpan duration, ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, DateTimeOffset now, out IReadOnlyList<ExchangeRate> result)
    {
        IReadOnlyList<CachedExchangeRate> fresh = _cache.GetRates(provider, pair, duration, now);

        // GetRates returns rows ordered ascending by date; treat the range as cached only when the fresh rows span it.
        if (fresh.Count == 0 || fresh[0].Date > startDate || fresh[^1].Date < endDate)
        {
            result = Array.Empty<ExchangeRate>();
            return false;
        }

        List<ExchangeRate> rates = new();
        foreach (CachedExchangeRate rate in fresh)
        {
            if (rate.Date >= startDate && rate.Date <= endDate)
                rates.Add(new ExchangeRate(pair.FromIsoCode, pair.ToIsoCode, rate.Date, rate.Rate, provider));
        }

        result = rates;
        return true;
    }

    /// <summary>
    /// Stores the resolved single-date observation for the requested pair under a source.
    /// </summary>
    /// <param name="provider">The source name to cache the rate under.</param>
    /// <param name="duration">The duration cached rows for the source stay fresh.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="result">The resolved lookup result returned by the wrapped source.</param>
    /// <param name="now">The instant to stamp the cached row with.</param>
    private void StoreResult(string provider, TimeSpan duration, string fromIsoCode, string toIsoCode, ExchangeRateLookupResult result, DateTimeOffset now)
    {
        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);
        CachedExchangeRate[] rows = { new(result.Rate.Date, result.Rate.Rate, now) };
        _cache.Store(provider, pair, rows, duration, now);
    }

    /// <summary>
    /// Stores a fetched range of observations for the requested pair under a source.
    /// </summary>
    /// <param name="provider">The source name to cache the rates under.</param>
    /// <param name="duration">The duration cached rows for the source stay fresh.</param>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="rates">The rates returned by the wrapped source.</param>
    /// <param name="now">The instant to stamp the cached rows with.</param>
    private void StoreRange(string provider, TimeSpan duration, ExchangeRatePair pair, IReadOnlyList<ExchangeRate> rates, DateTimeOffset now)
    {
        CachedExchangeRate[] rows = new CachedExchangeRate[rates.Count];
        for (var i = 0; i < rates.Count; i++)
            rows[i] = new CachedExchangeRate(rates[i].Date, rates[i].Rate, now);

        _cache.Store(provider, pair, rows, duration, now);
    }
}
