// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IFileExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Specializes <see cref="IExchangeRateCache" /> for caches that persist rates as files, exposing the storage directory
/// and the file or directory a pair resolves to under the configured layout.
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
    /// Resolves the full path of the single file that backs the supplied pair for this cache's provider.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The resolved file path the pair's rates are read from and written to.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the cache uses a partitioned layout, where a pair has no single backing file; use
    /// <see cref="ResolveDirectory" /> or <see cref="ResolvePartitionPath" /> instead.
    /// </exception>
    string ResolveFilePath(CurrencyPair pair);

    /// <summary>
    /// Resolves the directory that holds the supplied pair's cache file or files for this cache's provider.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The directory the pair's rates are stored in.</returns>
    string ResolveDirectory(CurrencyPair pair);

    /// <summary>
    /// Resolves the full path of the file that backs the supplied pair's rates for the partition containing
    /// <paramref name="date" />.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="date">A date within the partition whose file is required.</param>
    /// <returns>
    /// The resolved partition file path. For a single-file layout this is the pair's one file regardless of
    /// <paramref name="date" />.
    /// </returns>
    string ResolvePartitionPath(CurrencyPair pair, DateOnly date);
}
