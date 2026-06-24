// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IFileExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Specializes <see cref="IExchangeRateCache" /> for caches that persist rates as files, exposing the storage directory
/// and the per-pair file path the cache resolves to.
/// </summary>
/// <remarks>
/// This is the file-storage seam of the caching design. Other storage kinds define their own specialization of
/// <see cref="IExchangeRateCache" /> (for example a database cache exposing a table or connection name); consumers that
/// only need the storage-agnostic contract continue to depend on <see cref="IExchangeRateCache" />.
/// </remarks>
public interface IFileExchangeRateCache
    : IExchangeRateCache
{
    /// <summary>
    /// Gets the directory in which cached rate files are stored.
    /// </summary>
    /// <value>The absolute or relative cache directory path.</value>
    string CacheDirectory { get; }

    /// <summary>
    /// Resolves the full path of the file that backs the supplied pair for this cache's provider.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The resolved file path the pair's rates are read from and written to.</returns>
    string ResolveFilePath(ExchangeRatePair pair);
}
