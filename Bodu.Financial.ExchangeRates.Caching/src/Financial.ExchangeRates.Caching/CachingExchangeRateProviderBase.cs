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
/// Provides the shared caching mechanism for a provider that wraps a single inner
/// <see cref="IDatedExchangeRateProvider" /> over a single-provider <see cref="IExchangeRateCache" />: it serves fresh
/// rates from the cache and delegates to the inner provider only on a miss, caching what the inner provider returns.
/// Derived types supply the wrapped inner provider.
/// </summary>
/// <remarks>
/// <para>
/// The provider implements the same <see cref="IDatedExchangeRateProvider" /> contract the caller resolves, so it can
/// be inserted transparently, and also the timeless <see cref="IExchangeRateProvider" /> surface, which resolves the
/// current UTC date under <see cref="CachingExchangeRateOptions.DefaultLookupOptions" />. The cache's
/// <see cref="IExchangeRateCache.Provider" /> identifies the source: it selects the caching duration from
/// <see cref="CachingExchangeRateOptions.ProviderExpiry" /> (falling back to
/// <see cref="CachingExchangeRateOptions.DefaultExpiry" />) and tags both cached rows and log messages.
/// </para>
/// <para>
/// Single-date lookups serve per-row fresh observations and cache the resolved row on a miss. Range lookups serve from
/// the cache only when its recorded coverage contains the whole requested window — that is, every day in the window was
/// actually fetched and is still fresh — so an interior day that was never fetched forces a refetch rather than being
/// served from a sparse set of rows. On a miss the whole range is refetched from the inner provider and written back
/// through a single atomic <see cref="IExchangeRateCache.StoreFetchedRange" /> that merges the rows and records the
/// covered window together, even when the fetch returned no rows, so an empty-but-fetched window is not refetched on
/// the next lookup. To group several sources behind one entry point, wrap each in its own caching provider and compose
/// them with an <see cref="AggregatingExchangeRateProvider" />.
/// </para>
/// <para>
/// Each of the four serve points additionally emits a provenance record alongside its hit/miss diagnostic, recording
/// whether the rate was resolved live or from the cache, the cache backend that served it, and — for a cache serve —
/// the age of the served data. The provenance event is logged at
/// <see cref="CachingExchangeRateOptions.RateProvenanceLogLevel" />.
/// </para>
/// </remarks>
public abstract class CachingExchangeRateProviderBase
    : IDatedExchangeRateProvider, IExchangeRateProvider
{
    /// <summary>
    /// The single-provider cache that serves fresh rates and stores resolved observations.
    /// </summary>
    private readonly IExchangeRateCache _cache;

    /// <summary>
    /// The options carrying the caching durations and the timeless-surface lookup options.
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
    /// The runtime identity of the cache backend, captured once at construction to avoid recomputing it on the serve
    /// path. It is reported as the backend of every provenance record this provider emits.
    /// </summary>
    private readonly string _backend;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingExchangeRateProviderBase" /> class.
    /// </summary>
    /// <param name="cache">The single-provider cache that serves fresh rates and stores resolved observations.</param>
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
        _backend = cache.GetType().Name;
    }

    /// <summary>
    /// Gets the inner provider consulted on a cache miss.
    /// </summary>
    /// <returns>The wrapped inner provider.</returns>
    protected abstract IDatedExchangeRateProvider Inner { get; }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, ExchangeRateLookupOptions? options = null)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRate(fromIsoCode, toIsoCode, today, options ?? _options.DefaultLookupOptions);
    }

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
        var duration = _options.GetExpiry(_cache.Provider);

        if (TryServeFromCache(duration, fromIsoCode, toIsoCode, date, options, now, out result, out DateTimeOffset? servedCachedAtUtc))
        {
            // The snapshot built from cached rows yields Live provenance; overwrite it with the cache lineage so the
            // returned result and the logged provenance describe the same serve and cannot diverge.
            result = result with { Provenance = ExchangeRateProvenance.FromCache(_cache.Provider, _backend, servedCachedAtUtc, now) };

            Log.CacheHit(_logger, _options.CacheHitLogLevel, _cache.Provider, fromIsoCode, toIsoCode, date);
            EmitProvenance(result.Provenance, fromIsoCode, toIsoCode);
            return true;
        }

        if (Inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
        {
            StoreResult(duration, fromIsoCode, toIsoCode, result, now);
            Log.CacheMissStored(_logger, _options.CacheMissLogLevel, _cache.Provider, fromIsoCode, toIsoCode, date);

            // A miss carries the inner provider's Live provenance through unchanged.
            EmitProvenance(result.Provenance, fromIsoCode, toIsoCode);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public ValueTask<ExchangeRateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        ExchangeRateLookupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRateAsync(fromIsoCode, toIsoCode, today, options ?? _options.DefaultLookupOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ExchangeRateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var duration = _options.GetExpiry(_cache.Provider);

        if (TryServeFromCache(duration, fromIsoCode, toIsoCode, date, options, now, out ExchangeRateLookupResult result, out DateTimeOffset? servedCachedAtUtc))
        {
            Log.CacheHit(_logger, _options.CacheHitLogLevel, _cache.Provider, fromIsoCode, toIsoCode, date);
            EmitProvenance(ExchangeRateProvenance.FromCache(_cache.Provider, _backend, servedCachedAtUtc, now), fromIsoCode, toIsoCode);
            return result;
        }

        result = await Inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken).ConfigureAwait(false);
        StoreResult(duration, fromIsoCode, toIsoCode, result, now);
        Log.CacheMissStored(_logger, _options.CacheMissLogLevel, _cache.Provider, fromIsoCode, toIsoCode, date);
        EmitProvenance(ExchangeRateProvenance.Live(_cache.Provider, _backend), fromIsoCode, toIsoCode);
        return result;
    }

    /// <inheritdoc />
    public ExchangeRateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_RangeInverted, nameof(endDate));

        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);
        var now = _timeProvider.GetUtcNow();
        var duration = _options.GetExpiry(_cache.Provider);

        if (TryServeRangeFromCache(duration, pair, startDate, endDate, now, out IReadOnlyList<ExchangeRate> cached, out DateTimeOffset? oldestCachedAtUtc))
        {
            Log.RangeCacheHit(_logger, _options.CacheRangeHitLogLevel, _cache.Provider, fromIsoCode, toIsoCode);
            EmitProvenance(ExchangeRateProvenance.FromCache(_cache.Provider, _backend, oldestCachedAtUtc, now), fromIsoCode, toIsoCode);
            return new ExchangeRateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, cached);
        }

        IReadOnlyList<ExchangeRate> fetched = [.. Inner.GetRates(fromIsoCode, toIsoCode, startDate, endDate)];

        // Write the fetched rows and the covered window atomically, regardless of how many rows came back: the request
        // asked for every interior day, so even an empty fetch must record the whole window as covered or the same range
        // would be refetched forever. A single atomic write rules out persisting coverage without its rows.
        ExchangeRateCacheWriteStatus status = StoreFetchedRange(duration, pair, fetched, startDate, endDate, now);

        Log.RangeRefetched(_logger, _options.CacheRangeRefetchLogLevel, _cache.Provider, fromIsoCode, toIsoCode, fetched.Count, status);
        EmitProvenance(ExchangeRateProvenance.Live(_cache.Provider, _backend), fromIsoCode, toIsoCode);
        return new ExchangeRateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, fetched);
    }

    /// <inheritdoc />
    public async ValueTask<ExchangeRateRangeResult> GetRatesAsync(
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
        var duration = _options.GetExpiry(_cache.Provider);

        if (TryServeRangeFromCache(duration, pair, startDate, endDate, now, out IReadOnlyList<ExchangeRate> cached, out DateTimeOffset? oldestCachedAtUtc))
        {
            Log.RangeCacheHit(_logger, _options.CacheRangeHitLogLevel, _cache.Provider, fromIsoCode, toIsoCode);
            EmitProvenance(ExchangeRateProvenance.FromCache(_cache.Provider, _backend, oldestCachedAtUtc, now), fromIsoCode, toIsoCode);
            return new ExchangeRateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, cached);
        }

        IReadOnlyList<ExchangeRate> fetched =
            await Inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken).ConfigureAwait(false);

        // Write the fetched rows and the covered window atomically, regardless of how many rows came back: the request
        // asked for every interior day, so even an empty fetch must record the whole window as covered or the same range
        // would be refetched forever. A single atomic write rules out persisting coverage without its rows.
        ExchangeRateCacheWriteStatus status = StoreFetchedRange(duration, pair, fetched, startDate, endDate, now);

        Log.RangeRefetched(_logger, _options.CacheRangeRefetchLogLevel, _cache.Provider, fromIsoCode, toIsoCode, fetched.Count, status);
        EmitProvenance(ExchangeRateProvenance.Live(_cache.Provider, _backend), fromIsoCode, toIsoCode);
        return new ExchangeRateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, fetched);
    }

    /// <inheritdoc />
    public decimal GetRate(string fromIsoCode, string toIsoCode)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        return new DatedExchangeRateProviderAdapter(this, today, _options.DefaultLookupOptions).GetRate(fromIsoCode, toIsoCode);
    }

    /// <summary>
    /// Attempts to resolve a single-date request from the fresh cached rows (and their inverse, when inversion is
    /// permitted) by delegating to a <see cref="FixedDatedExchangeRateProvider" /> built from them.
    /// </summary>
    /// <param name="duration">The duration cached rows stay fresh.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The calendar date for which a rate is required.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="now">The instant against which cached rows are evaluated for freshness.</param>
    /// <param name="result">When this method returns <see langword="true" />, the resolved result.</param>
    /// <param name="servedCachedAtUtc">
    /// When this method returns <see langword="true" />, the cache instant representing the served data: the
    /// <see cref="CachedExchangeRate.CachedAtUtc" /> of the row whose date matches the resolved result, or the oldest
    /// instant among the fresh candidate rows when no exact match is found. <see langword="null" /> when this method
    /// returns <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the request was satisfied from the cache; otherwise <see langword="false" />.
    /// </returns>
    private bool TryServeFromCache(TimeSpan duration, string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, DateTimeOffset now, out ExchangeRateLookupResult result, out DateTimeOffset? servedCachedAtUtc)
    {
        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);

        IReadOnlyList<CachedExchangeRate> direct = _cache.GetRates(pair, duration, now);

        var allowInverse = options?.AllowInverse ?? ExchangeRateLookupOptions.Exact.AllowInverse;
        IReadOnlyList<CachedExchangeRate> inverse = allowInverse
            ? _cache.GetRates(pair.Inverse(), duration, now)
            : Array.Empty<CachedExchangeRate>();

        if (direct.Count == 0 && inverse.Count == 0)
        {
            result = default;
            servedCachedAtUtc = null;
            return false;
        }

        var provider = _cache.Provider;
        List<ExchangeRate> rates = new(direct.Count + inverse.Count);
        foreach (CachedExchangeRate rate in direct)
            rates.Add(new ExchangeRate(fromIsoCode, toIsoCode, rate.Date, rate.Rate, provider));
        foreach (CachedExchangeRate rate in inverse)
            rates.Add(new ExchangeRate(toIsoCode, fromIsoCode, rate.Date, rate.Rate, provider));

        FixedDatedExchangeRateProvider snapshot = new(rates);
        if (!snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
        {
            servedCachedAtUtc = null;
            return false;
        }

        servedCachedAtUtc = ResolveServedInstant(direct, inverse, result.Rate.Date);

        // FixedDatedExchangeRateProvider flattens the cached rows to (Date, Rate) and drops the fetch instant, so restore
        // it from the matching cached row onto the rebuilt rate; the cache-write instant stays separate in the provenance.
        DateTimeOffset? servedObservedAt = ResolveServedObservedInstant(direct, inverse, result.Rate.Date);
        result = result with { Rate = result.Rate.WithFetchedAtUtc(servedObservedAt) };
        return true;
    }

    /// <summary>
    /// Attempts to serve a range request from the cache, treating the window as cached only when the recorded coverage
    /// contains every day of it — so a window that straddles an unfetched interior gap is not served from a sparse set
    /// of rows.
    /// </summary>
    /// <param name="duration">The duration cached rows and coverage windows stay fresh.</param>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="now">The instant against which cached rows and coverage are evaluated for freshness.</param>
    /// <param name="result">When this method returns <see langword="true" />, the rates within the range.</param>
    /// <param name="oldestCachedAtUtc">
    /// When this method returns <see langword="true" />, the oldest <see cref="CachedExchangeRate.CachedAtUtc" /> among
    /// the in-window rows added to <paramref name="result" />, or <see langword="null" /> when the covered window
    /// yields no rows. <see langword="null" /> when this method returns <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the range was satisfied from the cache; otherwise <see langword="false" />.
    /// </returns>
    private bool TryServeRangeFromCache(TimeSpan duration, ExchangeRatePair pair, DateOnly startDate, DateOnly endDate, DateTimeOffset now, out IReadOnlyList<ExchangeRate> result, out DateTimeOffset? oldestCachedAtUtc)
    {
        // The fresh coverage, not the span of the rate rows, decides whether the whole window was actually fetched.
        if (!_cache.GetCoverage(pair, duration, now).Contains(startDate, endDate))
        {
            result = Array.Empty<ExchangeRate>();
            oldestCachedAtUtc = null;
            return false;
        }

        var provider = _cache.Provider;
        IReadOnlyList<CachedExchangeRate> fresh = _cache.GetRates(pair, duration, now);
        List<ExchangeRate> rates = new();
        DateTimeOffset? oldest = null;
        foreach (CachedExchangeRate rate in fresh)
        {
            if (rate.Date >= startDate && rate.Date <= endDate)
            {
                // This serve path returns the rebuilt rows directly, so the cached fetch instant is stamped onto the
                // rate here rather than restored later; no FixedDatedExchangeRateProvider round-trip drops it.
                rates.Add(new ExchangeRate(pair.FromIsoCode, pair.ToIsoCode, rate.Date, rate.Rate, provider, isInverted: false, rate.ObservedAtUtc));
                if (oldest is not { } current || rate.CachedAtUtc < current)
                    oldest = rate.CachedAtUtc;
            }
        }

        result = rates;
        oldestCachedAtUtc = oldest;
        return true;
    }

    /// <summary>
    /// Stores the resolved single-date observation for the requested pair.
    /// </summary>
    /// <param name="duration">The duration cached rows stay fresh.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="result">The resolved lookup result returned by the inner provider.</param>
    /// <param name="now">The instant to stamp the cached row with.</param>
    /// <remarks>
    /// A single-date miss caches only the resolved row through <see cref="IExchangeRateCache.Store" /> and records no
    /// coverage window, so a later range query that spans the same day still refetches it. This asymmetry is by design:
    /// a single-date serve is satisfied per row, whereas only a range fetch establishes the contiguous coverage a range
    /// serve requires.
    /// </remarks>
    private void StoreResult(TimeSpan duration, string fromIsoCode, string toIsoCode, ExchangeRateLookupResult result, DateTimeOffset now)
    {
        ExchangeRatePair pair = new(fromIsoCode, toIsoCode);
        CachedExchangeRate[] rows = { new(result.Rate.Date, result.Rate.Rate, now, result.Rate.FetchedAtUtc) };
        _cache.Store(pair, rows, duration, now);
    }

    /// <summary>
    /// Atomically caches a fetched range of observations and records the whole requested window as covered for the
    /// requested pair.
    /// </summary>
    /// <param name="duration">The duration cached rows and the recorded coverage window stay fresh.</param>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="rates">The rates returned by the inner provider, possibly empty.</param>
    /// <param name="startDate">The inclusive first date of the fetched range.</param>
    /// <param name="endDate">The inclusive last date of the fetched range.</param>
    /// <param name="now">The instant to stamp the cached rows and the fetched window with.</param>
    /// <returns>The outcome of the atomic rows-and-coverage write.</returns>
    /// <remarks>
    /// The whole window is recorded as covered, not merely the dates that returned a row: the request asked for every
    /// interior day, so a later lookup of the same window can be served without refetching gaps. The rows and the
    /// coverage window are written as one atomic unit so a swallowed storage failure can never leave coverage recorded
    /// without its rows.
    /// </remarks>
    private ExchangeRateCacheWriteStatus StoreFetchedRange(TimeSpan duration, ExchangeRatePair pair, IReadOnlyList<ExchangeRate> rates, DateOnly startDate, DateOnly endDate, DateTimeOffset now)
    {
        var rows = new CachedExchangeRate[rates.Count];
        for (var i = 0; i < rates.Count; i++)
            rows[i] = new CachedExchangeRate(rates[i].Date, rates[i].Rate, now, rates[i].FetchedAtUtc);

        return _cache.StoreFetchedRange(pair, rows, startDate, endDate, duration, now);
    }

    /// <summary>
    /// Resolves the cache instant that represents a single-date serve: the cache instant of the candidate row whose
    /// date matches the resolved result, or the oldest instant among all candidate rows when no row matches that date.
    /// </summary>
    /// <param name="direct">The fresh candidate rows for the requested pair.</param>
    /// <param name="inverse">The fresh candidate rows for the inverse pair, empty when inversion is disallowed.</param>
    /// <param name="resolvedDate">The observation date of the resolved result.</param>
    /// <returns>
    /// The cache instant of the row dated <paramref name="resolvedDate" /> when one exists, otherwise the oldest cache
    /// instant among the candidate rows, or <see langword="null" /> when no candidate rows are present.
    /// </returns>
    /// <remarks>
    /// A nearest-date or inverse serve has no row dated exactly <paramref name="resolvedDate" />, so the oldest
    /// candidate instant is reported as a stable, lower-bound representative of the data backing the serve.
    /// </remarks>
    private static DateTimeOffset? ResolveServedInstant(IReadOnlyList<CachedExchangeRate> direct, IReadOnlyList<CachedExchangeRate> inverse, DateOnly resolvedDate)
    {
        DateTimeOffset? oldest = null;

        foreach (CachedExchangeRate row in direct)
        {
            if (row.Date == resolvedDate)
                return row.CachedAtUtc;

            if (oldest is not { } current || row.CachedAtUtc < current)
                oldest = row.CachedAtUtc;
        }

        foreach (CachedExchangeRate row in inverse)
        {
            if (row.Date == resolvedDate)
                return row.CachedAtUtc;

            if (oldest is not { } current || row.CachedAtUtc < current)
                oldest = row.CachedAtUtc;
        }

        return oldest;
    }

    /// <summary>
    /// Resolves the upstream fetch instant that represents a single-date serve: the
    /// <see cref="CachedExchangeRate.ObservedAtUtc" /> of the candidate row whose date matches the resolved result, or
    /// the upstream fetch instant of the oldest candidate row (by cache-write instant) when no row matches that date.
    /// </summary>
    /// <param name="direct">The fresh candidate rows for the requested pair.</param>
    /// <param name="inverse">The fresh candidate rows for the inverse pair, empty when inversion is disallowed.</param>
    /// <param name="resolvedDate">The observation date of the resolved result.</param>
    /// <returns>
    /// The upstream fetch instant of the row dated <paramref name="resolvedDate" /> when one exists, otherwise the
    /// upstream fetch instant of the oldest candidate row, or <see langword="null" /> when no candidate rows are
    /// present or none carried an upstream fetch instant.
    /// </returns>
    /// <remarks>
    /// Parallels <see cref="ResolveServedInstant" />: a nearest-date or inverse serve has no row dated exactly
    /// <paramref name="resolvedDate" />, so the oldest candidate's upstream fetch instant is reported, matching the
    /// cache-write instant the same serve reports through the provenance.
    /// </remarks>
    private static DateTimeOffset? ResolveServedObservedInstant(IReadOnlyList<CachedExchangeRate> direct, IReadOnlyList<CachedExchangeRate> inverse, DateOnly resolvedDate)
    {
        DateTimeOffset? oldestCachedAt = null;
        DateTimeOffset? oldestObservedAt = null;

        foreach (CachedExchangeRate row in direct)
        {
            if (row.Date == resolvedDate)
                return row.ObservedAtUtc;

            if (oldestCachedAt is not { } current || row.CachedAtUtc < current)
            {
                oldestCachedAt = row.CachedAtUtc;
                oldestObservedAt = row.ObservedAtUtc;
            }
        }

        foreach (CachedExchangeRate row in inverse)
        {
            if (row.Date == resolvedDate)
                return row.ObservedAtUtc;

            if (oldestCachedAt is not { } current || row.CachedAtUtc < current)
            {
                oldestCachedAt = row.CachedAtUtc;
                oldestObservedAt = row.ObservedAtUtc;
            }
        }

        return oldestObservedAt;
    }

    /// <summary>
    /// Emits the provenance of a served rate through the source-generated provenance log message at the configured
    /// level.
    /// </summary>
    /// <param name="provenance">The lineage of the served rate.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <remarks>
    /// The call is unconditional: the <see cref="LoggerMessageAttribute" /> source generator short-circuits before any
    /// argument is formatted when <see cref="CachingExchangeRateOptions.RateProvenanceLogLevel" /> is disabled.
    /// </remarks>
    private void EmitProvenance(ExchangeRateProvenance provenance, string fromIsoCode, string toIsoCode) =>
        Log.RateProvenance(
            _logger,
            _options.RateProvenanceLogLevel,
            provenance.Provider,
            fromIsoCode,
            toIsoCode,
            provenance.Origin,
            provenance.Backend,
            provenance.Age);
}
