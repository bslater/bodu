// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FromDayOfYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the <paramref name="dayOfYear" />-th day of the specified
    /// <paramref name="year" />.
    /// </summary>
    /// <param name="year">The calendar year to evaluate.</param>
    /// <param name="dayOfYear">The one-based ordinal day within the year.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the date that is the <paramref name="dayOfYear" />-th day of
    /// <paramref name="year" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is the inverse of the <see cref="DateOnly.DayOfYear" /> property. Day numbering is one-based, so day
    /// <c>1</c> is January 1 and the final day (<c>365</c> in a common year, <c>366</c> in a leap year) is December 31.
    /// </para>
    /// <para>
    /// The mapping depends on whether <paramref name="year" /> is a leap year: day <c>60</c> resolves to February 29 in
    /// a leap year but to March 1 in a common year.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than 1 or greater than 9999, -or- <paramref name="dayOfYear" /> is
    /// less than 1 or greater than the number of days in <paramref name="year" /> (365 in a common year, 366 in a leap
    /// year).
    /// </exception>
    public static DateOnly FromDayOfYear(int year, int dayOfYear)
    {
        int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365; // also validates year (1..9999)
        ThrowHelper.ThrowIfLessThan(dayOfYear, 1);
        ThrowHelper.ThrowIfGreaterThan(dayOfYear, daysInYear);

        return new DateOnly(year, 1, 1).AddDays(dayOfYear - 1);
    }
}
