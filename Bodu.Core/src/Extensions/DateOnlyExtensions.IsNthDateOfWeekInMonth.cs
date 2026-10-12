// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsNthDateOfWeekInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <paramref name="date" /> is the requested ordinal occurrence of a
    /// <see cref="DayOfWeek" /> within its calendar month.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="dayOfWeek">The weekday whose occurrence is being tested.</param>
    /// <param name="ordinal">First through fifth occurrence, or <see cref="WeekOrdinal.Last" />.</param>
    /// <returns>
    /// <see langword="true" /> if the date is the requested weekday occurrence; otherwise <see langword="false" />. A
    /// fifth occurrence missing from a month returns <see langword="false" /> instead of throwing.
    /// </returns>
    /// <remarks>
    /// The ordinal describes repeated occurrences of a weekday in the Gregorian month, not culture-defined calendar
    /// week numbers. Unlike <see cref="NthDateOfWeekInMonth(DateOnly, DayOfWeek, WeekOrdinal)" />, this predicate does
    /// not throw when a valid ordinal does not occur in the month.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> or <paramref name="ordinal" /> is not a defined enum value.
    /// </exception>
    public static bool IsNthDateOfWeekInMonth(this DateOnly date, DayOfWeek dayOfWeek, WeekOrdinal ordinal)
    {
        // Both enums define one contiguous range, so a range test finds every undefined value without the metadata
        // lookup ThrowIfEnumValueIsUndefined makes, which cron's searches would pay on every day they test. The
        // helper still throws, so the exception is the same.
        if (dayOfWeek is < DayOfWeek.Sunday or > DayOfWeek.Saturday) ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        if (ordinal is < WeekOrdinal.First or > WeekOrdinal.Last) ThrowHelper.ThrowIfEnumValueIsUndefined(ordinal);

        if (date.DayOfWeek != dayOfWeek)
        {
            return false;
        }

        return ordinal == WeekOrdinal.Last
            ? date.Day + 7 > DateTime.DaysInMonth(date.Year, date.Month)
            : ((date.Day - 1) / 7) + 1 == (int)ordinal;
    }
}
