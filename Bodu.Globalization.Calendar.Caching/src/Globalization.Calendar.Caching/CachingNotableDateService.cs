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
/// same year is served without recomputing.
/// </para>
/// <para>
/// The filtered overloads apply the filter after assembling the unfiltered result, exactly as the wrapped service does,
/// so a <see cref="NotableDateFilter" /> never participates in the cache key. The discovery methods delegate straight to
/// the wrapped service.
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

    /// <summary>Guards the resource-version generation tracking.</summary>
    private readonly object _versionLock = new();

    /// <summary>The resource observed on the previous version resolution, tracked by reference identity to detect a reload.</summary>
    private NotableDateResource? _lastResource;

    /// <summary>The monotonic reload generation, bumped whenever the observed resource reference changes.</summary>
    private long _generation;

    /// <summary>The version token derived for the current observed resource.</summary>
    private string? _versionToken;

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

        List<NotableDate> assembled = new();
        for (int year = range.StartDate.Year; year <= range.EndDate.Year; year++)
            assembled.AddRange(ResolveYear(territory, year, version, ttl, now));

        return [.. assembled
            .Where(occurrence => occurrence.Date >= range.StartDate && occurrence.Date <= range.EndDate)
            .OrderBy(occurrence => occurrence.Date)
            .ThenBy(occurrence => occurrence.NotableDateId, StringComparer.Ordinal)
            .ThenBy(occurrence => occurrence.RuleId, StringComparer.Ordinal)];
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
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The civil year to resolve.</param>
    /// <param name="version">The resource version token entries are keyed by.</param>
    /// <param name="ttl">The time-to-live freshness is evaluated against.</param>
    /// <param name="now">The lookup instant.</param>
    /// <returns>The occurrences emitted within the year, unfiltered.</returns>
    private IReadOnlyList<NotableDate> ResolveYear(string territory, int year, string version, TimeSpan ttl, DateTimeOffset now)
    {
        NotableDateCacheEntry? entry = _cache.GetYear(territory, year, version, ttl, now);
        if (entry is not null)
        {
            Log.CacheHit(_logger, _options.CacheHitLogLevel, territory, year, (now - entry.ComputedAtUtc).TotalSeconds);
            return entry.Occurrences;
        }

        IReadOnlyList<NotableDate> occurrences =
            _inner.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory);

        string normalized = NotableDateCacheRules.NormalizeTerritory(territory);
        _cache.StoreYear(new NotableDateCacheEntry(normalized, year, version, occurrences, now), ttl, now);

        Log.CacheMiss(_logger, _options.CacheMissLogLevel, territory, year, occurrences.Count);
        return occurrences;
    }

    /// <summary>
    /// Resolves the resource-version token, deriving it from the observed resource identity and reload generation when a
    /// provider is present, or from the fixed options token otherwise.
    /// </summary>
    /// <returns>The current resource-version token.</returns>
    private string ResolveVersion()
    {
        if (_versionSource is null)
            return _options.ResourceVersion ?? DefaultVersionToken;

        NotableDateResource current = _versionSource.Current;

        lock (_versionLock)
        {
            // A reload swaps the resource reference; bump the generation so the token — and therefore every cache key —
            // changes even when the new resource carries the same identifier and schema version as the old one.
            if (!ReferenceEquals(current, _lastResource))
            {
                _lastResource = current;
                _generation++;
                _versionToken = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}|{1}|{2}",
                    current.ResourceId,
                    current.SchemaVersion,
                    _generation);
            }

            return _versionToken!;
        }
    }
}
