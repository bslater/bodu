// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> obtained by adding the specified number of years, months, and fractional days to the supplied <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value to which the offsets are applied.</param>
    /// <param name="years">The number of calendar years to add. A negative value subtracts years.</param>
    /// <param name="months">The number of calendar months to add. A negative value subtracts months.</param>
    /// <param name="days">The number of days to add, including fractional values. A negative value subtracts days.</param>
    /// <returns>An object whose value is the result of adding the specified number of years, months, and days to <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>Adjustments are applied in the order years, then months, then days. When the resulting day does not exist in the target month (e.g. February 30), the date is clamped to the last valid day of that month, accounting for leap years and varying month lengths.</para>
    /// <para>The <paramref name="days"/> parameter supports fractional values, applied with tick-level precision. Values smaller than 1e-10 are ignored. The original time-of-day is preserved unless <paramref name="days"/> includes a fractional component, in which case the time is adjusted accordingly.</para>
    /// <para>This method performs all adjustments using tick arithmetic and does not rely on <see cref="DateTime.AddYears(int)"/>, <see cref="DateTime.AddMonths(int)"/>, or <see cref="DateTime.AddDays(double)"/>, making it suitable for performance-critical paths.</para>
    /// <para><b>Examples:</b></para>
    /// <code>
    ///<![CDATA[
    /// var dt1 = new DateTime(2023, 1, 31);
    /// var result1 = dt1.Add(0, 1, 0); // → 2023-02-28 (non-leap year)
    ///
    /// var dt2 = new DateTime(2020, 1, 31);
    /// var result2 = dt2.Add(0, 1, 0); // → 2020-02-29 (leap year)
    ///
    /// var dt3 = new DateTime(2023, 3, 15, 10, 30, 0);
    /// var result3 = dt3.Add(1, -2, 10.75); // → 2024-01-25 19:30:00
    ///
    /// var dt4 = new DateTime(2022, 10, 5, 8, 0, 0);
    /// var result4 = dt4.Add(0, 0, -2.5); // → 2022-10-02 20:00:00
    ///
    /// var dt5 = new DateTime(2024, 2, 29);
    /// var result5 = dt5.Add(1, 0, 0); // → 2025-02-28 (2025 is not a leap year)
    ///]]>
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the resulting date is earlier than <see cref="DateTime.MinValue"/> or later than <see cref="DateTime.MaxValue"/>.</exception>
    public static DateTime Add(this DateTime dateTime, int years, int months, double days)
    {
        // Extract parts from original date
        dateTime.GetDateParts(out var y, out var m, out var d);

        // Adjust year/month
        var i = m + months - 1 + (years * 12);
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
        var day = Math.Min(d, DateTime.DaysInMonth(y, m));
        var totalTicks = GetDateTicks(y, m, day) + GetTimeTicks(dateTime);

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
