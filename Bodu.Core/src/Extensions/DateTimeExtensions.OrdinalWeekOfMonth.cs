// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.OrdinalWeekOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Gets the <see cref="WeekOfMonthOrdinal"/> represented by this <see cref="DateTime"/> instance, indicating the ordinal occurrence
    /// of its <see cref="DateTime.DayOfWeek"/> within the month.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> instance to evaluate.</param>
    /// <returns>
    /// A <see cref="WeekOfMonthOrdinal"/> value that indicates which occurrence of the weekday this <paramref name="dateTime"/>
    /// represents within its calendar month.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method calculates how many full 7-day intervals have passed since the start of the month, based on <see cref="DateTime.Day"/>.
    /// For example, the 1st through 7th of the month yield <see cref="WeekOfMonthOrdinal.First"/>, while the 8th through 14th yield
    /// <see cref="WeekOfMonthOrdinal.Second"/>, and so on.
    /// </para>
    /// <para><b>Note:</b><see cref="WeekOfMonthOrdinal.Fifth"/> is only returned when a month contains five occurrences of the given <see cref="DateTime.DayOfWeek"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the calculated ordinal does not correspond to a defined <see cref="WeekOfMonthOrdinal"/> value.
    /// </exception>
    public static WeekOfMonthOrdinal OrdinalWeekOfMonth(this DateTime dateTime)
    {
        int ordinal = ((dateTime.Day - 1) / 7) + 1;

        ThrowHelper.ThrowIfEnumValueIsUndefined<WeekOfMonthOrdinal>((WeekOfMonthOrdinal)ordinal);

        return (WeekOfMonthOrdinal)ordinal;
    }
}
