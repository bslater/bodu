// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Resolves notable-date occurrences for a requested territory and day or date range.
/// </summary>
public interface INotableDateService
{
    /// <summary>
    /// Resolves the notable-date occurrences emitted on a single day for the requested territory.
    /// </summary>
    /// <param name="date">The day to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>
    /// The occurrences whose emitted date equals <paramref name="date" />; empty when there are none.
    /// </returns>
    IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory);

    /// <summary>
    /// Resolves the notable-date occurrences emitted within an inclusive date range for the requested territory.
    /// </summary>
    /// <param name="range">The inclusive range of days to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>The occurrences whose emitted date falls within the range, ordered by date then identity.</returns>
    IReadOnlyList<NotableDate> Resolve(DateRange range, string territory);
}
