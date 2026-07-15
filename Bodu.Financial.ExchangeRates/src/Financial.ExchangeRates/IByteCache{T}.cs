// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IByteCache{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Caches the raw bytes of a downloaded response so an identical download unit need not be re-fetched.
/// </summary>
/// <typeparam name="TKey">The type identifying a cached download unit (a date range, feed, era, and so on).</typeparam>
/// <remarks>
/// A response is keyed by the download unit that produced it. Because a unit that covers the recent past can gain a new
/// observation over time, a cached entry is treated as stale once it is older than a caller-supplied refresh interval.
/// Implementations are expected to be resilient: a cache failure should manifest as a miss rather than an exception
/// that breaks rate retrieval.
/// </remarks>
public interface IByteCache<TKey>
{
    /// <summary>
    /// Attempts to retrieve the cached bytes for a download unit.
    /// </summary>
    /// <param name="key">The download unit to look up.</param>
    /// <param name="refreshInterval">The maximum age at which a cached response is still considered fresh.</param>
    /// <param name="bytes">
    /// When this method returns <see langword="true" />, the cached response bytes; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when a fresh cache entry exists; otherwise <see langword="false" />.</returns>
    bool TryGet(TKey key, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes);

    /// <summary>
    /// Stores the bytes for a download unit, replacing any existing entry.
    /// </summary>
    /// <param name="key">The download unit the bytes belong to.</param>
    /// <param name="bytes">The response bytes to store.</param>
    void Store(TKey key, byte[] bytes);
}
