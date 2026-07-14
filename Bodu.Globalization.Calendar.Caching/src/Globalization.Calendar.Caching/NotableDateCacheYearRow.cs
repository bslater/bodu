// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheYearRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// The persisted metadata for one cached civil year: the year, the resource version it was computed under, and the UTC
/// instant it was computed.
/// </summary>
/// <remarks>
/// A year row exists for every cached year, including a year that yielded no occurrences, so the cache can distinguish
/// a computed-but-empty year from a never-computed one.
/// </remarks>
public sealed class NotableDateCacheYearRow
{
    /// <summary>
    /// Gets or sets the civil year the entry holds.
    /// </summary>
    /// <value>The civil year.</value>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the resource version the year was computed under.
    /// </summary>
    /// <value>The resource version token, or <see langword="null" /> in a malformed file.</value>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant the year was computed.
    /// </summary>
    /// <value>The computed instant.</value>
    public DateTimeOffset ComputedAtUtc { get; set; }
}
