// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBoeResponseCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Caches the raw bytes of downloaded Bank of England IADB range responses so an identical range need not be
/// re-fetched.
/// </summary>
/// <remarks>
/// A response is keyed by the inclusive date range that produced it. Because a range that ends on or near the current
/// date can gain a new observation each business day, a cached entry is treated as stale once it is older than a
/// caller-supplied refresh interval. Implementations are expected to be resilient: a cache failure should manifest as a
/// miss rather than an exception that breaks rate retrieval.
/// </remarks>
public interface IBoeResponseCache
{
    /// <summary>
    /// Attempts to retrieve the cached bytes for a range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="refreshInterval">The maximum age at which a cached response is still considered fresh.</param>
    /// <param name="bytes">
    /// When this method returns <see langword="true" />, the cached response bytes; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when a fresh cache entry exists; otherwise <see langword="false" />.</returns>
    bool TryGet(DateOnly startDate, DateOnly endDate, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes);

    /// <summary>
    /// Stores the bytes for a range, replacing any existing entry.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="bytes">The response bytes to store.</param>
    void Store(DateOnly startDate, DateOnly endDate, byte[] bytes);
}
