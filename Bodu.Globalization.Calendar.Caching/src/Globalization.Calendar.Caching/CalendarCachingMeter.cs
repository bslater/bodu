// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarCachingMeter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.Metrics;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Emits the caching metrics for this assembly through a process-wide <see cref="Meter" /> named
/// <c>Bodu.Globalization.Calendar.Caching</c>: cache hits and misses and coalesced in-flight computations (tagged by
/// normalized territory) and swallowed storage failures (tagged by operation).
/// </summary>
/// <remarks>
/// <para>
/// The instruments complement — never replace — the existing log messages: logs carry per-event detail while the
/// counters make hit ratio, stampede pressure, and failure rate measurable. Storage-failure counts increment on <em>every</em>
/// swallowed failure, deliberately outside the rate-limited warning gate that throttles log volume, so sustained
/// degradation is quantifiable even while its logging is suppressed.
/// </para>
/// <para>
/// With no listener attached a counter add is a no-op branch, so the instrumentation costs nothing on the hot path; tag
/// values are normalized ISO territory codes and fixed operation literals, so cardinality stays bounded.
/// </para>
/// </remarks>
internal static class CalendarCachingMeter
{
    /// <summary>The meter name listeners subscribe to.</summary>
    public const string MeterName = "Bodu.Globalization.Calendar.Caching";

    /// <summary>The process-wide meter the instruments belong to.</summary>
    private static readonly Meter s_meter = new(MeterName);

    /// <summary>Counts years served from the cache.</summary>
    private static readonly Counter<long> s_hits = s_meter.CreateCounter<long>(
        "bodu.calendar.notable_date_cache.hits",
        unit: "{year}",
        description: "Notable-date years served from the cache.");

    /// <summary>Counts years recomputed on a cache miss.</summary>
    private static readonly Counter<long> s_misses = s_meter.CreateCounter<long>(
        "bodu.calendar.notable_date_cache.misses",
        unit: "{year}",
        description: "Notable-date years recomputed on a cache miss.");

    /// <summary>Counts callers that joined an already in-flight computation instead of starting their own.</summary>
    private static readonly Counter<long> s_coalescedFlights = s_meter.CreateCounter<long>(
        "bodu.calendar.notable_date_cache.coalesced_flights",
        unit: "{caller}",
        description: "Concurrent cold-year callers coalesced onto an existing in-flight computation.");

    /// <summary>Counts swallowed best-effort storage failures.</summary>
    private static readonly Counter<long> s_storageFailures = s_meter.CreateCounter<long>(
        "bodu.calendar.notable_date_cache.storage_failures",
        unit: "{failure}",
        description: "Best-effort cache storage failures that were swallowed.");

    /// <summary>Counts background refresh-ahead recomputes, tagged by outcome.</summary>
    private static readonly Counter<long> s_refreshAhead = s_meter.CreateCounter<long>(
        "bodu.calendar.notable_date_cache.refresh_ahead",
        unit: "{refresh}",
        description: "Background refresh-ahead recomputes triggered by aged cache hits.");

    /// <summary>
    /// Records a year served from the cache.
    /// </summary>
    /// <param name="territory">The normalized territory the year belongs to.</param>
    public static void CacheHit(string territory)
    {
        if (s_hits.Enabled)
            s_hits.Add(1, new KeyValuePair<string, object?>("territory", NotableDateCacheRules.NormalizeTerritory(territory)));
    }

    /// <summary>
    /// Records a year recomputed on a cache miss.
    /// </summary>
    /// <param name="territory">The normalized territory the year belongs to.</param>
    public static void CacheMiss(string territory)
    {
        if (s_misses.Enabled)
            s_misses.Add(1, new KeyValuePair<string, object?>("territory", NotableDateCacheRules.NormalizeTerritory(territory)));
    }

    /// <summary>
    /// Records a caller that joined an already in-flight computation for its year rather than starting a duplicate.
    /// </summary>
    /// <param name="territory">The normalized territory the coalesced computation belongs to.</param>
    /// <remarks>
    /// The count is approximate under race: a joiner is detected by probing the in-flight map before the atomic
    /// get-or-add, so a caller that races the flight's completion may go uncounted. A counter tracking stampede
    /// pressure tolerates that imprecision.
    /// </remarks>
    public static void CoalescedFlight(string territory)
    {
        if (s_coalescedFlights.Enabled)
            s_coalescedFlights.Add(1, new KeyValuePair<string, object?>("territory", NotableDateCacheRules.NormalizeTerritory(territory)));
    }

    /// <summary>
    /// Records a swallowed best-effort storage failure. Called on every swallow, outside any log rate limiting.
    /// </summary>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    public static void StorageFailure(string operation)
    {
        if (s_storageFailures.Enabled)
            s_storageFailures.Add(1, new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Records a completed background refresh-ahead recompute.
    /// </summary>
    /// <param name="territory">The normalized territory the recompute belongs to.</param>
    /// <param name="outcome">The attempt outcome: <c>success</c> or <c>failed</c>.</param>
    /// <remarks>
    /// One count is recorded per scheduled background task; aged hits that joined an already pending recompute are not
    /// counted separately.
    /// </remarks>
    public static void RefreshAhead(string territory, string outcome)
    {
        if (s_refreshAhead.Enabled)
            s_refreshAhead.Add(1, new KeyValuePair<string, object?>("territory", NotableDateCacheRules.NormalizeTerritory(territory)), new KeyValuePair<string, object?>("outcome", outcome));
    }
}
