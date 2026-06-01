// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDateOfWeekInYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first occurrence of the specified
    /// <see cref="DayOfWeek" /> within the same calendar year as the specified <paramref name="date" />.
    /// </summary>
    /// <param name="date">The date value whose year is used to determine the result.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the year. For example, <see cref="DayOfWeek.Monday" /> returns the
    /// first Monday.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the first occurrence of <paramref name="dayOfWeek" /> within the same
    /// calendar year as <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The search begins on January 1 of the year and proceeds forward to locate the first matching weekday.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateOnly FirstDateOfWeekInYear(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        var dayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, 1, 1);
        return DateOnly.FromDayNumber(dayNumber + (((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(dayNumber) + 7) % 7));
    }
}
