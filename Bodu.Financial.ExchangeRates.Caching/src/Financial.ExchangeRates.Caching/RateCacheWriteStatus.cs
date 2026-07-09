// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheWriteStatus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Reports the outcome of an atomic
/// <see cref="IRateCache.StoreFetchedRange(CurrencyPair, IReadOnlyList{CachedRate}, DateOnly, DateOnly, TimeSpan, DateTimeOffset)" />
/// write, distinguishing a durable success from a swallowed storage failure and from a deliberate no-op cache.
/// </summary>
/// <remarks>
/// The status exists so a caller — most notably the caching decorator — can tell whether the rows and coverage window
/// were actually persisted. A <see cref="Failed" /> result signals that nothing was persisted (a backing-store error
/// was swallowed to keep the cache best-effort), so a subsequent range lookup must refetch rather than trust the
/// coverage; this closes the window in which a half-completed write left coverage recorded without its rows.
/// </remarks>
public enum RateCacheWriteStatus
{
    /// <summary>
    /// The rows and coverage window were persisted atomically; both halves are durable.
    /// </summary>
    Stored,

    /// <summary>
    /// The cache intentionally stored nothing, as a no-op cache does; nothing was persisted and nothing was expected to
    /// be.
    /// </summary>
    Skipped,

    /// <summary>
    /// A storage error was swallowed and nothing was persisted; neither the rows nor the coverage window are durable.
    /// </summary>
    Failed,
}
