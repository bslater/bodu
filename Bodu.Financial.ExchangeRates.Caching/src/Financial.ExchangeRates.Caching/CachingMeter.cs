// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingMeter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.Metrics;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Emits the caching metrics for this assembly through a process-wide <see cref="Meter" /> named
/// <c>Bodu.Financial.ExchangeRates.Caching</c>: cache hits and misses (tagged by provider and lookup shape) and
/// swallowed storage failures (tagged by provider and operation).
/// </summary>
/// <remarks>
/// <para>
/// The instruments complement — never replace — the existing log messages: logs carry per-event detail while the
/// counters make hit ratio and failure rate measurable. Storage-failure counts increment on <em>every</em> swallowed
/// failure, deliberately outside the rate-limited warning gate that throttles log volume, so sustained degradation is
/// quantifiable even while its logging is suppressed.
/// </para>
/// <para>
/// With no listener attached a counter add is a no-op branch, so the instrumentation costs nothing on the hot path; tag
/// values are drawn from small fixed sets (provider names, <c>single</c>/<c>range</c>, operation literals) so
/// cardinality stays bounded.
/// </para>
/// </remarks>
internal static class CachingMeter
{
    /// <summary>The meter name listeners subscribe to.</summary>
    public const string MeterName = "Bodu.Financial.ExchangeRates.Caching";

    /// <summary>The process-wide meter the instruments belong to.</summary>
    private static readonly Meter s_meter = new(MeterName);

    /// <summary>Counts lookups served from the cache.</summary>
    private static readonly Counter<long> s_hits = s_meter.CreateCounter<long>(
        "bodu.financial.rate_cache.hits",
        unit: "{lookup}",
        description: "Rate lookups served from the cache.");

    /// <summary>Counts lookups that missed the cache and were delegated to the inner provider.</summary>
    private static readonly Counter<long> s_misses = s_meter.CreateCounter<long>(
        "bodu.financial.rate_cache.misses",
        unit: "{lookup}",
        description: "Rate lookups that missed the cache and were refetched.");

    /// <summary>Counts swallowed best-effort storage failures.</summary>
    private static readonly Counter<long> s_storageFailures = s_meter.CreateCounter<long>(
        "bodu.financial.rate_cache.storage_failures",
        unit: "{failure}",
        description: "Best-effort cache storage failures that were swallowed.");

    /// <summary>Counts background refresh-ahead attempts, tagged by outcome.</summary>
    private static readonly Counter<long> s_refreshAhead = s_meter.CreateCounter<long>(
        "bodu.financial.rate_cache.refresh_ahead",
        unit: "{refresh}",
        description: "Background refresh-ahead attempts triggered by aged cache hits.");

    /// <summary>
    /// Records a lookup served from the cache.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    /// <param name="lookup">The lookup shape: <c>single</c> or <c>range</c>.</param>
    public static void CacheHit(string provider, string lookup)
    {
        if (s_hits.Enabled)
            s_hits.Add(1, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("lookup", lookup));
    }

    /// <summary>
    /// Records a lookup that missed the cache.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    /// <param name="lookup">The lookup shape: <c>single</c> or <c>range</c>.</param>
    public static void CacheMiss(string provider, string lookup)
    {
        if (s_misses.Enabled)
            s_misses.Add(1, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("lookup", lookup));
    }

    /// <summary>
    /// Records a swallowed best-effort storage failure. Called on every swallow, outside any log rate limiting.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    public static void StorageFailure(string provider, string operation)
    {
        if (s_storageFailures.Enabled)
            s_storageFailures.Add(1, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Records a completed background refresh-ahead attempt.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    /// <param name="outcome">The attempt outcome: <c>success</c> or <c>failed</c>.</param>
    /// <remarks>
    /// One count is recorded per scheduled background task; aged hits that joined an already pending refresh are not
    /// counted separately.
    /// </remarks>
    public static void RefreshAhead(string provider, string outcome)
    {
        if (s_refreshAhead.Enabled)
            s_refreshAhead.Add(1, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("outcome", outcome));
    }
}
