// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> that adds the specified number of years, months, and fractional days to the value of the specified instance.
    /// </summary>
    /// <param name="dateTime">The date and time value to add to.</param>
    /// <param name="years">The number of years to add.</param>
    /// <param name="months">The number of months to add.</param>
    /// <param name="days">The number of days to add, including fractional values.</param>
    /// <returns>
    /// An object whose value is the sum of the date and time represented by <paramref name="dateTime"/> and the specified number of years, months, and days.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns a new <see cref="DateTime"/> whose value is the result of adding the specified number of calendar years, calendar months,
    /// and fractional days to <paramref name="dateTime"/>. The original instance is not modified.
    /// </para>
    /// <para>
    /// Adjustments are performed in the following order: years, then months, then days.
    /// Negative values for any parameter subtract from the date.
    /// </para>
    /// <para>
    /// When the resulting day does not exist in the target month (e.g., February 30), the date is clamped to the last valid day of that month.
    /// The method accounts for leap years and the varying number of days in each month.
    /// </para>
    /// <para>
    /// The <paramref name="days"/> parameter supports fractional values, which are applied with tick-level precision. Values smaller than 1e-10 are ignored.
    /// The original time-of-day is preserved unless <paramref name="days"/> includes a fractional component, in which case the time is adjusted accordingly.
    /// </para>
    /// <para>
    /// This method performs all adjustments using tick arithmetic and does not rely on <see cref="DateTime.AddYears(int)"/>,
    /// <see cref="DateTime.AddMonths(int)"/>, or <see cref="DateTime.AddDays(double)"/>, making it suitable for performance-critical paths.
    /// </para>
    /// <para>
    /// The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.
    /// </para>
    /// <para>
    /// <b>Examples:</b>
    /// <code>
    ///<![CDATA[
    /// var dt1 = new DateTime(2023, 1, 31);
    /// var result1 = dt1.Add(0, 1, 0); // → 2023-02-28 (non-leap year)
    ///
    /// var dt2 = new DateTime(2020, 1, 31);
    /// var result2 = dt2.Add(0, 1, 0); // → 2020-02-29 (leap year)
    ///
    /// var dt3 = new DateTime(2023, 3, 15, 10, 30, 0);
    /// var result3 = dt3.Add(1, -2, 10.75); // → 2024-01-25 19:30:00 (adds 1 year, subtracts 2 months, adds 10.75 days)
    ///
    /// var dt4 = new DateTime(2022, 10, 5, 8, 0, 0);
    /// var result4 = dt4.Add(0, 0, -2.5); // → 2022-10-02 20:00:00 (subtracts 2.5 days)
    ///
    /// var dt5 = new DateTime(2024, 2, 29);
    /// var result5 = dt5.Add(1, 0, 0); // → 2025-02-28 (2025 is not a leap year)
    ///]]>
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The resulting date is earlier than <see cref="DateTime.MinValue"/> or later than <see cref="DateTime.MaxValue"/>.
    /// </exception>
    public static DateTime Add(this DateTime dateTime, int years, int months, double days)
    {
        // Extract parts from original date
        dateTime.GetDateParts(out int y, out int m, out int d);

        // Adjust year/month
        int i = m + months - 1 + (years * 12);
        if (i >= 0)
        {
            m = (i % 12) + 1;
            y += i / 12;
        }
        else
        {
            m = 12 + ((i + 1) % 12);
            y += (i - 11) / 12;
        }

        // Clamp day
        int day = Math.Min(d, DateTime.DaysInMonth(y, m));
        long totalTicks = GetDateTicks(y, m, day) + GetTimeTicks(dateTime);

        // Add fractional days (if any)
        if (Math.Abs(days) > Epsilon)
            totalTicks += GetDaysToTicks(days);

        if (totalTicks < DateTime.MinValue.Ticks || totalTicks > DateTime.MaxValue.Ticks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateTime)));
        }

        return new DateTime(totalTicks, dateTime.Kind);
    }
}
