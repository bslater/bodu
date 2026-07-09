// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// The root TOML table for a cache file, wrapping the array of cached rate rows together with the array of recorded
/// coverage windows. TOML requires the document root to be a table, so each collection is nested under a named
/// array-of-tables rather than serialized as a bare array.
/// </summary>
public sealed class RateCacheFile
{
    /// <summary>
    /// Gets or sets the name of the provider whose rates the file holds.
    /// </summary>
    /// <value>
    /// The provider identifier, or <see langword="null" /> in a file written before this field was recorded.
    /// </value>
    /// <remarks>
    /// Recorded as a top-level key so a file identifies its own provider and currency pair, rather than relying solely
    /// on the file name and directory layout. A cache file written before these keys were recorded simply has none of
    /// them and deserializes them to <see langword="null" />, so older caches remain readable.
    /// </remarks>
    public string? Provider { get; set; }

    /// <summary>
    /// Gets or sets the source currency of the pair the file holds.
    /// </summary>
    /// <value>
    /// The source currency code, or <see langword="null" /> in a file written before this field was recorded.
    /// </value>
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the destination currency of the pair the file holds.
    /// </summary>
    /// <value>
    /// The destination currency code, or <see langword="null" /> in a file written before this field was recorded.
    /// </value>
    public string? To { get; set; }

    /// <summary>
    /// Gets or sets the cached rate rows, serialized as a TOML array of tables.
    /// </summary>
    /// <value>The cached rate rows.</value>
    public List<RateCacheEntry> Entries { get; set; } = new();

    /// <summary>
    /// Gets or sets the recorded coverage windows, serialized as a TOML array of tables.
    /// </summary>
    /// <value>The coverage windows recording which date ranges were fetched.</value>
    /// <remarks>
    /// A cache file written before coverage was tracked has no coverage array; it deserializes to an empty list so old
    /// caches keep working, with ranges simply refetched until coverage is recorded.
    /// </remarks>
    public List<RateCacheCoverageEntry> Coverage { get; set; } = new();
}
