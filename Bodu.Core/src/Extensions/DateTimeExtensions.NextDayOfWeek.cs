// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.NextDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the next calendar occurrence of the specified <paramref name="dayOfWeek" /> after
    /// the given <paramref name="dateTime" />.
    /// </summary>
    /// <param name="dateTime">
    /// The starting <see cref="DateTime" /> from which to search forward. The result preserves the original time-of-day and <see cref="DateTime.Kind" />.
    /// </param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> value to locate. For example, specifying <see cref="DayOfWeek.Monday" /> returns the next Monday.
    /// </param>
    /// <returns>
    /// An object whose value is set to the next occurrence of <paramref name="dayOfWeek" /> following <paramref name="dateTime" />. The
    /// result maintains the original time-of-day and <see cref="DateTime.Kind" /> of <paramref name="dateTime" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a valid value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="dateTime" /> already falls on the specified <paramref name="dayOfWeek" />, the result is exactly 7 days later.
    /// </para>
    /// <para>This method advances forward in time and does not return the current date, even if it matches the requested day of the week.</para>
    /// </remarks>
    public static DateTime NextDayOfWeek(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return dateTime.AddTicks(
            dateTime.DayOfWeek == dayOfWeek
                ? TicksPerWeek
                : GetTicksUntilNextOrSameDayOfWeek(dateTime, dayOfWeek));
    }
}
