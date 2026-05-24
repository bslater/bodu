// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.LastDateOfMonth.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the same calendar month and year as the
    /// specified <paramref name="dateTime" />.
    /// </summary>
    /// <param name="dateTime">The date and time value whose year and month are used to determine the result.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the last day of the same calendar month and year as
    /// <paramref name="dateTime" />, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method calculates the last day of the month using Gregorian calendar rules. Leap years are correctly
    /// accounted for when determining the length of February.
    /// </para>
    /// <para>
    /// The returned value has its time component normalized to midnight (00:00:00), and the original
    /// <see cref="DateTime.Kind" /> is retained.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var dt = new DateTime(2024, 2, 15, 14, 45, 0);
    /// var result = dt.LastDateOfMonth(); // → 2024-02-29 00:00:00
    ///]]>
    /// </code>
    /// </remarks>
    public static DateTime LastDateOfMonth(this DateTime dateTime) => new DateTime(GetLastDateOfMonthTicks(dateTime), dateTime.Kind);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the specified calendar month and year.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the result. Must be between the <c>Year</c> property values of
    /// <see cref="DateTime.MinValue" /> and <see cref="DateTime.MaxValue" />, inclusive.
    /// </param>
    /// <param name="month">
    /// The calendar month of the result. Must be between 1 and 12, inclusive, where 1 represents January and 12
    /// represents December.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the last day of the specified month and year, using
    /// <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses Gregorian calendar rules to determine the number of days in the specified month, including
    /// leap-year adjustments for February.
    /// </para>
    /// <para>
    /// The returned value is normalized to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateTime.MinValue" /> or greater
    /// than that of <see cref="DateTime.MaxValue" />, -or- <paramref name="month" /> is less than 1 or greater than 12.
    /// </exception>
    public static DateTime GetLastDateOfMonth(int year, int month)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTime.MinValue.Year, DateTime.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);

        return new DateTime(GetDateTicks(year, month, DateTime.DaysInMonth(year, month)), DateTimeKind.Unspecified);
    }
}
