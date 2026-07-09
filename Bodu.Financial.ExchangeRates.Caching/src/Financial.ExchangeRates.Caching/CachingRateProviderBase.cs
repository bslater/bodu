// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the shared caching mechanism for a provider that wraps a single inner
/// <see cref="IDatedRateProvider" /> over a single-provider <see cref="IRateCache" />: it serves fresh
/// rates from the cache and delegates to the inner provider only on a miss, caching what the inner provider returns.
/// Derived types supply the wrapped inner provider.
/// </summary>
/// <remarks>
/// <para>
/// The provider implements the same <see cref="IDatedRateProvider" /> contract the caller resolves, so it can
/// be inserted transparently, and also the timeless <see cref="IRateProvider" /> surface, which resolves the
/// current UTC date under <see cref="CachingRateOptions.DefaultLookupOptions" />. The cache's
/// <see cref="IRateCache.Provider" /> identifies the source: it selects the caching duration from
/// <see cref="CachingRateOptions.ProviderExpiry" /> (falling back to
/// <see cref="CachingRateOptions.DefaultExpiry" />) and tags both cached rows and log messages.
/// </para>
/// <para>
/// Single-date lookups serve per-row fresh observations and cache the resolved row on a miss. Range lookups serve from
/// the cache only when its recorded coverage contains the whole requested window — that is, every day in the window was
/// actually fetched and is still fresh — so an interior day that was never fetched forces a refetch rather than being
/// served from a sparse set of rows. On a miss the whole range is refetched from the inner provider and written back
/// through a single atomic <see cref="IRateCache.StoreFetchedRange" /> that merges the rows and records the
/// covered window together, even when the fetch returned no rows, so an empty-but-fetched window is not refetched on
/// the next lookup. To group several sources behind one entry point, wrap each in its own caching provider and compose
/// them with an <see cref="AggregatingRateProvider" />.
/// </para>
/// <para>
/// When the inner provider advertises its history depth through <see cref="IHistoryAwareRateProvider" /> and
/// <see cref="CachingRateOptions.RespectHistoryAvailability" /> is enabled (the default), misses for dates the
/// source has declared unavailable are not delegated: a single-date lookup outside the advertised history surfaces as
/// an ordinary miss without an inner call, and a range fetch is clamped to start at the advertised earliest date — or
/// skipped entirely when the whole window precedes it — while the full requested window is still recorded as covered,
/// so the unavailable prefix is not re-asked until normal expiry.
/// </para>
/// <para>
/// Because this provider is itself an <see cref="IDatedRateProvider" /> and accepts one as its inner source,
/// caching providers also <em>stack</em>: wrapping a cached provider in a second caching provider forms a tiered
/// read-through — a fast outer cache (for example in-memory) over a durable inner one (for example SQLite) over the
/// origin — where each layer is consulted in turn and only a miss falls through. Bind every layer's cache to the same
/// <see cref="IRateCache.Provider" /> so a served rate is tagged with the correct source.
/// </para>
/// <para>
/// Both the single-date and range surfaces additionally emit a provenance record alongside each hit/miss diagnostic,
/// recording whether the rate was resolved live or from the cache, the cache backend that served it, and — for a cache
/// serve — the age of the served data. The provenance event is logged at
/// <see cref="CachingRateOptions.RateProvenanceLogLevel" />.
/// </para>
/// <para>
/// Request coalescing is delegated to the inner provider rather than performed here, so the synchronous and
/// asynchronous surfaces stay identical: the shipped network providers already single-flight their downloads, so
/// concurrent misses for the same source collapse onto one fetch at the layer where the cost actually lives.
/// </para>
/// <para>
/// The provider is <see cref="IDisposable" />. By default it does not dispose the inner provider it wraps, because the
/// inner is supplied by the caller (and, under dependency injection, owned by the container). Pass <c>ownsInner</c> as
/// <see langword="true" /> at construction to make disposing this provider also dispose a disposable inner — the case
/// where a single owner composes a self-owning source (for example a provider that builds its own
/// <see cref="System.Net.Http.HttpClient" />) behind the cache by hand.
/// </para>
/// </remarks>
public abstract class CachingRateProviderBase
    : IDatedRateProvider, IRateProvider, IHistoryAwareRateProvider, IDisposable
{
    /// <summary>The single-provider cache that serves fresh rates and stores resolved observations.</summary>
    private readonly IRateCache _cache;

    /// <summary>The options carrying the caching durations and the timeless-surface lookup options.</summary>
    private readonly CachingRateOptions _options;

    /// <summary>The time source used to evaluate freshness and stamp newly cached rows.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The logger that records cache hits, misses, and refetches.</summary>
    private readonly ILogger _logger;

    /// <summary>The runtime identity of the cache backend, captured once at construction to avoid recomputing it on the serve path. It is reported as the backend of every provenance record this provider emits.</summary>
    private readonly string _backend;

    /// <summary>Indicates whether disposing this provider also disposes a disposable inner provider.</summary>
    private readonly bool _ownsInner;

    /// <summary>Tracks whether <see cref="Dispose()" /> has already run, so disposal is idempotent.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingRateProviderBase" /> class.
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
    /// <param name="ownsInner">
    /// <see langword="true" /> to dispose a disposable inner provider when this provider is disposed; otherwise
    /// <see langword="false" /> to leave the inner's lifetime to its owner.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected CachingRateProviderBase(IRateCache cache, CachingRateOptions options, TimeProvider? timeProvider, ILogger? logger = null, bool ownsInner = false)
    {
        ThrowHelper.ThrowIfNull(cache);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _cache = cache;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger.Instance;
        _backend = cache.GetType().Name;
        _ownsInner = ownsInner;
    }

    /// <summary>
    /// Gets the inner provider consulted on a cache miss.
    /// </summary>
    /// <value>The wrapped inner provider.</value>
    protected abstract IDatedRateProvider Inner { get; }

    /// <summary>
    /// Gets the history depth this provider advertises, forwarded from the inner provider it wraps. The cache itself
    /// adds no history of its own — it can only hold what the inner once served — so the decorator is exactly as deep
    /// as its source.
    /// </summary>
    /// <value>
    /// The inner provider's advertised availability when it implements <see cref="IHistoryAwareRateProvider" />;
    /// otherwise <see cref="RateHistoryAvailability.Unbounded" />.
    /// </value>
    public RateHistoryAvailability HistoryAvailability =>
        Inner is IHistoryAwareRateProvider aware
            ? aware.HistoryAvailability
            : RateHistoryAvailability.Unbounded;

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRate(fromIsoCode, toIsoCode, today, options ?? _options.DefaultLookupOptions);
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out RateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.IO_KeyNotFound_ExchangeRate, fromIsoCode, toIsoCode, date));
    }

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        TimeSpan duration = _options.GetExpiry(_cache.Provider);

        if (TryServeFromCache(duration, fromIsoCode, toIsoCode, date, options, now, out result, out DateTimeOffset? servedCachedAtUtc))
        {
            // The snapshot built from cached rows yields Live provenance; overwrite it with the cache lineage so the
            // returned result and the logged provenance describe the same serve and cannot diverge.
            result = result with { Provenance = CacheServeProvenance(servedCachedAtUtc, now) };

            Log.CacheHit(_logger, _options.CacheHitLogLevel, _cache.Provider, fromIsoCode, toIsoCode, date);
            EmitProvenance(result.Provenance, fromIsoCode, toIsoCode);
            return true;
        }

        if (IsOutsideAdvertisedHistory(date, options, now, out DateOnly earliest))
        {
            Log.SkippedDateOutsideHistory(_logger, _options.HistoryClampLogLevel, _cache.Provider, fromIsoCode, toIsoCode, date, earliest);

            result = default;
            return false;
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
    public ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return GetRateAsync(fromIsoCode, toIsoCode, today, options ?? _options.DefaultLookupOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<RateLookupResult> GetRateAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        RateLookupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        TimeSpan duration = _options.GetExpiry(_cache.Provider);

        if (TryServeFromCache(duration, fromIsoCode, toIsoCode, date, options, now, out RateLookupResult result, out DateTimeOffset? servedCachedAtUtc))
        {
            // Overwrite the snapshot's Live provenance with the cache lineage exactly as the synchronous path does, so a
            // cache hit returns Origin.Cache (with the backend and age) on both surfaces rather than diverging.
            result = result with { Provenance = CacheServeProvenance(servedCachedAtUtc, now) };

            Log.CacheHit(_logger, _options.CacheHitLogLevel, _cache.Provider, fromIsoCode, toIsoCode, date);
            EmitProvenance(result.Provenance, fromIsoCode, toIsoCode);
            return result;
        }

        if (IsOutsideAdvertisedHistory(date, options, now, out DateOnly earliest))
        {
            // A skipped date surfaces exactly as a miss does on the throwing surfaces.
            Log.SkippedDateOutsideHistory(_logger, _options.HistoryClampLogLevel, _cache.Provider, fromIsoCode, toIsoCode, date, earliest);

            throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.IO_KeyNotFound_ExchangeRate, fromIsoCode, toIsoCode, date));
        }

        result = await Inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken).ConfigureAwait(false);
        StoreResult(duration, fromIsoCode, toIsoCode, result, now);
        Log.CacheMissStored(_logger, _options.CacheMissLogLevel, _cache.Provider, fromIsoCode, toIsoCode, date);

        // A miss carries the inner provider's Live provenance through unchanged, identically to the synchronous path.
        EmitProvenance(result.Provenance, fromIsoCode, toIsoCode);
        return result;
    }

    /// <inheritdoc />
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (endDate < startDate)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_RangeInverted, nameof(endDate));

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        DateTimeOffset now = _timeProvider.GetUtcNow();
        TimeSpan duration = _options.GetExpiry(_cache.Provider);

        if (TryServeRangeFromCache(duration, pair, startDate, endDate, now, out IReadOnlyList<ExchangeRate> cached, out DateTimeOffset? oldestCachedAtUtc))
        {
            Log.RangeCacheHit(_logger, _options.CacheRangeHitLogLevel, _cache.Provider, fromIsoCode, toIsoCode);
            EmitProvenance(CacheServeProvenance(oldestCachedAtUtc, now), fromIsoCode, toIsoCode);
            return new RateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, cached);
        }

        if (!TryClampFetchStart(startDate, endDate, now, out DateOnly fetchStart, out DateOnly? earliestAvailable))
        {
            // No part of the window is inside the inner's advertised history: skip the doomed fetch and record the
            // whole window as covered-with-no-rows, so repeats are served as an (empty) cache hit until expiry.
            RateCacheWriteStatus skipStatus = StoreFetchedRange(duration, pair, [], startDate, endDate, now);

            Log.SkippedRangeOutsideHistory(_logger, _options.HistoryClampLogLevel, _cache.Provider, fromIsoCode, toIsoCode, startDate, endDate, earliestAvailable!.Value, skipStatus);
            return new RateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, []);
        }

        if (fetchStart != startDate)
            Log.ClampedRangeToHistory(_logger, _options.HistoryClampLogLevel, _cache.Provider, fromIsoCode, toIsoCode, startDate, fetchStart);

        IReadOnlyList<ExchangeRate> fetched = [.. Inner.GetRates(fromIsoCode, toIsoCode, fetchStart, endDate)];

        // Write the fetched rows and the covered window atomically, regardless of how many rows came back: the request
        // asked for every interior day, so even an empty fetch must record the whole window as covered or the same range
        // would be refetched forever. A single atomic write rules out persisting coverage without its rows. Coverage is
        // recorded over the full requested window even when the fetch start was clamped, so the prefix the source has
        // declared unavailable is not refetched until normal expiry.
        RateCacheWriteStatus status = StoreFetchedRange(duration, pair, fetched, startDate, endDate, now);

        Log.RangeRefetched(_logger, _options.CacheRangeRefetchLogLevel, _cache.Provider, fromIsoCode, toIsoCode, fetched.Count, status);
        EmitProvenance(RateProvenance.Live(_cache.Provider, _backend), fromIsoCode, toIsoCode);
        return new RateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, fetched);
    }

    /// <inheritdoc />
    public async ValueTask<RateRangeResult> GetRatesAsync(
        string fromIsoCode,
        string toIsoCode,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (endDate < startDate)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_RangeInverted, nameof(endDate));

        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        DateTimeOffset now = _timeProvider.GetUtcNow();
        TimeSpan duration = _options.GetExpiry(_cache.Provider);

        if (TryServeRangeFromCache(duration, pair, startDate, endDate, now, out IReadOnlyList<ExchangeRate> cached, out DateTimeOffset? oldestCachedAtUtc))
        {
            Log.RangeCacheHit(_logger, _options.CacheRangeHitLogLevel, _cache.Provider, fromIsoCode, toIsoCode);
            EmitProvenance(CacheServeProvenance(oldestCachedAtUtc, now), fromIsoCode, toIsoCode);
            return new RateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, cached);
        }

        if (!TryClampFetchStart(startDate, endDate, now, out DateOnly fetchStart, out DateOnly? earliestAvailable))
        {
            // No part of the window is inside the inner's advertised history: skip the doomed fetch and record the
            // whole window as covered-with-no-rows, identically to the synchronous path.
            RateCacheWriteStatus skipStatus = StoreFetchedRange(duration, pair, [], startDate, endDate, now);

            Log.SkippedRangeOutsideHistory(_logger, _options.HistoryClampLogLevel, _cache.Provider, fromIsoCode, toIsoCode, startDate, endDate, earliestAvailable!.Value, skipStatus);
            return new RateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, []);
        }

        if (fetchStart != startDate)
            Log.ClampedRangeToHistory(_logger, _options.HistoryClampLogLevel, _cache.Provider, fromIsoCode, toIsoCode, startDate, fetchStart);

        IReadOnlyList<ExchangeRate> fetched =
            await Inner.GetRatesAsync(fromIsoCode, toIsoCode, fetchStart, endDate, cancellationToken).ConfigureAwait(false);

        // Write the fetched rows and the covered window atomically, regardless of how many rows came back: the request
        // asked for every interior day, so even an empty fetch must record the whole window as covered or the same range
        // would be refetched forever. A single atomic write rules out persisting coverage without its rows. Coverage is
        // recorded over the full requested window even when the fetch start was clamped, so the prefix the source has
        // declared unavailable is not refetched until normal expiry.
        RateCacheWriteStatus status = StoreFetchedRange(duration, pair, fetched, startDate, endDate, now);

        Log.RangeRefetched(_logger, _options.CacheRangeRefetchLogLevel, _cache.Provider, fromIsoCode, toIsoCode, fetched.Count, status);
        EmitProvenance(RateProvenance.Live(_cache.Provider, _backend), fromIsoCode, toIsoCode);
        return new RateRangeResult(fromIsoCode, toIsoCode, startDate, endDate, fetched);
    }

    /// <inheritdoc />
    public decimal GetRate(string fromIsoCode, string toIsoCode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        return new DatedRateProviderAdapter(this, today, _options.DefaultLookupOptions).GetRate(fromIsoCode, toIsoCode);
    }

    /// <summary>
    /// Determines whether a single-date lookup should be skipped because no date it could resolve to lies inside the
    /// inner provider's advertised history.
    /// </summary>
    /// <param name="date">The requested date.</param>
    /// <param name="options">The lookup rules to apply; <see langword="null" /> selects the exact-match rules.</param>
    /// <param name="now">The instant the advertised rolling window is evaluated against.</param>
    /// <param name="earliest">
    /// When this method returns <see langword="true" />, the earliest date the inner provider advertises it can serve.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the clamp is enabled, the inner provider is history-aware, and the latest date the
    /// lookup could resolve to still precedes the advertised earliest date; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Forward-resolving rules (<see cref="RateDateResolution.NextOnOrAfter" /> and the nearest family) can
    /// reach up to <see cref="RateLookupOptions.ToleranceDays" /> past the requested date, so the guard tests
    /// that reachable maximum rather than the requested date itself — a request just outside the advertised floor whose
    /// tolerance reaches back inside it is still delegated.
    /// </remarks>
    private bool IsOutsideAdvertisedHistory(DateOnly date, RateLookupOptions? options, DateTimeOffset now, out DateOnly earliest)
    {
        earliest = default;

        if (!_options.RespectHistoryAvailability || Inner is not IHistoryAwareRateProvider aware)
            return false;

        DateOnly? floor = aware.HistoryAvailability.GetEarliestAvailable(DateOnly.FromDateTime(now.UtcDateTime));
        if (floor is null)
            return false;

        DateOnly latestReachable = HistoryAvailabilityGuard.LatestReachableDate(date, options);
        if (latestReachable >= floor.Value)
            return false;

        earliest = floor.Value;
        return true;
    }

    /// <summary>
    /// Applies the inner provider's advertised history to a requested range window, resolving the first date worth
    /// fetching.
    /// </summary>
    /// <param name="startDate">The inclusive first date of the requested window.</param>
    /// <param name="endDate">The inclusive last date of the requested window.</param>
    /// <param name="now">The instant the advertised rolling window is evaluated against.</param>
    /// <param name="fetchStart">
    /// The first date to fetch from the inner provider: the requested start when no clamp applies, or the advertised
    /// earliest date when the window starts before it.
    /// </param>
    /// <param name="earliestAvailable">
    /// The earliest date the inner provider advertises it can serve when that floor clips the window; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when at least part of the window is inside the advertised history and a fetch should be
    /// issued; <see langword="false" /> when the whole window precedes the advertised earliest date and the fetch
    /// should be skipped.
    /// </returns>
    private bool TryClampFetchStart(DateOnly startDate, DateOnly endDate, DateTimeOffset now, out DateOnly fetchStart, out DateOnly? earliestAvailable)
    {
        fetchStart = startDate;
        earliestAvailable = null;

        if (!_options.RespectHistoryAvailability || Inner is not IHistoryAwareRateProvider aware)
            return true;

        DateOnly? floor = aware.HistoryAvailability.GetEarliestAvailable(DateOnly.FromDateTime(now.UtcDateTime));
        if (floor is null || floor.Value <= startDate)
            return true;

        earliestAvailable = floor;
        if (floor.Value > endDate)
            return false;

        fetchStart = floor.Value;
        return true;
    }

    /// <summary>
    /// Builds the provenance for a rate served from this provider's cache, attributing it to the bound provider and the
    /// cache backend captured at construction.
    /// </summary>
    /// <param name="servedCachedAtUtc">
    /// The cache-write instant representing the served data, or <see langword="null" /> when no cached row backs it.
    /// </param>
    /// <param name="asOf">The lookup instant the served data's age is derived from.</param>
    /// <returns>
    /// An <see cref="RateProvenance" /> carrying <see cref="RateOrigin.Cache" /> lineage.
    /// </returns>
    /// <remarks>
    /// Used by every serve path — single-date and range, synchronous and asynchronous — so a cache hit reports an
    /// identical provenance regardless of the surface it was served through.
    /// </remarks>
    private RateProvenance CacheServeProvenance(DateTimeOffset? servedCachedAtUtc, DateTimeOffset asOf) =>
        RateProvenance.FromCache(_cache.Provider, _backend, servedCachedAtUtc, asOf);

    /// <summary>
    /// Attempts to resolve a single-date request from the fresh cached rows (and their inverse, when inversion is
    /// permitted) by delegating to a <see cref="FixedDatedRateProvider" /> built from them.
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
    /// <see cref="CachedRate.CachedAtUtc" /> of the row whose date matches the resolved result, or the oldest
    /// instant among the fresh candidate rows when no exact match is found. <see langword="null" /> when this method
    /// returns <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the request was satisfied from the cache; otherwise <see langword="false" />.
    /// </returns>
    private bool TryServeFromCache(TimeSpan duration, string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, DateTimeOffset now, out RateLookupResult result, out DateTimeOffset? servedCachedAtUtc)
    {
        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));

        IReadOnlyList<CachedRate> direct = _cache.GetRates(pair, duration, now);

        bool allowInverse = options?.AllowInverse ?? RateLookupOptions.Exact.AllowInverse;
        IReadOnlyList<CachedRate> inverse = allowInverse
            ? _cache.GetRates(pair.Inverse(), duration, now)
            : Array.Empty<CachedRate>();

        if (direct.Count == 0 && inverse.Count == 0)
        {
            result = default;
            servedCachedAtUtc = null;
            return false;
        }

        string provider = _cache.Provider;
        List<ExchangeRate> rates = new(direct.Count + inverse.Count);
        foreach (CachedRate rate in direct)
            rates.Add(new ExchangeRate(pair.From, pair.To, rate.Date, rate.Rate, provider));
        foreach (CachedRate rate in inverse)
            rates.Add(new ExchangeRate(pair.To, pair.From, rate.Date, rate.Rate, provider));

        FixedDatedRateProvider snapshot = new(rates);
        if (!snapshot.TryGetRate(fromIsoCode, toIsoCode, date, options, out result))
        {
            servedCachedAtUtc = null;
            return false;
        }

        servedCachedAtUtc = ResolveServedInstant(direct, inverse, result.Rate.Date);

        // FixedDatedRateProvider flattens the cached rows to (Date, Rate) and drops the fetch instant, so restore
        // it from the matching cached row onto the rebuilt rate; the cache-write instant stays separate in the provenance.
        DateTimeOffset? servedObservedAt = ResolveServedObservedInstant(direct, inverse, result.Rate.Date);
        result = result with { Rate = result.Rate.WithFetchedAtUtc(servedObservedAt) };
        return true;
    }

    /// <summary>
    /// Gets a value indicating whether a range request that misses the direct pair's coverage may instead be served
    /// from a complete inverse-pair coverage by reciprocating each rate, governed by the provider's default lookup
    /// options.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when inverse range serving is permitted; otherwise <see langword="false" />.
    /// </value>
    private bool AllowInverseRangeServe => _options.DefaultLookupOptions.AllowInverse;

    /// <summary>
    /// Attempts to serve a range request from the cache, treating the window as cached only when the recorded coverage
    /// contains every day of it — so a window that straddles an unfetched interior gap is not served from a sparse set
    /// of rows. The direct pair is preferred; a complete inverse-pair coverage serves the window by inverting each rate
    /// when inversion is permitted.
    /// </summary>
    /// <param name="duration">The duration cached rows and coverage windows stay fresh.</param>
    /// <param name="pair">The requested currency pair.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="now">The instant against which cached rows and coverage are evaluated for freshness.</param>
    /// <param name="result">When this method returns <see langword="true" />, the rates within the range.</param>
    /// <param name="oldestCachedAtUtc">
    /// When this method returns <see langword="true" />, the oldest <see cref="CachedRate.CachedAtUtc" /> among
    /// the in-window rows added to <paramref name="result" />, or <see langword="null" /> when the covered window
    /// yields no rows. <see langword="null" /> when this method returns <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the range was satisfied from the cache; otherwise <see langword="false" />.
    /// </returns>
    private bool TryServeRangeFromCache(TimeSpan duration, CurrencyPair pair, DateOnly startDate, DateOnly endDate, DateTimeOffset now, out IReadOnlyList<ExchangeRate> result, out DateTimeOffset? oldestCachedAtUtc)
    {
        // The fresh coverage, not the span of the rate rows, decides whether the whole window was actually fetched. The
        // direct pair is tried first; when inversion is permitted a complete inverse-pair coverage serves the window by
        // inverting each rate, mirroring the single-date serve path.
        if (_cache.GetCoverage(pair, duration, now).Contains(startDate, endDate))
            return BuildRange(pair, duration, startDate, endDate, now, invert: false, out result, out oldestCachedAtUtc);

        if (AllowInverseRangeServe && _cache.GetCoverage(pair.Inverse(), duration, now).Contains(startDate, endDate))
            return BuildRange(pair.Inverse(), duration, startDate, endDate, now, invert: true, out result, out oldestCachedAtUtc);

        result = Array.Empty<ExchangeRate>();
        oldestCachedAtUtc = null;
        return false;
    }

    /// <summary>
    /// Builds the in-window rates for a range serve from the cached rows of <paramref name="cachedPair" />, optionally
    /// reciprocating each rate so a complete inverse-pair coverage can satisfy the requested direction.
    /// </summary>
    /// <param name="cachedPair">
    /// The pair whose cached rows back the serve — the requested pair, or its inverse when <paramref name="invert" />
    /// is set.
    /// </param>
    /// <param name="duration">The duration cached rows stay fresh.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="now">The instant against which cached rows are evaluated for freshness.</param>
    /// <param name="invert">
    /// <see langword="true" /> to reciprocate each cached rate and emit the inverse direction; otherwise
    /// <see langword="false" />.
    /// </param>
    /// <param name="result">The rates within the range, always carrying the requested direction.</param>
    /// <param name="oldestCachedAtUtc">
    /// The oldest <see cref="CachedRate.CachedAtUtc" /> among the in-window rows, or <see langword="null" />
    /// when the covered window yields no rows.
    /// </param>
    /// <returns>Always <see langword="true" />; the caller has already confirmed the window is covered.</returns>
    private bool BuildRange(CurrencyPair cachedPair, TimeSpan duration, DateOnly startDate, DateOnly endDate, DateTimeOffset now, bool invert, out IReadOnlyList<ExchangeRate> result, out DateTimeOffset? oldestCachedAtUtc)
    {
        string provider = _cache.Provider;
        IReadOnlyList<CachedRate> fresh = _cache.GetRates(cachedPair, duration, now);

        // The output always carries the requested direction. When inverting, the cached pair is the requested pair's
        // inverse, so the requested from/to are the cached pair's to/from.
        CurrencyCode fromCode = invert ? cachedPair.To : cachedPair.From;
        CurrencyCode toCode = invert ? cachedPair.From : cachedPair.To;

        List<ExchangeRate> rates = new();
        DateTimeOffset? oldest = null;
        foreach (CachedRate rate in fresh)
        {
            if (rate.Date < startDate || rate.Date > endDate)
                continue;

            // A non-positive cached rate cannot be reciprocated; skip it rather than divide by zero or flip its sign.
            if (invert && rate.Rate <= 0m)
                continue;

            decimal value = invert ? 1m / rate.Rate : rate.Rate;

            // This serve path returns the rebuilt rows directly, so the cached fetch instant is stamped onto the rate
            // here rather than restored later; no FixedDatedRateProvider round-trip drops it.
            rates.Add(new ExchangeRate(fromCode, toCode, rate.Date, value, provider, isInverted: invert, rate.ObservedAtUtc));
            if (oldest is not { } current || rate.CachedAtUtc < current)
                oldest = rate.CachedAtUtc;
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
    /// A single-date miss caches only the resolved row through <see cref="IRateCache.Store" /> and records no
    /// coverage window, so a later range query that spans the same day still refetches it. This asymmetry is by design:
    /// a single-date serve is satisfied per row, whereas only a range fetch establishes the contiguous coverage a range
    /// serve requires.
    /// </remarks>
    private void StoreResult(TimeSpan duration, string fromIsoCode, string toIsoCode, RateLookupResult result, DateTimeOffset now)
    {
        CurrencyPair pair = new(CurrencyInfo.ParseCurrencyCode(fromIsoCode), CurrencyInfo.ParseCurrencyCode(toIsoCode));
        CachedRate[] rows = { new(result.Rate.Date, result.Rate.Rate, now, result.Rate.FetchedAtUtc) };
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
    private RateCacheWriteStatus StoreFetchedRange(TimeSpan duration, CurrencyPair pair, IReadOnlyList<ExchangeRate> rates, DateOnly startDate, DateOnly endDate, DateTimeOffset now)
    {
        var rows = new CachedRate[rates.Count];
        for (int i = 0; i < rates.Count; i++)
            rows[i] = new CachedRate(rates[i].Date, rates[i].Rate, now, rates[i].FetchedAtUtc);

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
    private static DateTimeOffset? ResolveServedInstant(IReadOnlyList<CachedRate> direct, IReadOnlyList<CachedRate> inverse, DateOnly resolvedDate)
    {
        DateTimeOffset? oldest = null;

        foreach (CachedRate row in direct)
        {
            if (row.Date == resolvedDate)
                return row.CachedAtUtc;

            if (oldest is not { } current || row.CachedAtUtc < current)
                oldest = row.CachedAtUtc;
        }

        foreach (CachedRate row in inverse)
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
    /// <see cref="CachedRate.ObservedAtUtc" /> of the candidate row whose date matches the resolved result, or
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
    private static DateTimeOffset? ResolveServedObservedInstant(IReadOnlyList<CachedRate> direct, IReadOnlyList<CachedRate> inverse, DateOnly resolvedDate)
    {
        DateTimeOffset? oldestCachedAt = null;
        DateTimeOffset? oldestObservedAt = null;

        foreach (CachedRate row in direct)
        {
            if (row.Date == resolvedDate)
                return row.ObservedAtUtc;

            if (oldestCachedAt is not { } current || row.CachedAtUtc < current)
            {
                oldestCachedAt = row.CachedAtUtc;
                oldestObservedAt = row.ObservedAtUtc;
            }
        }

        foreach (CachedRate row in inverse)
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
    /// argument is formatted when <see cref="CachingRateOptions.RateProvenanceLogLevel" /> is disabled.
    /// </remarks>
    private void EmitProvenance(RateProvenance provenance, string fromIsoCode, string toIsoCode) =>
        Log.RateProvenance(
            _logger,
            _options.RateProvenanceLogLevel,
            provenance.Provider,
            fromIsoCode,
            toIsoCode,
            provenance.Origin,
            provenance.Backend,
            provenance.Age);

    /// <summary>
    /// Releases the resources held by this provider, disposing the inner provider when <c>ownsInner</c> was set at
    /// construction and the inner is <see cref="IDisposable" />.
    /// </summary>
    /// <remarks>
    /// Disposal is idempotent. The cache is not disposed here: the shipped caches hold no unmanaged resources, and a
    /// caller-supplied cache is owned by its caller.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_ownsInner && Inner is IDisposable disposable)
            disposable.Dispose();

        GC.SuppressFinalize(this);
    }
}
