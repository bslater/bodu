// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.FirstDateOfWeekInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the same calendar year as the specified <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value whose year is used to determine the result.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the year. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek"/> within the same calendar year as <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>The search begins on January 1 of the year and proceeds forward to locate the first matching weekday.</para>
    /// <para>The returned value has its time component normalized to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.</exception>
    public static DateTime FirstDateOfWeekInYear(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        var ticks = GetDateTicks(dateTime.Year, 1, 1);
        ticks += GetTicksUntilNextOrSameDayOfWeek(ticks, dayOfWeek);
        return new DateTime(ticks, dateTime.Kind);
    }
}
