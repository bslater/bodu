// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IImfReportCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Caches the raw bytes of downloaded IMF monthly report files so they need not be re-fetched on every load.
/// </summary>
/// <remarks>
/// A report for a closed month is immutable, so a cached entry for a past month is always fresh; the report for the
/// current month is still accumulating business days, so it is treated as stale once it is older than a caller-supplied
/// refresh interval. Implementations are expected to be resilient: a cache failure should manifest as a miss rather
/// than an exception that breaks rate retrieval.
/// </remarks>
public interface IImfReportCache
{
    /// <summary>
    /// Attempts to retrieve the cached bytes for a report month.
    /// </summary>
    /// <param name="month">The report month whose file is requested.</param>
    /// <param name="refreshInterval">The maximum age at which a current-month report is still considered fresh.</param>
    /// <param name="bytes">
    /// When this method returns <see langword="true" />, the cached report bytes; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when a fresh cache entry exists; otherwise <see langword="false" />.</returns>
    bool TryGet(ImfReportMonth month, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes);

    /// <summary>
    /// Stores the bytes for a report month, replacing any existing entry.
    /// </summary>
    /// <param name="month">The report month whose file is being cached.</param>
    /// <param name="bytes">The report bytes to store.</param>
    void Store(ImfReportMonth month, byte[] bytes);
}
