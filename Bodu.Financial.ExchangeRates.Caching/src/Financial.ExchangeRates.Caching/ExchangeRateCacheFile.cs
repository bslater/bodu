// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The root TOML table for a cache file, wrapping the array of cached rate rows. TOML requires the document root to be
/// a table, so the entries are nested under a named array-of-tables rather than serialized as a bare array.
/// </summary>
public sealed class ExchangeRateCacheFile
{
    /// <summary>
    /// Gets or sets the cached rate rows, serialized as a TOML array of tables.
    /// </summary>
    /// <returns>The cached rate rows.</returns>
    public List<ExchangeRateCacheEntry> Entries { get; set; } = new();
}
