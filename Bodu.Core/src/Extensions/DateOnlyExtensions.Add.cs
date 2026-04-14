// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Adds the specified number of years, months, and fractional days to the given <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly"/> value to which the offsets will be applied.</param>
    /// <param name="years">The number of calendar years to add. A negative value subtracts years.</param>
    /// <param name="months">The number of calendar months to add. A negative value subtracts months.</param>
    /// <param name="days">The number of days to add, including fractional values. A negative value subtracts days.</param>
    /// <returns>A new <see cref="DateOnly"/> adjusted by the specified number of years, months, and days.</returns>
    /// <remarks>
    /// <para>Applies adjustments in the following order: <b>years</b>, then <b>months</b>, then <b>days</b>.</para>
    /// <para>Clamps the resulting day to the last valid day of the calculated month if overflow occurs.</para>
    /// <para>Only full days are supported. Any fractional <paramref name="days"/> is rounded to the nearest whole number.</para>
    /// <para>Internally uses <see cref="DateOnly.DayNumber"/> for efficient tick-free date math.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the resulting date is before <see cref="DateOnly.MinValue"/> or after <see cref="DateOnly.MaxValue"/>.
    /// </exception>
    public static DateOnly Add(this DateOnly date, int years, int months, int days)
    {
        date.GetDateParts(out int year, out int month, out int day);

        // Convert months to total and update year/month accordingly
        int totalMonths = ((year * 12) + (month - 1)) + ((years * 12) + months);
        year = totalMonths / 12;
        month = (totalMonths % 12) + 1;

        // Clamp day based on new month/year
        bool isLeap = DateTime.IsLeapYear(year);
        int[] daysInMonths = isLeap ? DateTimeExtensions.DaysToMonth366 : DateTimeExtensions.DaysToMonth365;
        int maxDay = daysInMonths[month] - daysInMonths[month - 1];
        if (day > maxDay)
            day = maxDay;

        int dayNumber = DateTimeExtensions.GetDayNumberUnchecked(year, month, day);

        // Add days if necessary
        if (days != 0)
            dayNumber = checked(dayNumber + days);

        if ((uint)dayNumber > DateOnly.MaxValue.DayNumber)
            throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)));

        return DateOnly.FromDayNumber(dayNumber);
    }
}
