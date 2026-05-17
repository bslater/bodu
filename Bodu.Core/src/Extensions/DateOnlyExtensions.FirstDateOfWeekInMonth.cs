// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDateOfWeekInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first occurrence of the specified
    /// <see cref="DayOfWeek" /> within the same calendar month and year as the specified <paramref name="date" />.
    /// </summary>
    /// <param name="date">The date value whose month and year are used to determine the result.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the month. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the first Monday.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the first occurrence of <paramref name="dayOfWeek" /> within the same
    /// calendar month and year as <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The search begins on the first day of the month and proceeds forward to locate the first matching weekday.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateOnly FirstDateOfWeekInMonth(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        var baseDayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, date.Month, 1);
        return DateOnly.FromDayNumber(baseDayNumber + (((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(baseDayNumber) + 7) % 7));
    }
}
