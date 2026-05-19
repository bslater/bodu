// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> obtained by adding the specified number of years, months, and days to the
    /// supplied <paramref name="date" />.
    /// </summary>
    /// <param name="date">The date value to which the offsets are applied.</param>
    /// <param name="years">The number of calendar years to add. A negative value subtracts years.</param>
    /// <param name="months">The number of calendar months to add. A negative value subtracts months.</param>
    /// <param name="days">The number of days to add. A negative value subtracts days.</param>
    /// <returns>A <see cref="DateOnly" /> value adjusted by the specified number of years, months, and days.</returns>
    /// <remarks>
    /// <para>
    /// Adjustments are applied in the order years, then months, then days. When the resulting day does not exist in the
    /// target month (e.g. February 30), the date is clamped to the last valid day of that month, accounting for leap
    /// years and varying month lengths.
    /// </para>
    /// <para>
    /// <b>Examples:</b>
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var d1 = new DateOnly(2023, 1, 31);
    /// var result1 = d1.Add(0, 1, 0); // → 2023-02-28 (non-leap year)
    ///
    /// var d2 = new DateOnly(2020, 1, 31);
    /// var result2 = d2.Add(0, 1, 0); // → 2020-02-29 (leap year)
    ///
    /// var d3 = new DateOnly(2024, 2, 29);
    /// var result3 = d3.Add(1, 0, 0); // → 2025-02-28 (2025 is not a leap year)
    ///]]>
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the resulting date is earlier than <see cref="DateOnly.MinValue" /> or later than
    /// <see cref="DateOnly.MaxValue" />.
    /// </exception>
    public static DateOnly Add(this DateOnly date, int years, int months, int days)
    {
        date.GetDateParts(out var year, out var month, out var day);

        // Convert months to total and update year/month accordingly
        var totalMonths = (year * 12) + (month - 1) + (years * 12) + months;
        year = totalMonths / 12;
        month = (totalMonths % 12) + 1;

        // Clamp day based on new month/year
        var isLeap = DateTime.IsLeapYear(year);
        var daysInMonths = isLeap ? DateTimeExtensions.DaysToMonth366 : DateTimeExtensions.DaysToMonth365;
        var maxDay = daysInMonths[month] - daysInMonths[month - 1];
        if (day > maxDay)
            day = maxDay;

        var dayNumber = DateTimeExtensions.GetDayNumberUnchecked(year, month, day);

        // Add days if necessary
        if (days != 0)
            dayNumber = checked(dayNumber + days);

        return (uint)dayNumber > DateOnly.MaxValue.DayNumber
            ? throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)))
            : DateOnly.FromDayNumber(dayNumber);
    }
}
