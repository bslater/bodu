// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IRateCacheAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The asynchronous seam of a rate cache whose backing store supports non-blocking I/O. The caching decorator
/// type-tests its cache for this interface and, when present, routes its asynchronous surfaces through these members
/// so a distributed backend's network I/O never blocks a thread-pool thread.
/// </summary>
/// <remarks>
/// <para>
/// Kept <see langword="internal" /> — like <see cref="IRateCacheSnapshotReader" /> — so the public
/// <see cref="IRateCache" /> contract is unchanged: third-party caches remain synchronous and are served through the
/// decorator's synchronous helpers on both surfaces. <see cref="RateCacheBase{TOptions}" /> implements this interface
/// for every derived backend, defaulting each member to its synchronous counterpart so only backends with genuinely
/// asynchronous storage override anything.
/// </para>
/// <para>
/// Every member applies exactly the same freshness, merge, and coverage policy as its synchronous counterpart; the
/// decorator's two surfaces must never diverge in behaviour, only in how they wait.
/// </para>
/// </remarks>
internal interface IRateCacheAsync
{
    /// <summary>
    /// Asynchronously retrieves the fresh cached rates for a pair, applying the same policy as
    /// <see cref="IRateCache.GetRates" />.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="duration">The duration a cached row remains fresh.</param>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The fresh rows ordered ascending by date; empty when none qualify.</returns>
    ValueTask<IReadOnlyList<CachedRate>> GetRatesAsync(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously reads a pair's rows and fresh coverage from one state read, applying the same policy as
    /// <see cref="IRateCacheSnapshotReader.ReadSnapshot" />.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="duration">The duration rows and coverage windows remain fresh.</param>
    /// <param name="asOf">The instant against which freshness is evaluated.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The snapshot of raw rows and freshness-evaluated coverage.</returns>
    ValueTask<RateCacheSnapshot> ReadSnapshotAsync(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously stores rates for a pair, applying the same merge policy as <see cref="IRateCache.Store" />.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="rates">The rows to store.</param>
    /// <param name="duration">The duration a cached row remains fresh.</param>
    /// <param name="asOf">The instant the write is evaluated at.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task representing the write.</returns>
    ValueTask StoreAsync(CurrencyPair pair, IReadOnlyList<CachedRate> rates, TimeSpan duration, DateTimeOffset asOf, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously stores a fetched range's rows and its covered window atomically, applying the same policy as
    /// <see cref="IRateCache.StoreFetchedRange" />.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="rows">The fetched rows.</param>
    /// <param name="start">The inclusive first date of the covered window.</param>
    /// <param name="end">The inclusive last date of the covered window.</param>
    /// <param name="duration">The duration rows and the coverage window remain fresh.</param>
    /// <param name="asOf">The instant the write is evaluated at.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The persistence outcome, so a swallowed failure surfaces as <see cref="RateCacheWriteStatus.Failed" />.</returns>
    ValueTask<RateCacheWriteStatus> StoreFetchedRangeAsync(CurrencyPair pair, IReadOnlyList<CachedRate> rows, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf, CancellationToken cancellationToken);
}
