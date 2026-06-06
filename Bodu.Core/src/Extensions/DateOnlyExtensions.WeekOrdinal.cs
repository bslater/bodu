// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.OrdinalWeekOfMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the <see cref="WeekOrdinal" /> represented by the specified <see cref="DateOnly" />, indicating
    /// the ordinal occurrence of its <see cref="DateOnly.DayOfWeek" /> within the month.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns>
    /// A <see cref="WeekOrdinal" /> value indicating which occurrence of the weekday <paramref name="date" />
    /// represents within its calendar month.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The result is calculated by counting how many full seven-day intervals have passed since the start of the month,
    /// based on <see cref="DateOnly.Day" />. For example, the 1st through 7th of the month yield
    /// <see cref="WeekOrdinal.First" />, while the 8th through 14th yield
    /// <see cref="WeekOrdinal.Second" />, and so on.
    /// </para>
    /// <para>
    /// <see cref="WeekOrdinal.Fifth" /> is only returned when a month contains five occurrences of the given
    /// <see cref="DateOnly.DayOfWeek" />.
    /// </para>
    /// </remarks>
    public static WeekOrdinal OrdinalWeekOfMonth(this DateOnly date)
    {
        var ordinal = ((date.Day - 1) / 7) + 1;
        return (WeekOrdinal)ordinal;
    }
}
