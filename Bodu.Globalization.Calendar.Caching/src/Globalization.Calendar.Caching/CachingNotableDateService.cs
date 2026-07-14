// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateService" /> decorator that serves notable-date resolutions from an
/// <see cref="INotableDateCache" />, recomputing and caching a whole civil year on a miss and refreshing on a
/// time-to-live or a resource-version change.
/// </summary>
/// <remarks>
/// <para>
/// Every query is answered per civil year: a range is decomposed into the years it spans, each year is served from the
/// cache when a fresh, version-matching entry exists or otherwise recomputed for the whole year and written back, and
/// the assembled occurrences are clipped to the requested window by their emitted date. Because the notable-date engine
/// resolves per Gregorian year, a whole-year entry is the reusable unit: a later single-day or sub-range query for the
/// same year is served without recomputing, and a query for exactly one whole civil year is served as the cached list
/// itself with no copying.
/// </para>
/// <para>
/// Output ordering relies on the <see cref="INotableDateCache" /> ordering contract — cached occurrences round-trip in
/// the order supplied, which is the wrapped service's date-then-identity ordering, and emitted dates in one civil year
/// always precede the next year's — so assembled results are ordered without re-sorting. As a safeguard, the assembled
/// result is verified with a linear scan, and only a non-conforming cache backend pays a full sort.
/// </para>
/// <para>
/// The filtered overloads apply the filter after assembling the unfiltered result, exactly as the wrapped service does,
/// so a <see cref="NotableDateFilter" /> never participates in the cache key. The discovery methods delegate straight
/// to the wrapped service.
/// </para>
/// <para>
/// When constructed with an <see cref="INotableDateResourceProvider" />, the decorator observes the resource currently
/// in effect and derives a version token from its identity and a reload generation, so a
/// <see cref="MutableNotableDateResourceProvider.Reload(NotableDateResource)" /> invalidates every cached year on the
/// next query. Without a provider, a fixed version token from the options is used.
/// </para>
/// </remarks>
public sealed class CachingNotableDateService
    : INotableDateService, IDisposable
{
    /// <summary>The version token used when no resource provider is observed and the options supply none.</summary>
    private const string DefaultVersionToken = "default";

    /// <summary>The wrapped service that computes a year on a cache miss.</summary>
    private readonly INotableDateService _inner;

    /// <summary>The cache computed years are served from and written to.</summary>
    private readonly INotableDateCache _cache;

    /// <summary>The validated caching options.</summary>
    private readonly NotableDateCachingOptions _options;

    /// <summary>The resource provider observed for reloads, or <see langword="null" /> for a fixed version token.</summary>
    private readonly INotableDateResourceProvider? _versionSource;

    /// <summary>The clock the computed and lookup instants are measured against.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The logger that receives the per-lookup hit and miss diagnostics.</summary>
    private readonly ILogger _logger;

    /// <summary>Whether the decorator owns and disposes the cache.</summary>
    private readonly bool _ownsCache;

    /// <summary>Guards the version-token derivation when the observed resource reference changes.</summary>
    private readonly object _versionLock = new();

    /// <summary>The immutable pair of the resource observed on the previous version resolution and the token derived for it, swapped atomically on reload so the per-query fast path is a single volatile read with no lock.</summary>
    private volatile Tuple<NotableDateResource, string>? _version;

    /// <summary>The monotonic reload generation, bumped whenever the observed resource reference changes.</summary>
    private long _generation;

    /// <summary>Guards against double disposal.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingNotableDateService" /> class.
    /// </summary>
    /// <param name="inner">The service that computes a year on a cache miss.</param>
    /// <param name="cache">The cache computed years are served from and written to.</param>
    /// <param name="options">The caching options.</param>
    /// <param name="versionSource">
    /// The resource provider to observe for reloads, or <see langword="null" /> to use a fixed version token from
    /// <paramref name="options" />.
    /// </param>
    /// <param name="timeProvider">The clock the computed and lookup instants are measured against, or <see langword="null" /> for <see cref="TimeProvider.System" />.</param>
    /// <param name="loggerFactory">The factory used to create the diagnostics logger, or <see langword="null" /> to disable logging.</param>
    /// <param name="ownsCache">Whether this service disposes <paramref name="cache" /> when it is disposed.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner" />, <paramref name="cache" />, or <paramref name="options" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public CachingNotableDateService(
        INotableDateService inner,
        INotableDateCache cache,
        NotableDateCachingOptions options,
        INotableDateResourceProvider? versionSource = null,
        TimeProvider? timeProvider = null,
        ILoggerFactory? loggerFactory = null,
        bool ownsCache = false)
    {
        ThrowHelper.ThrowIfNull(inner);
        ThrowHelper.ThrowIfNull(cache);
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _inner = inner;
        _cache = cache;
        _options = options;
        _versionSource = versionSource;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = loggerFactory?.CreateLogger<CachingNotableDateService>() ?? NullLogger<CachingNotableDateService>.Instance;
        _ownsCache = ownsCache;
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory) =>
        Resolve(new DateRange(date, date), territory);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        // Preserve the wrapped service's inverted-range behaviour exactly rather than silently returning empty.
        if (range.EndDate < range.StartDate)
            return _inner.Resolve(range, territory);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        string version = ResolveVersion();
        TimeSpan ttl = _options.Ttl;

        int firstYear = range.StartDate.Year;
        int lastYear = range.EndDate.Year;

        if (firstYear == lastYear)
        {
            IReadOnlyList<NotableDate> year = ResolveYear(territory, firstYear, version, ttl, now);

            // Whole-civil-year window (the Resolve(year, territory) extension shape): the cached year list is the
            // answer itself — no clipping, no copying.
            if (range.StartDate.DayOfYear == 1 && range.EndDate.Month == 12 && range.EndDate.Day == 31)
                return EnsureOrdered(year);

            return EnsureOrdered(Clip(year, range));
        }

        List<NotableDate> assembled = new();

        // A backend that stores a territory as one unit exposes the batch seam, so the whole span is read from one
        // territory state read instead of once per year; other backends are consulted per year.
        if (_cache is INotableDateCacheBatchReader batchReader)
        {
            NotableDateCacheEntry?[] years = batchReader.GetYears(territory, firstYear, lastYear, version, ttl, now);
            for (int year = firstYear; year <= lastYear; year++)
            {
                NotableDateCacheEntry? entry = years[year - firstYear];
                IReadOnlyList<NotableDate> occurrences;
                if (entry is not null)
                {
                    Log.CacheHit(_logger, _options.CacheHitLogLevel, territory, year, (now - entry.ComputedAtUtc).TotalSeconds);
                    occurrences = entry.Occurrences;
                }
                else
                {
                    occurrences = ComputeAndStoreYear(territory, year, version, ttl, now);
                }

                AppendInRange(assembled, occurrences, range);
            }
        }
        else
        {
            for (int year = firstYear; year <= lastYear; year++)
                AppendInRange(assembled, ResolveYear(territory, year, version, ttl, now), range);
        }

        return EnsureOrdered(assembled);
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory, NotableDateFilter filter) =>
        Resolve(new DateRange(date, date), territory, filter);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory, NotableDateFilter filter)
    {
        ThrowHelper.ThrowIfNull(filter);

        return [.. Resolve(range, territory).Where(filter.Matches)];
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedTerritories() =>
        _inner.GetSupportedTerritories();

    /// <inheritdoc />
    public IReadOnlyList<CalendarSystem> GetSupportedCalendars() =>
        _inner.GetSupportedCalendars();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_ownsCache && _cache is IDisposable disposable)
            disposable.Dispose();
    }

    /// <summary>
    /// Resolves one civil year, serving it from the cache when a fresh entry exists or otherwise recomputing the whole
    /// year and caching it.
    /// </summary>
    /// <param name="territory">The requested territory code, passed through unmodified; the cache normalizes it.</param>
    /// <param name="year">The civil year to resolve.</param>
    /// <param name="version">The resource version token entries are keyed by.</param>
    /// <param name="ttl">The time-to-live freshness is evaluated against.</param>
    /// <param name="now">The lookup instant.</param>
    /// <returns>The occurrences emitted within the year, unfiltered, in the wrapped service's order.</returns>
    private IReadOnlyList<NotableDate> ResolveYear(string territory, int year, string version, TimeSpan ttl, DateTimeOffset now)
    {
        NotableDateCacheEntry? entry = _cache.GetYear(territory, year, version, ttl, now);
        if (entry is not null)
        {
            Log.CacheHit(_logger, _options.CacheHitLogLevel, territory, year, (now - entry.ComputedAtUtc).TotalSeconds);
            return entry.Occurrences;
        }

        return ComputeAndStoreYear(territory, year, version, ttl, now);
    }

    /// <summary>
    /// Computes one civil year through the wrapped service and writes it to the cache — the shared miss path of the
    /// per-year and batch read branches.
    /// </summary>
    /// <param name="territory">The requested territory code, passed through unmodified; the cache normalizes it.</param>
    /// <param name="year">The civil year to compute.</param>
    /// <param name="version">The resource version token the entry is keyed by.</param>
    /// <param name="ttl">The time-to-live the write prunes against.</param>
    /// <param name="now">The instant the computed year is stamped with.</param>
    /// <returns>The computed occurrences, unfiltered, in the wrapped service's order.</returns>
    private IReadOnlyList<NotableDate> ComputeAndStoreYear(string territory, int year, string version, TimeSpan ttl, DateTimeOffset now)
    {
        IReadOnlyList<NotableDate> occurrences =
            _inner.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory);

        _cache.StoreYear(new NotableDateCacheEntry(territory, year, version, occurrences, now), ttl, now);

        Log.CacheMiss(_logger, _options.CacheMissLogLevel, territory, year, occurrences.Count);
        return occurrences;
    }

    /// <summary>
    /// Resolves the resource-version token, deriving it from the observed resource identity and reload generation when
    /// a provider is present, or from the fixed options token otherwise.
    /// </summary>
    /// <returns>The current resource-version token.</returns>
    /// <remarks>
    /// The common case — the observed resource has not changed since the previous query — is a single volatile read and
    /// a reference comparison, with no lock. The lock is taken only when a reload has swapped the resource reference,
    /// deriving the new token once and publishing the immutable pair atomically.
    /// </remarks>
    private string ResolveVersion()
    {
        if (_versionSource is null)
            return _options.ResourceVersion ?? DefaultVersionToken;

        NotableDateResource current = _versionSource.Current;

        Tuple<NotableDateResource, string>? version = _version;
        if (version is not null && ReferenceEquals(version.Item1, current))
            return version.Item2;

        lock (_versionLock)
        {
            // Re-check under the lock: a concurrent caller may already have derived the token for this resource.
            version = _version;
            if (version is not null && ReferenceEquals(version.Item1, current))
                return version.Item2;

            // A reload swaps the resource reference; bump the generation so the token — and therefore every cache key —
            // changes even when the new resource carries the same identifier and schema version as the old one.
            _generation++;
            string token = string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2}",
                current.ResourceId,
                current.SchemaVersion,
                _generation);

            _version = Tuple.Create(current, token);
            return token;
        }
    }

    /// <summary>
    /// Copies the occurrences whose emitted date falls within the requested window into a new list, preserving order.
    /// </summary>
    /// <param name="occurrences">The year's occurrences, in stored order.</param>
    /// <param name="range">The requested window.</param>
    /// <returns>The clipped occurrences.</returns>
    private static List<NotableDate> Clip(IReadOnlyList<NotableDate> occurrences, DateRange range)
    {
        List<NotableDate> clipped = new(occurrences.Count);
        AppendInRange(clipped, occurrences, range);
        return clipped;
    }

    /// <summary>
    /// Appends the occurrences whose emitted date falls within the requested window, preserving order.
    /// </summary>
    /// <param name="target">The list the in-range occurrences are appended to.</param>
    /// <param name="occurrences">The year's occurrences, in stored order.</param>
    /// <param name="range">The requested window.</param>
    private static void AppendInRange(List<NotableDate> target, IReadOnlyList<NotableDate> occurrences, DateRange range)
    {
        for (int i = 0; i < occurrences.Count; i++)
        {
            NotableDate occurrence = occurrences[i];
            if (occurrence.Date >= range.StartDate && occurrence.Date <= range.EndDate)
                target.Add(occurrence);
        }
    }

    /// <summary>
    /// Returns the occurrences ordered by emitted date then identity, verifying the expected order with a linear scan
    /// and sorting only when a non-conforming cache backend violated the ordering contract.
    /// </summary>
    /// <param name="occurrences">The assembled occurrences.</param>
    /// <returns>
    /// <paramref name="occurrences" /> itself when already ordered — the common case for every conforming backend — or
    /// a sorted copy otherwise.
    /// </returns>
    private static IReadOnlyList<NotableDate> EnsureOrdered(IReadOnlyList<NotableDate> occurrences)
    {
        for (int i = 1; i < occurrences.Count; i++)
        {
            if (Compare(occurrences[i - 1], occurrences[i]) > 0)
            {
                var sorted = new List<NotableDate>(occurrences);
                sorted.Sort(Compare);
                return sorted;
            }
        }

        return occurrences;
    }

    /// <summary>
    /// Compares two occurrences by emitted date, then notable-date id, then rule id, matching the wrapped service's
    /// output ordering.
    /// </summary>
    /// <param name="left">The first occurrence.</param>
    /// <param name="right">The second occurrence.</param>
    /// <returns>A signed comparison result.</returns>
    private static int Compare(NotableDate left, NotableDate right)
    {
        int result = left.Date.CompareTo(right.Date);
        if (result != 0)
            return result;

        result = string.CompareOrdinal(left.NotableDateId, right.NotableDateId);
        return result != 0 ? result : string.CompareOrdinal(left.RuleId, right.RuleId);
    }
}
