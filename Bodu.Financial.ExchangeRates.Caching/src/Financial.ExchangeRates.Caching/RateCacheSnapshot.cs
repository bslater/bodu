// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheSnapshot.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The result of a single-read snapshot of one pair's persisted cache state: the raw stored rows together with the
/// freshness-evaluated coverage, as produced by an <see cref="IRateCacheSnapshotReader" />.
/// </summary>
/// <remarks>
/// The rows are deliberately raw — unfiltered and in stored order — so a coverage miss pays no filtering cost; the
/// consumer applies <see cref="RateCacheRules.SelectFresh" /> only when the coverage confirms the window is served.
/// The coverage, by contrast, is already freshness-evaluated because its only use is the containment probe.
/// </remarks>
internal readonly struct RateCacheSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateCacheSnapshot" /> struct.
    /// </summary>
    /// <param name="rows">The raw stored rows for the pair, unfiltered.</param>
    /// <param name="coverage">The freshness-evaluated coverage for the pair.</param>
    public RateCacheSnapshot(IReadOnlyList<CachedRate> rows, DateRangeCoverage coverage)
    {
        Rows = rows;
        Coverage = coverage;
    }

    /// <summary>
    /// Gets the raw stored rows for the pair, unfiltered and in stored order.
    /// </summary>
    /// <value>The stored rows, possibly empty.</value>
    public IReadOnlyList<CachedRate> Rows { get; }

    /// <summary>
    /// Gets the freshness-evaluated coverage for the pair.
    /// </summary>
    /// <value>The coverage describing the days known to have been fetched and still fresh.</value>
    public DateRangeCoverage Coverage { get; }
}
