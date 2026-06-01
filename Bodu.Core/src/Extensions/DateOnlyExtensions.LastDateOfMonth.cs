// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.LastDateOfMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the last day of the same calendar month and year as the
    /// specified <paramref name="date" />.
    /// </summary>
    /// <param name="date">The date value whose year and month are used to determine the result.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the last day of the same calendar month and year as
    /// <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method calculates the last day of the month using Gregorian calendar rules. Leap years are correctly
    /// accounted for when determining the length of February.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var date = new DateOnly(2024, 2, 15);
    /// var result = date.LastDateOfMonth(); // → 2024-02-29
    ///]]>
    /// </code>
    /// </remarks>
    public static DateOnly LastDateOfMonth(this DateOnly date) => DateOnly.FromDayNumber(DateTimeExtensions.GetDayNumber(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)));

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the last day of the specified calendar month and year.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the result. Must be between the <c>Year</c> property values of
    /// <see cref="DateOnly.MinValue" /> and <see cref="DateOnly.MaxValue" />, inclusive.
    /// </param>
    /// <param name="month">
    /// The calendar month of the result. Must be between 1 and 12, inclusive, where 1 represents January and 12
    /// represents December.
    /// </param>
    /// <returns>A <see cref="DateOnly" /> value set to the last day of the specified month and year.</returns>
    /// <remarks>
    /// <para>
    /// This method uses Gregorian calendar rules to determine the number of days in the specified month, including
    /// leap-year adjustments for February.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateOnly.MinValue" /> or greater
    /// than that of <see cref="DateOnly.MaxValue" />, -or- <paramref name="month" /> is less than 1 or greater than 12.
    /// </exception>
    public static DateOnly GetLastDateOfMonth(int year, int month)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateOnly.MinValue.Year, DateOnly.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);

        return DateOnly.FromDayNumber(DateTimeExtensions.GetDayNumber(year, month, DateTime.DaysInMonth(year, month)));
    }
}
