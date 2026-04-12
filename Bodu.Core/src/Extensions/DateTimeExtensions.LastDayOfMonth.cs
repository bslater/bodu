// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.LastDayOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the same month and year as the specified <paramref name="dateTime" />.
    /// </summary>
    /// <param name="dateTime">The input <see cref="DateTime" /> whose month and year are used to determine the last day.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the last day of the month, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method calculates the last day of the month using Gregorian calendar rules. Leap years are correctly accounted for when
    /// determining the length of February.
    /// </para>
    /// <para>The result has its time component normalized to midnight (00:00:00), and the original <see cref="DateTime.Kind" /> is retained.</para>
    /// </remarks>
    public static DateTime LastDayOfMonth(this DateTime dateTime) => new DateTime(GetLastDayOfMonthTicks(dateTime), dateTime.Kind);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the specified month and year.
    /// </summary>
    /// <param name="year">
    /// The calendar year component of the date. Must be between the <c>Year</c> property values of <see cref="DateTime.MinValue" /> and
    /// <see cref="DateTime.MaxValue" />, inclusive.
    /// </param>
    /// <param name="month">
    /// The month component of the date. Must be an integer between 1 and 12, inclusive, where 1 represents January and 12 represents December.
    /// </param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the specified month and year, using <see cref="DateTimeKind.Unspecified" />.</returns>
    /// <remarks>
    /// <para>
    /// This method uses Gregorian calendar logic to determine the number of days in the specified month, including leap year adjustments
    /// for February.
    /// </para>
    /// <para>The returned value is normalized to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified" />.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateTime.MinValue" /> or greater than that of
    /// <see cref="DateTime.MaxValue" />, or if <paramref name="month" /> is outside the valid range of 1 to 12.
    /// </exception>
    public static DateTime LastDayOfMonth(int year, int month)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTime.MinValue.Year, DateTime.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);

        return new DateTime(GetDateTicks(year, month, DateTime.DaysInMonth(year, month)), DateTimeKind.Unspecified);
    }
}
