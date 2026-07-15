// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarCachingDistributedMeter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.Metrics;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Emits the caching metrics for this assembly through a process-wide <see cref="Meter" /> named
/// <c>Bodu.Globalization.Calendar.Caching.Distributed</c>: swallowed best-effort storage failures, tagged by operation.
/// </summary>
/// <remarks>
/// The counter complements — never replaces — the existing log messages, and increments on <em>every</em> swallowed
/// failure, deliberately outside the rate-limited warning gate that throttles log volume, so sustained degradation is
/// quantifiable even while its logging is suppressed. With no listener attached the add is a no-op branch.
/// </remarks>
internal static class CalendarCachingDistributedMeter
{
    /// <summary>The meter name listeners subscribe to.</summary>
    public const string MeterName = "Bodu.Globalization.Calendar.Caching.Distributed";

    /// <summary>The process-wide meter the instrument belongs to.</summary>
    private static readonly Meter s_meter = new(MeterName);

    /// <summary>Counts swallowed best-effort storage failures.</summary>
    private static readonly Counter<long> s_storageFailures = s_meter.CreateCounter<long>(
        "bodu.calendar.notable_date_cache.distributed.storage_failures",
        unit: "{failure}",
        description: "Best-effort cache storage failures that were swallowed.");

    /// <summary>
    /// Records a swallowed best-effort storage failure. Called on every swallow, outside any log rate limiting.
    /// </summary>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    public static void StorageFailure(string operation)
    {
        if (s_storageFailures.Enabled)
            s_storageFailures.Add(1, new KeyValuePair<string, object?>("operation", operation));
    }
}
