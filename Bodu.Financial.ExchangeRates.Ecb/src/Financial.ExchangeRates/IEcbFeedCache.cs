// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEcbFeedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Caches the raw bytes of downloaded ECB feed files so they need not be re-fetched on every load.
/// </summary>
/// <remarks>
/// Every ECB feed extends to the most recent business day, so each cached entry is treated as stale once it is older
/// than a caller-supplied refresh interval. Implementations are expected to be resilient: a cache failure should
/// manifest as a miss rather than an exception that breaks rate retrieval.
/// </remarks>
public interface IEcbFeedCache
{
    /// <summary>
    /// Attempts to retrieve the cached bytes for a feed.
    /// </summary>
    /// <param name="feed">The feed whose file is requested.</param>
    /// <param name="refreshInterval">The maximum age at which a cached feed is still considered fresh.</param>
    /// <param name="bytes">
    /// When this method returns <see langword="true" />, the cached feed bytes; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when a fresh cache entry exists; otherwise <see langword="false" />.</returns>
    bool TryGet(EcbRateFeed feed, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes);

    /// <summary>
    /// Stores the bytes for a feed, replacing any existing entry.
    /// </summary>
    /// <param name="feed">The feed whose file is being cached.</param>
    /// <param name="bytes">The feed bytes to store.</param>
    void Store(EcbRateFeed feed, byte[] bytes);
}
