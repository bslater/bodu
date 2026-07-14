// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingSqliteMeter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.Metrics;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Emits the caching metrics for this assembly through a process-wide <see cref="Meter" /> named
/// <c>Bodu.Financial.ExchangeRates.Caching.Sqlite</c>: swallowed best-effort storage failures, tagged by provider and operation.
/// </summary>
/// <remarks>
/// The counter complements — never replaces — the existing log messages, and increments on <em>every</em> swallowed
/// failure, deliberately outside the rate-limited warning gate that throttles log volume, so sustained degradation is
/// quantifiable even while its logging is suppressed. With no listener attached the add is a no-op branch.
/// </remarks>
internal static class CachingSqliteMeter
{
    /// <summary>The meter name listeners subscribe to.</summary>
    public const string MeterName = "Bodu.Financial.ExchangeRates.Caching.Sqlite";

    /// <summary>The process-wide meter the instrument belongs to.</summary>
    private static readonly Meter s_meter = new(MeterName);

    /// <summary>Counts swallowed best-effort storage failures.</summary>
    private static readonly Counter<long> s_storageFailures = s_meter.CreateCounter<long>(
        "bodu.financial.rate_cache.sqlite.storage_failures",
        unit: "{failure}",
        description: "Best-effort cache storage failures that were swallowed.");

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
}
