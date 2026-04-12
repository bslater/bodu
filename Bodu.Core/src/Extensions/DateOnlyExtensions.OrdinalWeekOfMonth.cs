// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.OrdinalWeekOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the ordinal occurrence of the weekday (e.g., first, second, third) for the specified <see cref="DateOnly" /> within its month.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> to evaluate.</param>
    /// <returns>
    /// A <see cref="WeekOfMonthOrdinal" /> value representing which occurrence of the <see cref="DateOnly.DayOfWeek" /> the given date
    /// is, within the same calendar month (e.g., the 2nd Tuesday).
    /// </returns>
    /// <remarks>
    /// The method calculates how many full weeks have passed since the first day of the month to determine the ordinal position of the
    /// weekday for the given <paramref name="date" />.
    /// <para>
    /// <b>Note:</b> The <see cref="WeekOfMonthOrdinal.Fifth" /> value is uncommon and only applies to months that contain five
    /// occurrences of the specified <see cref="DayOfWeek" />.
    /// </para>
    /// </remarks>
    public static WeekOfMonthOrdinal OrdinalWeekOfMonth(this DateOnly date)
    {
        int ordinal = ((date.Day - 1) / 7) + 1;
        return (WeekOfMonthOrdinal)ordinal;
    }
}
