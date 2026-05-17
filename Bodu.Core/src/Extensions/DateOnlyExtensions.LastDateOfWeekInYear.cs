// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.LastDateOfWeekInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the last occurrence of the specified <see cref="DayOfWeek" />
    /// within the same calendar year as the specified <paramref name="date" />.
    /// </summary>
    /// <param name="date">The date value whose year is used to determine the result.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the year. For example, <see cref="DayOfWeek.Monday" /> returns the
    /// last Monday.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the last occurrence of <paramref name="dayOfWeek" /> within the same
    /// calendar year as <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The search begins on December 31 of the year and proceeds backward to locate the last matching weekday.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateOnly LastDateOfWeekInYear(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        var dayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, 12, 31);
        return DateOnly.FromDayNumber(dayNumber - (((int)GetDayOfWeekFromDayNumber(dayNumber) - (int)dayOfWeek + 7) % 7));
    }
}
