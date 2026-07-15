// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IRateCacheSnapshotReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An optional read seam for <see cref="IRateCache" /> backends whose rate rows and coverage windows live in one
/// physical unit, letting the caching decorator's range path obtain both halves from a single state read instead of
/// separate <see cref="IRateCache.GetCoverage" /> and <see cref="IRateCache.GetRates" /> calls.
/// </summary>
/// <remarks>
/// <para>
/// A range lookup needs the pair's coverage first and its rows only on a coverage hit. For a backend that persists both
/// halves together — the in-memory and file caches' single per-pair state, or the distributed cache's single per-pair
/// blob — answering those as two separate calls reads and deserializes the same state twice. Implementing this seam
/// lets the decorator read once: the snapshot carries the <em>raw, unfiltered</em> rows (so a coverage miss pays no
/// freshness filtering) together with the freshness-evaluated coverage, and the decorator applies
/// <see cref="RateCacheRules.SelectFresh" /> to the rows only when the window is actually served.
/// </para>
/// <para>
/// The seam is deliberately optional. A backend whose halves are independently addressable — the SQLite cache's two
/// tables — is <em>better off without it</em>: its coverage-first probe reads the rows only on a hit, which an eager
/// combined read would forfeit. The decorator falls back to the standard <see cref="IRateCache" /> calls for any cache
/// that does not implement the seam, so third-party backends are unaffected.
/// </para>
/// </remarks>
internal interface IRateCacheSnapshotReader
{
    /// <summary>
    /// Reads the pair's raw rows and freshness-evaluated coverage from one state read.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="duration">The duration a recorded coverage window remains fresh after it was fetched.</param>
    /// <param name="asOf">The instant against which coverage freshness is evaluated.</param>
    /// <returns>The snapshot of the pair's persisted state.</returns>
    RateCacheSnapshot ReadSnapshot(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf);
}
