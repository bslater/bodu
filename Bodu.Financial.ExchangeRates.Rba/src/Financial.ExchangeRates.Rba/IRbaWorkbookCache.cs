// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IRbaWorkbookCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Caches the raw bytes of downloaded RBA workbook files so they need not be re-fetched on every load.
/// </summary>
/// <remarks>
/// Immutable era files (those with a fixed end date) may be cached indefinitely; the open-ended current era is treated
/// as stale once it is older than a caller-supplied refresh interval. Implementations are expected to be resilient: a
/// cache failure should manifest as a miss rather than an exception that breaks rate retrieval.
/// </remarks>
public interface IRbaWorkbookCache
{
    /// <summary>
    /// Attempts to retrieve the cached bytes for an era.
    /// </summary>
    /// <param name="era">The era whose workbook is requested.</param>
    /// <param name="currentEraRefreshInterval">
    /// The maximum age at which the open-ended current era is still considered fresh. Ignored for immutable eras.
    /// </param>
    /// <param name="bytes">
    /// When this method returns <see langword="true" />, the cached workbook bytes; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when a fresh cache entry exists; otherwise <see langword="false" />.</returns>
    bool TryGet(RbaEra era, TimeSpan currentEraRefreshInterval, [MaybeNullWhen(false)] out byte[] bytes);

    /// <summary>
    /// Stores the bytes for an era, replacing any existing entry.
    /// </summary>
    /// <param name="era">The era whose workbook is being cached.</param>
    /// <param name="bytes">The workbook bytes to store.</param>
    void Store(RbaEra era, byte[] bytes);
}
