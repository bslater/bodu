// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.FirstDayOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first day of the same month and year as the specified instance.
    /// </summary>
    /// <param name="dateTime">The date and time value whose year and month are used to determine the result.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first day of the same calendar month and year.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns a <see cref="DateTime"/> with the day component set to 1 and the time component set to midnight (00:00:00).
    /// </para>
    /// <para>
    /// The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.
    /// </para>
    /// <para><b>Example:</b></para>
    /// <code>
    ///<![CDATA[
    /// var dt = new DateTime(2025, 7, 15, 14, 45, 0);
    /// var result = dt.FirstDayOfMonth(); // → 2025-07-01 00:00:00
    ///]]>
    /// </code>
    /// </remarks>
    public static DateTime FirstDayOfMonth(this DateTime dateTime) => new DateTime(GetFirstDayOfMonthTicks(dateTime), dateTime.Kind);

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first day of the specified month and year.
    /// </summary>
    /// <param name="year">The calendar year of the result. Must be between 1 and 9999, inclusive.</param>
    /// <param name="month">The calendar month of the result, where 1 represents January and 12 represents December.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first day of the specified month and year.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method constructs a <see cref="DateTime"/> from the specified <paramref name="year"/> and <paramref name="month"/> with the day
    /// component set to 1 and the time component set to midnight (00:00:00).
    /// </para>
    /// <para>
    /// The <see cref="DateTime.Kind"/> property of the returned instance is <see cref="DateTimeKind.Unspecified"/>.
    /// </para>
    /// <para><b>Example:</b></para>
    /// <code>
    ///<![CDATA[
    /// var result = DateTimeExtensions.GetFirstDayOfMonth(2024, 2); // → 2024-02-01 00:00:00
    ///]]>
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than 1 or greater than 9999,
    /// -or- if <paramref name="month"/> is less than 1 or greater than 12.
    /// </exception>
    public static DateTime GetFirstDayOfMonth(int year, int month)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTime.MinValue.Year, DateTime.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);

        return new DateTime(GetDateTicks(year, month, 1), DateTimeKind.Unspecified);
    }
}
