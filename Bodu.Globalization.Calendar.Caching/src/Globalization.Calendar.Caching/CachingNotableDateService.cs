// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using Bodu.Caching;
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

    /// <summary>The in-flight year computations, keyed by normalized territory, year, and version, so concurrent misses for the same cold year coalesce onto one computation instead of stampeding the wrapped service.</summary>
    private readonly ConcurrentDictionary<(string Territory, int Year, string Version), Lazy<IReadOnlyList<NotableDate>>> _inFlight = new();

    /// <summary>The pending background refresh-ahead recomputes, keyed like <see cref="_inFlight" />, so concurrent aged hits for the same year join the one pending recompute rather than duplicating it. Entries are removed when their task completes.</summary>
    private readonly ConcurrentDictionary<(string Territory, int Year, string Version), Task> _pendingRefreshAhead = new();

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

        // Jitter is keyed by the normalized territory (not territory+year) so the batch and per-year read paths,
        // which share this one time-to-live for the whole span, always agree on freshness.
        TimeSpan ttl = _options.TtlJitter > 0
            ? CacheFreshness.WithJitter(_options.Ttl, NotableDateCacheRules.NormalizeTerritory(territory), _options.TtlJitter)
            : _options.Ttl;

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
                    CalendarCachingMeter.CacheHit(territory);
                    MaybeScheduleRefreshAhead(territory, year, version, ttl, entry.ComputedAtUtc, now);
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

        IReadOnlyList<NotableDate> unfiltered = Resolve(range, territory);
        if (unfiltered.Count == 0)
            return unfiltered;

        List<NotableDate> matched = new(unfiltered.Count);
        for (int i = 0; i < unfiltered.Count; i++)
        {
            if (filter.Matches(unfiltered[i]))
                matched.Add(unfiltered[i]);
        }

        // Everything matched: hand back the unfiltered list itself — the unfiltered overload already returns shared
        // cached lists, so this exposes nothing new.
        return matched.Count == unfiltered.Count ? unfiltered : matched;
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
            CalendarCachingMeter.CacheHit(territory);
            MaybeScheduleRefreshAhead(territory, year, version, ttl, entry.ComputedAtUtc, now);
            return entry.Occurrences;
        }

        return ComputeAndStoreYear(territory, year, version, ttl, now);
    }

    /// <summary>
    /// Computes one civil year through the wrapped service and writes it to the cache — the shared miss path of the
    /// per-year and batch read branches — coalescing concurrent misses for the same cold year onto one computation.
    /// </summary>
    /// <param name="territory">The requested territory code, passed through unmodified; the cache normalizes it.</param>
    /// <param name="year">The civil year to compute.</param>
    /// <param name="version">The resource version token the entry is keyed by.</param>
    /// <param name="ttl">The time-to-live the write prunes against.</param>
    /// <param name="now">The instant the computed year is stamped with.</param>
    /// <param name="refreshAhead">
    /// <see langword="true" /> when the computation was triggered by a background refresh-ahead rather than a genuine
    /// miss, selecting the refresh-ahead diagnostics instead of the miss log and counter.
    /// </param>
    /// <returns>The computed occurrences, unfiltered, in the wrapped service's order.</returns>
    /// <remarks>
    /// The wrapped service has no coalescing of its own (year resolution is a synchronous, CPU-bound computation), so
    /// without this guard N concurrent misses for the same cold year would run N identical computations. The first
    /// caller for a key computes and stores; concurrent callers share the same <see cref="Lazy{T}" /> result — including
    /// its exception when the computation faults. The in-flight entry is removed once the value is realized, so a
    /// faulted computation never poisons the key and the next caller retries fresh. Background refresh-ahead recomputes
    /// share this single-flight guard, so a genuine miss that arrives while a refresh is computing joins the refresh
    /// flight and is served the refreshed value — such a joining caller is counted as neither a hit nor a miss, because
    /// the creator of the flight determines which diagnostics its computation emits.
    /// </remarks>
    private IReadOnlyList<NotableDate> ComputeAndStoreYear(string territory, int year, string version, TimeSpan ttl, DateTimeOffset now, bool refreshAhead = false)
    {
        (string Territory, int Year, string Version) key = (NotableDateCacheRules.NormalizeTerritory(territory), year, version);

        // Approximate coalesce detection: a probe that finds an existing flight means this caller will join it
        // rather than compute. Racing the flight's completion may undercount, which a pressure counter tolerates.
        if (_inFlight.ContainsKey(key))
            CalendarCachingMeter.CoalescedFlight(key.Territory);

        Lazy<IReadOnlyList<NotableDate>> flight = _inFlight.GetOrAdd(
            key,
            _ => new Lazy<IReadOnlyList<NotableDate>>(
                () =>
                {
                    IReadOnlyList<NotableDate> occurrences =
                        _inner.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory);

                    _cache.StoreYear(new NotableDateCacheEntry(territory, year, version, occurrences, now), ttl, now);

                    if (refreshAhead)
                    {
                        Log.RefreshAheadRecomputed(_logger, _options.CacheMissLogLevel, territory, year, occurrences.Count);
                    }
                    else
                    {
                        Log.CacheMiss(_logger, _options.CacheMissLogLevel, territory, year, occurrences.Count);
                        CalendarCachingMeter.CacheMiss(territory);
                    }

                    return occurrences;
                },
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return flight.Value;
        }
        finally
        {
            // Remove the completed (or faulted) flight so later misses — after expiry or a failure — start fresh
            // rather than observing a cached Lazy exception forever.
            _inFlight.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Schedules a background refresh-ahead recompute of a served year when refresh-ahead is enabled and the served
    /// entry has aged past the configured fraction of the effective time-to-live.
    /// </summary>
    /// <param name="territory">The requested territory code, passed through unmodified; the key normalizes it.</param>
    /// <param name="year">The served civil year.</param>
    /// <param name="version">The resource version token in effect at the serving lookup.</param>
    /// <param name="ttl">The effective (post-jitter) time-to-live the serving lookup evaluated freshness against.</param>
    /// <param name="computedAtUtc">The instant the served entry was computed.</param>
    /// <param name="now">The serving lookup's instant.</param>
    /// <remarks>
    /// The pending registration is published <em>before</em> the task starts — a completion source's task is added
    /// under the key first, and only a successful add spawns the worker — so a concurrent aged hit can never slip in
    /// between a worker finishing and its registration appearing. The worker captures the schedule-time version token
    /// (a mid-flight resource reload merely writes an entry under the stale version, which is never read again) but
    /// takes a fresh computation instant when it runs, and routes through
    /// <see cref="ComputeAndStoreYear" /> so it shares the single-flight guard with genuine misses. Failures are
    /// swallowed after logging: the hit that triggered the recompute was already served, and the next aged hit
    /// schedules a fresh attempt. Disposal is re-checked when the worker starts; pending recomputes are abandoned, not
    /// drained, by <see cref="Dispose" />.
    /// </remarks>
    private void MaybeScheduleRefreshAhead(string territory, int year, string version, TimeSpan ttl, DateTimeOffset computedAtUtc, DateTimeOffset now)
    {
        if (_options.RefreshAheadFraction <= 0 || _disposed)
            return;

        // Because the fraction is below one, the threshold always precedes expiry, so a continuously hot territory
        // refreshes before it can ever miss.
        var threshold = TimeSpan.FromTicks((long)(ttl.Ticks * _options.RefreshAheadFraction));
        if (now - computedAtUtc < threshold)
            return;

        (string Territory, int Year, string Version) key = (NotableDateCacheRules.NormalizeTerritory(territory), year, version);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRefreshAhead.TryAdd(key, completion.Task))
            return;

        // Fire-and-forget by design: the task is observed through the pending registry (and the test hook), and its
        // body routes every outcome to the meter and log rather than throwing.
        _ = Task.Run(() =>
        {
            try
            {
                if (_disposed)
                    return;

                DateTimeOffset freshNow = _timeProvider.GetUtcNow();
                _ = ComputeAndStoreYear(territory, year, version, ttl, freshNow, refreshAhead: true);
                CalendarCachingMeter.RefreshAhead(territory, "success");
            }
            catch (Exception ex)
            {
                CalendarCachingMeter.RefreshAhead(territory, "failed");
                Log.RefreshAheadFailed(_logger, territory, year, ex);
            }
            finally
            {
                _pendingRefreshAhead.TryRemove(key, out _);
                completion.SetResult();
            }
        });
    }

    /// <summary>
    /// Waits for every currently pending background refresh-ahead recompute to complete, so tests can
    /// deterministically observe refresh-ahead side effects. Recomputes are registered synchronously inside the
    /// serving call, so a pending recompute scheduled by a completed resolution is always visible here.
    /// </summary>
    /// <returns>A task that completes when every recompute pending at the call instant has finished.</returns>
    internal Task WaitForRefreshAheadForTestingAsync() =>
        Task.WhenAll(_pendingRefreshAhead.Values.ToArray());

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
