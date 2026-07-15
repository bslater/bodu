// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// The root table for a persisted notable-date cache file, holding one territory's cached years as a flat pair of
/// arrays: the per-year entry metadata and the occurrences that belong to those years.
/// </summary>
/// <remarks>
/// The occurrences are stored flat rather than nested under each year — each occurrence row carries its own
/// <see cref="NotableDateCacheOccurrenceRow.Year" /> and <see cref="NotableDateCacheOccurrenceRow.Version" /> — so the
/// document is a shallow structure that both the TOML and JSON serializers round-trip without nested arrays of tables.
/// A cached year that yielded no occurrences appears only in <see cref="Entries" /> with no matching occurrence rows.
/// </remarks>
public sealed class NotableDateCacheFile
{
    /// <summary>
    /// Gets or sets the territory whose cached years the file holds.
    /// </summary>
    /// <value>The normalized territory code, or <see langword="null" /> in a malformed file.</value>
    public string? Territory { get; set; }

    /// <summary>
    /// Gets or sets the per-year cache entry metadata, serialized as an array of tables.
    /// </summary>
    /// <value>The per-year entries, one per cached civil year.</value>
    public List<NotableDateCacheYearRow> Entries { get; set; } = new();

    /// <summary>
    /// Gets or sets the cached occurrences, serialized as an array of tables and associated back to their year by the
    /// occurrence row's own year and version.
    /// </summary>
    /// <value>The cached occurrences across every year the file holds.</value>
    public List<NotableDateCacheOccurrenceRow> Occurrences { get; set; } = new();
}
