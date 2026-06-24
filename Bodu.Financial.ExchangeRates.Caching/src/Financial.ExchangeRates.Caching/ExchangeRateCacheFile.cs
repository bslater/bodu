// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The root TOML table for a cache file, wrapping the array of cached rate rows together with the array of recorded
/// coverage windows. TOML requires the document root to be a table, so each collection is nested under a named
/// array-of-tables rather than serialized as a bare array.
/// </summary>
public sealed class ExchangeRateCacheFile
{
    /// <summary>
    /// Gets or sets the cached rate rows, serialized as a TOML array of tables.
    /// </summary>
    /// <value>The cached rate rows.</value>
    public List<ExchangeRateCacheEntry> Entries { get; set; } = new();

    /// <summary>
    /// Gets or sets the recorded coverage windows, serialized as a TOML array of tables.
    /// </summary>
    /// <value>The coverage windows recording which date ranges were fetched.</value>
    /// <remarks>
    /// A cache file written before coverage was tracked has no coverage array; it deserializes to an empty list so old
    /// caches keep working, with ranges simply refetched until coverage is recorded.
    /// </remarks>
    public List<ExchangeRateCacheCoverageEntry> Coverage { get; set; } = new();
}
