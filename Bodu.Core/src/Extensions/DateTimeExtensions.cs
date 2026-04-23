// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

/// <summary>
/// Provides a set of <see langword="static"/> ( <see langword="Shared"/> in Visual Basic) methods that extend the
/// <see cref="System.DateTime"/> class.
/// </summary>
public static partial class DateTimeExtensions
{
    /// <summary>
    /// Represents a small threshold used for comparing floating-point numbers where exact equality is unreliable due to binary precision.
    /// This field is constant.
    /// </summary>
    internal const double Epsilon = 1e-10;

    /// <summary>
    /// The maximum year supported by <see cref="DateTime"/>.
    /// </summary>
    internal const int MaxYear = 9999;

    /// <summary>
    /// Represents the number of milliseconds in 1 day. This field is constant.
    /// </summary>
    internal const int MillisecondsPerDay = DateTimeExtensions.MillisecondsPerHour * 24;

    /// <summary>
    /// Represents the number of milliseconds in 1 hour. This field is constant.
    /// </summary>
    internal const int MillisecondsPerHour = DateTimeExtensions.MillisecondsPerMinute * 60;

    /// <summary>
    /// Represents the number of milliseconds in 1 minute. This field is constant.
    /// </summary>
    internal const int MillisecondsPerMinute = DateTimeExtensions.MillisecondsPerSecond * 60;

    /// <summary>
    /// Represents the number of ticks (100ns) in 1 second. This field is constant.
    /// </summary>
    internal const int MillisecondsPerSecond = 1000;

    /// <summary>
    /// The minimum year supported by <see cref="DateTime"/>.
    /// </summary>
    internal const int MinYear = 1;

    /// <summary>
    /// Represents the number of ticks (100ns) in 30 days. This field is constant.
    /// </summary>
    internal const long TicksPer30Days = DateTimeExtensions.TicksPerDay * 30;

    /// <summary>
    /// Represents the number of ticks (100ns) in 1 day. This field is constant.
    /// </summary>
    internal const long TicksPerDay = DateTimeExtensions.TicksPerHour * 24;

    /// <summary>
    /// Represents the number of ticks (100ns) in 1 hour. This field is constant.
    /// </summary>
    internal const long TicksPerHour = DateTimeExtensions.TicksPerMinute * 60;

    /// <summary>
    /// Represents the number of ticks (100ns) in 1 millisecond. This field is constant.
    /// </summary>
    internal const long TicksPerMillisecond = 10000;

    /// <summary>
    /// Represents the number of ticks (100ns) in 1 minute. This field is constant.
    /// </summary>
    internal const long TicksPerMinute = DateTimeExtensions.TicksPerSecond * 60;

    /// <summary>
    /// Represents the number of ticks (100ns) in 1 second. This field is constant.
    /// </summary>
    internal const long TicksPerSecond = DateTimeExtensions.TicksPerMillisecond * 1000;

    /// <summary>
    /// Represents the number of ticks (100ns) in 1 week (7 days). This field is constant.
    /// </summary>
    internal const long TicksPerWeek = DateTimeExtensions.TicksPerDay * 7;

    /// <summary>
    /// Represents the number of ticks (100ns) in 1 year. This field is constant.
    /// </summary>
    internal const long TicksPerYear = DateTimeExtensions.TicksPerDay * DaysPerYear;

    /// <summary>
    /// Cumulative day counts at the start of each month in a non-leap year (365 days).
    /// </summary>
    internal static readonly int[] DaysToMonth365 = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };

    /// <summary>
    /// Cumulative day counts at the start of each month in a leap year (366 days).
    /// </summary>
    internal static readonly int[] DaysToMonth366 = { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };

    /// <summary>
    /// Represents the number of days in 100 years. This field is constant.
    /// </summary>
    private const int DaysPer100Years = (DateTimeExtensions.DaysPer4Years * 25) - 1;

    /// <summary>
    /// Represents the number of days in 400 years. This field is constant.
    /// </summary>
    private const int DaysPer400Years = (DateTimeExtensions.DaysPer100Years * 4) + 1;

    /// <summary>
    /// Represents the number of days in 4 years. This field is constant.
    /// </summary>
    private const int DaysPer4Years = (DateTimeExtensions.DaysPerYear * 4) + 1;

    /// <summary>
    /// Represents the number of days a non-leap year. This field is constant.
    /// </summary>
    private const int DaysPerYear = 365;

    /// <summary>
    /// Represents the number of days from 1-Jan-0001 to 31-Dec-9999. This field is constant.
    /// </summary>
    private const int DaysTo10000 = (DateTimeExtensions.DaysPer400Years * 25) - 366;

    /// <summary>
    /// Represents the number of days from 1-Jan-0001 to 31-Dec-1969. This field is constant.
    /// </summary>
    private const int DaysTo1970 = (DateTimeExtensions.DaysPer400Years * 4) + (DateTimeExtensions.DaysPer100Years * 3) + (DateTimeExtensions.DaysPer4Years * 17) + DateTimeExtensions.DaysPerYear;

    /// <summary>
    /// Represents the maximum number of milliseconds. This field is constant.
    /// </summary>
    private const long MaxMilliseconds = (long)DateTimeExtensions.DaysTo10000 * DateTimeExtensions.MillisecondsPerDay;

    /// <summary>
    /// Represents the maximum number of ticks (100ns). This field is constant.
    /// </summary>
    private const long MaxTicks = (DateTimeExtensions.DaysTo10000 * DateTimeExtensions.TicksPerDay) - 1;

    /// <summary>
    /// Represents the minimum number of milliseconds. This field is constant.
    /// </summary>
    private const long MinMilliseconds = 0;

    /// <summary>
    /// Represents the minimum number of ticks (100ns). This field is constant.
    /// </summary>
    private const long MinTicks = 0;

    /// <summary>
    /// Computes the day number corresponding to the specified <see cref="DateTime"/>, representing the number of days since 0001-01-01.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to convert.</param>
    /// <returns>The number of days elapsed since 0001-01-01, where that date is treated as day 0.</returns>
    /// <remarks>
    /// This method performs a fast, allocation-free conversion by dividing the <see cref="DateTime.Ticks"/> value by the number of ticks
    /// per day.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetDayNumber(DateTime dateTime) => (int)((ulong)dateTime.Ticks / DateTimeExtensions.TicksPerDay);

    /// <summary>
    /// Computes the day number for the specified year, month, and day, representing the number of days elapsed since 0001-01-01.
    /// </summary>
    /// <param name="year">The year component, which must be between 1 and 9999 inclusive.</param>
    /// <param name="month">The month component, which must be between 1 and 12 inclusive.</param>
    /// <param name="day">The day component, which must be valid for the specified year and month.</param>
    /// <returns>An <see cref="int"/> representing the number of days since 0001-01-01, where that date is treated as day 0.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/>, <paramref name="month"/>, or <paramref name="day"/> is outside the valid range of the
    /// Gregorian calendar, or if the combination does not form a valid date.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs full validation of the input parameters to ensure that the specified date is valid in the proleptic Gregorian calendar.
    /// </para>
    /// <para>
    /// It provides functionality equivalent to computing <c>new DateOnly(year, month, day).DayNumber</c>, but avoids object allocations and
    /// is optimised for scenarios where correctness and validation are both required.
    /// </para>
    /// </remarks>
    public static int GetDayNumber(int year, int month, int day)
    {
        if (year >= 1 && year <= 9999 && month >= 1 && month <= 12)
        {
            int[] days = DateTime.IsLeapYear(year) ? DateTimeExtensions.DaysToMonth366 : DateTimeExtensions.DaysToMonth365;
            if (day >= 1 && day <= days[month] - days[month - 1])
            {
                int y = year - 1;
                int dayNumber = (y * 365) + (y / 4) - (y / 100) + (y / 400) + days[month - 1] + day - 1;
                return dayNumber;
            }
        }

        throw new ArgumentOutOfRangeException(null, ResourceStrings.Arg_OutOfRange_BadYearMonthDay);
    }

    /// <summary>
    /// Returns the number of ticks representing the date portion (midnight) of the specified tick value.
    /// </summary>
    /// <param name="ticks">
    /// A tick count representing a date and time, where one tick equals 100 nanoseconds since 0001-01-01 00:00:00.000 in the Gregorian calendar.
    /// </param>
    /// <returns>
    /// The number of ticks corresponding to the start of the day (midnight) for the given <paramref name="ticks"/>. This value is a
    /// multiple of <see cref="DateTimeExtensions.TicksPerDay"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method discards any time-of-day component by rounding down to the nearest whole day. It is equivalent to calling
    /// <c>DateTime.Date.Ticks</c> but avoids object allocations and is optimised for internal calendar or performance-sensitive usage.
    /// </para>
    /// <para>
    /// No validation is performed. The caller is responsible for ensuring that <paramref name="ticks"/> falls within the valid
    /// <see cref="DateTime"/> range.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetDateAsTicks(long ticks) =>
        ticks - (ticks % DateTimeExtensions.TicksPerDay);

    /// <summary>
    /// Extracts the Gregorian calendar year, month, and day components from a given tick value.
    /// </summary>
    /// <param name="ticks">
    /// The number of ticks representing the date, where one tick equals 100 nanoseconds since 0001-01-01T00:00:00.000 (Gregorian calendar).
    /// </param>
    /// <param name="year">When this method returns, contains the year component (1–9999) corresponding to the specified <paramref name="ticks"/>.</param>
    /// <param name="month">When this method returns, contains the month component (1–12) corresponding to the specified <paramref name="ticks"/>.</param>
    /// <param name="day">When this method returns, contains the day component (1–31) corresponding to the specified <paramref name="ticks"/>.</param>
    /// <remarks>
    /// <para>
    /// This method performs a tick-based decomposition of a date using Gregorian calendar rules, without instantiating a
    /// <see cref="DateTime"/> object. It is intended for performance-critical scenarios where object allocations must be avoided, or where
    /// calendar field extraction is required from raw tick values.
    /// </para>
    /// <para>
    /// The calculation is equivalent to accessing the <see cref="DateTime.Year"/>, <see cref="DateTime.Month"/>, and
    /// <see cref="DateTime.Day"/> properties, but uses optimised integer arithmetic to reduce overhead.
    /// </para>
    /// <para>
    /// The input tick value must fall within the valid range supported by <see cref="DateTime"/>, which spans from
    /// <c>DateTime.MinValue.Ticks</c> to <c>DateTime.MaxValue.Ticks</c>. No validation is performed on this input.
    /// </para>
    /// <para>
    /// This method uses the standard proleptic Gregorian calendar and assumes 400-year cycles with leap years every 4 years, except years
    /// divisible by 100 unless divisible by 400.
    /// </para>
    /// </remarks>
    internal static void GetDateParts(long ticks, out int year, out int month, out int day)
    {
        // Convert ticks to total days since 0001-01-01
        int n = (int)(ticks / TicksPerDay);

        // Calculate number of whole 400-year periods
        int y400 = n / DaysPer400Years;
        n -= y400 * DaysPer400Years;

        // Calculate number of whole 100-year periods within the current 400-year block
        int y100 = n / DaysPer100Years;

        // Cap at 3 to avoid overflow into next 400-year block (i.e., max is 300 years)
        if (y100 == 4)
            y100 = 3;
        n -= y100 * DaysPer100Years;

        // Calculate number of whole 4-year periods within the current 100-year block
        int y4 = n / DaysPer4Years;
        n -= y4 * DaysPer4Years;

        // Calculate number of whole years within the current 4-year block
        int y1 = n / DaysPerYear;

        // Cap at 3 to avoid overflow into next 4-year block (max is 3 years)
        if (y1 == 4)
            y1 = 3;
        n -= y1 * DaysPerYear;

        // Final computed year
        year = (y400 * 400) + (y100 * 100) + (y4 * 4) + y1 + 1;

        // Determine leap year using reduced logic (does not rely on IsLeapYear) Only the final year of a 4-year cycle is leap if the
        // 100-year and 400-year rules are satisfied
        bool isLeap = (y1 == 3) && ((y4 != 24) || (y100 == 3));

        // Choose correct month day table
        int[] daysToMonth = isLeap ? DaysToMonth366 : DaysToMonth365;

        // Estimate month (right shift by 5 ~ divide by 32, since all months have < 32 days)
        int m = (n >> 5) + 1;

        // Correct for any overshoot in estimate
        while (n >= daysToMonth[m])
            m++;

        // Compute month and day
        month = m;
        day = n - daysToMonth[m - 1] + 1;
    }

    /// <summary>
    /// Extracts the year, month, and day components from the specified <paramref name="dateTime"/> using tick-based computation.
    /// </summary>
    /// <param name="dateTime">The <see cref="System.DateTime"/> instance whose date components are to be extracted.</param>
    /// <param name="year">When this method returns, contains the year component of the specified <paramref name="dateTime"/>.</param>
    /// <param name="month">When this method returns, contains the month component (1–12) of the specified <paramref name="dateTime"/>.</param>
    /// <param name="day">When this method returns, contains the day component (1–31) of the specified <paramref name="dateTime"/>.</param>
    /// <remarks>
    /// <para>
    /// This method computes the date parts directly from the <see cref="DateTime.Ticks"/> value, using Gregorian calendar math. It avoids
    /// calling the standard <see cref="DateTime.Year"/>, <see cref="DateTime.Month"/>, or <see cref="DateTime.Day"/> properties to
    /// reduce overhead in high-performance scenarios.
    /// </para>
    /// <para>
    /// The result is equivalent to the values returned by the standard <see cref="DateTime"/> accessors, but the method is optimised for
    /// scenarios where multiple components are needed and maximum efficiency is desired.
    /// </para>
    /// <para>This method assumes the input is within the valid <see cref="DateTime"/> range and uses no internal validation.</para>
    /// </remarks>
    internal static void GetDateParts(this DateTime dateTime, out int year, out int month, out int day) => GetDateParts(dateTime.Ticks, out year, out month, out day);

    /// <summary>
    /// Returns the number of ticks representing the date portion (midnight) of the specified <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="System.DateTime"/> instance from which to extract the date portion as a tick count.</param>
    /// <returns>
    /// A <see cref="long"/> representing the number of ticks at midnight (00:00:00.000) on the day of <paramref name="dateTime"/>. This
    /// value is a multiple of <see cref="DateTimeExtensions.TicksPerDay"/> and falls within the valid <see cref="DateTime"/> range.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method truncates the time component of the input <paramref name="dateTime"/> by rounding down to the nearest whole day
    /// boundary. It is equivalent to <c>dateTime.Date.Ticks</c> but avoids allocation of a new <see cref="DateTime"/> instance and is
    /// optimised for internal use.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long TruncateToDateTicks(DateTime dateTime) =>
        dateTime.Ticks - (dateTime.Ticks % DateTimeExtensions.TicksPerDay);

    /// <summary>
    /// Computes the day number for the specified year, month, and day, representing the number of days since 0001-01-01.
    /// </summary>
    /// <param name="year">The year component (e.g., 2025).</param>
    /// <param name="month">The month component in the range 1 through 12.</param>
    /// <param name="day">The day component in the range 1 through 31, depending on the month and year.</param>
    /// <returns>An integer representing the number of days elapsed since 0001-01-01, with that date treated as day 0.</returns>
    /// <remarks>
    /// <para>
    /// This method uses Gregorian calendar rules and a precomputed day-of-year lookup table to account for leap years. It performs no
    /// validation on the input parameters and assumes that <paramref name="year"/>, <paramref name="month"/>, and <paramref name="day"/>
    /// form a valid calendar date.
    /// </para>
    /// <para>
    /// This method is intended for trusted, performance-critical internal use where input correctness is guaranteed by the caller. It
    /// avoids allocating <see cref="DateTime"/> instances and is suitable for inlining.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetDayNumberUnchecked(int year, int month, int day)
    {
        bool isLeap = DateTime.IsLeapYear(year);
        int[] days = isLeap ? DateTimeExtensions.DaysToMonth366 : DateTimeExtensions.DaysToMonth365;

        return (int)(
            ((long)(year - 1) * 365)
            + ((year - 1) / 4)
            - ((year - 1) / 100)
            + ((year - 1) / 400)
            + days[month - 1]
            + day - 1); // Subtract 1 to match DateOnly.DayNumber origin
    }

    /// <summary>
    /// Calculates the <see cref="System.DayOfWeek"/> for a date represented as a tick count.
    /// </summary>
    /// <param name="ticks">
    /// A date and time expressed as the number of 100-nanosecond intervals (ticks) that have elapsed since January 1, 0001 at 00:00:00.000,
    /// based on the proleptic Gregorian calendar.
    /// </param>
    /// <returns>A <see cref="DayOfWeek"/> value indicating the day of the week corresponding to the specified <paramref name="ticks"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method computes the day of the week using modulo arithmetic based on the number of days since 0001-01-01. It is equivalent in
    /// result to <see cref="DateTime.DayOfWeek"/> but avoids object instantiation and is optimised for tick-level operations.
    /// </para>
    /// <para>
    /// No argument validation is performed. The caller is responsible for ensuring that the input <paramref name="ticks"/> value is valid
    /// within the supported <see cref="DateTime"/> range.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DayOfWeek GetDayOfWeekFromTicks(long ticks) => (DayOfWeek)(((ticks / DateTimeExtensions.TicksPerDay) + 1) % 7);

    /// <summary>
    /// Converts a fractional number of days to the equivalent number of ticks (100-nanosecond intervals).
    /// </summary>
    /// <param name="days">
    /// A value representing whole and/or fractional days. This value may be positive or negative and supports sub-day precision.
    /// </param>
    /// <returns>
    /// A <see cref="long"/> representing the number of ticks that correspond to the specified number of days. One day equals
    /// 864,000,000,000 ticks.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method multiplies the input <paramref name="days"/> by the number of ticks per day, rounding to the nearest whole tick using
    /// symmetric arithmetic rounding (midpoint rounding away from zero).
    /// </para>
    /// <para>It is equivalent in purpose to <see cref="TimeSpan.FromDays(double)"/> but optimised for internal use and avoids allocations.</para>
    /// <para>
    /// No argument validation is performed; it is the caller's responsibility to ensure that <paramref name="days"/> is within a valid range.
    /// </para>
    /// </remarks>
    internal static long GetDaysToTicks(double days)
    {
        // Precompute the scaling factor to reduce rounding errors: TicksPerDay / 10000 = number of 10000-tick units in one day = 86_400_000
        const double units = DateTimeExtensions.TicksPerDay / 10000.0;

        // Multiply days by 86_400_000 to convert to 10000-tick units, then add 0.5 (or subtract 0.5 for negatives) to apply midpoint
        // rounding, then truncate to long to get the integer part, and finally scale back to ticks by multiplying by 10000.
        return ((long)((days * units) + (days >= 0.0 ? 0.5 : -0.5))) * 10000;
    }

    /// <summary>
    /// Returns the tick count that represents the date nearest to the specified <paramref name="dayOfWeek"/> relative to the provided
    /// <paramref name="ticks"/> value.
    /// </summary>
    /// <param name="ticks">The reference tick count.</param>
    /// <param name="dayOfWeek">The target <see cref="DayOfWeek"/> to align to.</param>
    /// <returns>The tick count for the date nearest to <paramref name="dayOfWeek"/>.</returns>
    internal static long GetTicksForNearestDayOfWeek(long ticks, DayOfWeek dayOfWeek)
    {
        int delta = ((int)dayOfWeek - (int)GetDayOfWeekFromTicks(ticks) + 7) % 7;
        return ticks + ((delta > 3 ? delta - 7 : delta) * TicksPerDay);
    }

    /// <summary>
    /// Calculates the number of ticks between the specified <paramref name="dateTime"/> and the
    /// next occurrence of <paramref name="dayOfWeek"/>, including the current day when it already matches.
    /// </summary>
    /// <param name="dateTime">
    /// The <see cref="DateTime"/> from which to calculate the forward offset.
    /// </param>
    /// <param name="dayOfWeek">
    /// A <see cref="DayOfWeek"/> value representing the target day of the week.
    /// </param>
    /// <returns>
    /// A non-negative <see cref="long"/> value representing the number of ticks between the specified
    /// <paramref name="dateTime"/> and the next occurrence of <paramref name="dayOfWeek"/>, including
    /// the current day when it already matches. This value is a non-negative multiple of
    /// <see cref="DateTimeExtensions.TicksPerDay"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="dateTime"/> already falls on <paramref name="dayOfWeek"/>, this method returns <c>0</c>.
    /// </para>
    /// <para>
    /// This method performs no validation and assumes that <paramref name="dayOfWeek"/> is a valid
    /// <see cref="DayOfWeek"/> value.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTicksUntilNextOrSameDayOfWeek(DateTime dateTime, DayOfWeek dayOfWeek) =>
        DateTimeExtensions.TicksPerDay * (((int)dayOfWeek - (int)dateTime.DayOfWeek + 7) % 7);

    /// <summary>
    /// Calculates the number of ticks between the specified <paramref name="ticks"/> value and the
    /// next occurrence of the given <paramref name="dayOfWeek"/>, including the current day when it
    /// already matches.
    /// </summary>
    /// <param name="ticks">
    /// A date and time expressed as the number of 100-nanosecond intervals (ticks) since
    /// 0001-01-01T00:00:00.000 in the Gregorian calendar.
    /// </param>
    /// <param name="dayOfWeek">
    /// A <see cref="DayOfWeek"/> value representing the target day of the week to compute forward to.
    /// </param>
    /// <returns>
    /// A <see cref="long"/> value representing the number of ticks between the specified
    /// <paramref name="ticks"/> value and the next occurrence of <paramref name="dayOfWeek"/>,
    /// including the current day when it already matches. This value is a non-negative multiple of
    /// <see cref="DateTimeExtensions.TicksPerDay"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the date represented by <paramref name="ticks"/> already falls on
    /// <paramref name="dayOfWeek"/>, this method returns <c>0</c>.
    /// </para>
    /// <para>
    /// This method performs no argument validation. The caller must ensure that
    /// <paramref name="ticks"/> falls within the valid range for <see cref="DateTime"/>, and that
    /// <paramref name="dayOfWeek"/> is a valid <see cref="DayOfWeek"/> enum value.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTicksUntilNextOrSameDayOfWeek(long ticks, DayOfWeek dayOfWeek)
    {
        DayOfWeek day = DateTimeExtensions.GetDayOfWeekFromTicks(ticks);
        return DateTimeExtensions.TicksPerDay * (((int)dayOfWeek - (int)day + 7) % 7);
    }

    /// <summary>
    /// Calculates the number of ticks that must be added to the specified <paramref name="dateTime"/>
    /// to reach the previous occurrence of the specified <paramref name="dayOfWeek"/>.
    /// </summary>
    /// <param name="dateTime">
    /// The <see cref="DateTime"/> instance from which to calculate the backward offset.
    /// </param>
    /// <param name="dayOfWeek">
    /// A <see cref="DayOfWeek"/> value representing the target day of the week.
    /// </param>
    /// <returns>
    /// A negative <see cref="long"/> value representing the number of ticks between the specified
    /// <paramref name="dateTime"/> and the previous occurrence of <paramref name="dayOfWeek"/>.
    /// This value is a negative multiple of <see cref="DateTimeExtensions.TicksPerDay"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="dateTime"/> already falls on <paramref name="dayOfWeek"/>, this method
    /// returns a 7-day negative tick interval, representing the corresponding day in the previous week.
    /// </para>
    /// <para>
    /// This method performs no validation and assumes that <paramref name="dayOfWeek"/> is a valid
    /// <see cref="DayOfWeek"/> value.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTicksToPreviousDayOfWeek(DateTime dateTime, DayOfWeek dayOfWeek) =>
        DateTimeExtensions.TicksPerDay * ((((((int)dayOfWeek - (int)dateTime.DayOfWeek) - 7) % 7) - 7) % 7);

    /// <summary>
    /// Calculates the number of ticks between the specified <paramref name="ticks"/> value and the
    /// most recent occurrence of the given <paramref name="dayOfWeek"/>, including the current day
    /// when it already matches.
    /// </summary>
    /// <param name="ticks">
    /// A date and time expressed as the number of 100-nanosecond intervals (ticks) since
    /// 0001-01-01T00:00:00.000 in the Gregorian calendar.
    /// </param>
    /// <param name="dayOfWeek">
    /// A <see cref="DayOfWeek"/> value representing the target day of the week to compute backward to.
    /// </param>
    /// <returns>
    /// A <see cref="long"/> value representing the number of ticks between the specified
    /// <paramref name="ticks"/> and the most recent occurrence of <paramref name="dayOfWeek"/>.
    /// This value is a non-negative multiple of <see cref="DateTimeExtensions.TicksPerDay"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the date represented by <paramref name="ticks"/> already falls on
    /// <paramref name="dayOfWeek"/>, this method returns <c>0</c>.
    /// </para>
    /// <para>
    /// This method performs no validation. The caller must ensure that <paramref name="ticks"/>
    /// falls within the valid <see cref="DateTime"/> range, and that <paramref name="dayOfWeek"/>
    /// is a valid <see cref="DayOfWeek"/> enum value.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTicksSincePreviousOrSameDayOfWeek(long ticks, DayOfWeek dayOfWeek)
    {
        DayOfWeek day = DateTimeExtensions.GetDayOfWeekFromTicks(ticks);
        return DateTimeExtensions.TicksPerDay * ((7 + (int)day - (int)dayOfWeek) % 7);
    }

    /// <summary>
    /// Returns the number of ticks at midnight on the specified year, month, and day.
    /// </summary>
    /// <param name="year">The year component, which must be between 1 and 9999 inclusive.</param>
    /// <param name="month">The month component, which must be between 1 and 12 inclusive.</param>
    /// <param name="day">The day component, which must be valid for the specified year and month.</param>
    /// <returns>
    /// A <see cref="long"/> value representing the number of ticks since 0001-01-01T00:00:00.000 that correspond to the specified date.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="year"/>, <paramref name="month"/>, or <paramref name="day"/> is outside the valid range of the
    /// Gregorian calendar or does not form a valid date.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs full validation of the input parameters and throws an exception if the combination does not represent a valid
    /// calendar date.
    /// </para>
    /// <para>
    /// It is equivalent in result to <c>new DateTime(year, month, day).Ticks</c> but avoids object allocation and is optimised for
    /// high-performance internal date calculations.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetDateTicks(int year, int month, int day) =>
        DateTimeExtensions.GetDayNumberUnchecked(year, month, day) * TicksPerDay;

    /// <summary>
    /// Returns the <see cref="DayOfWeek"/> corresponding to January 1st of the specified year, using Gregorian calendar rules.
    /// </summary>
    /// <param name="year">The year for which to calculate the weekday of January 1st.</param>
    /// <returns>A <see cref="DayOfWeek"/> value indicating the weekday of January 1st in the specified year.</returns>
    /// <remarks>
    /// <para>
    /// This method uses a fast arithmetic formula to determine the weekday of January 1st without allocating a <see cref="DateTime"/>
    /// instance. It is equivalent in result to: <c>new DateTime(year, 1, 1).DayOfWeek</c>.
    /// </para>
    /// <para>
    /// The returned value corresponds to the <see cref="DayOfWeek"/> enumeration, where <c>0 = Sunday</c>, <c>1 = Monday</c>, ..., <c>6 = Saturday</c>.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DayOfWeek GetDayOfWeekForJanuary1(int year)
    {
        // Zeller’s-like congruence to determine the weekday of Jan 1
        int weekdayIndex = (year + (year / 4) - (year / 100) + (year / 400)) % 7;
        return (DayOfWeek)((weekdayIndex + 7) % 7); // Ensure non-negative
    }

    /// <summary>
    /// Calculates the calendar week number of the year for the specified tick value, using the provided week rule and week start day.
    /// </summary>
    /// <param name="ticks">The number of ticks representing the target date (100ns intervals since 0001-01-01T00:00:00.000).</param>
    /// <param name="rule">
    /// A <see cref="CalendarWeekRule"/> value that determines how the first week of the year is defined (FirstDay, FirstFullWeek, FirstFourDayWeek).
    /// </param>
    /// <param name="firstDayOfWeek">A <see cref="DayOfWeek"/> value indicating the first day of the week (e.g., Sunday, Monday).</param>
    /// <returns>The 1-based week number of the year that contains the specified tick value, based on the specified rule.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rule"/> is not a valid <see cref="CalendarWeekRule"/>.</exception>
    internal static int GetWeekOfYear(long ticks, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
    {
        // Convert ticks into a date for computing day-of-year
        var date = new DateTime(ticks, DateTimeKind.Unspecified);
        int dayOfYear = date.DayOfYear - 1; // Convert to 0-based day index

        return rule switch
        {
            CalendarWeekRule.FirstDay =>
                GetFirstDayWeekOfYear(dayOfYear, date.DayOfWeek, (int)firstDayOfWeek),

            CalendarWeekRule.FirstFullWeek =>
                GetWeekOfYearFullDays(ticks, dayOfYear, (int)firstDayOfWeek, 7),

            CalendarWeekRule.FirstFourDayWeek =>
                GetWeekOfYearFullDays(ticks, dayOfYear, (int)firstDayOfWeek, 4),

            _ => throw new ArgumentOutOfRangeException(
                    nameof(rule),
                    string.Format(ResourceStrings.Arg_OutOfRangeException_EnumValue, rule, nameof(CalendarWeekRule))),
        };
    }

    /// <summary>
    /// Returns the day of the week considered the start of the week for a given <see cref="CalendarWeekendDefinition"/> definition.
    /// </summary>
    /// <param name="weekend">The weekend configuration to evaluate.</param>
    /// <returns>The inferred <see cref="DayOfWeek"/> that begins the week.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided <paramref name="weekend"/> is not supported.</exception>
    internal static DayOfWeek GetWeekStartDay(CalendarWeekendDefinition weekend) => weekend switch
    {
        CalendarWeekendDefinition.SaturdaySunday => DayOfWeek.Monday,
        CalendarWeekendDefinition.ThursdayFriday => DayOfWeek.Saturday,
        CalendarWeekendDefinition.FridaySaturday => DayOfWeek.Sunday,
        CalendarWeekendDefinition.SundayOnly => DayOfWeek.Monday,
        CalendarWeekendDefinition.FridayOnly => DayOfWeek.Saturday,
        CalendarWeekendDefinition.None => DayOfWeek.Monday,
        _ => throw new ArgumentOutOfRangeException(
            nameof(weekend),
            $"Unsupported {nameof(CalendarWeekendDefinition)} definition: {weekend}")
    };

    /// <summary>
    /// Returns the number of ticks at midnight on the first day of the specified month.
    /// </summary>
    /// <param name="dateTime">
    /// The <see cref="System.DateTime"/> value from which to extract the year and month for computing the first day.
    /// </param>
    /// <returns>A <see cref="long"/> value representing the number of ticks at 00:00:00.000 on the first day of the month of <paramref name="dateTime"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method is equivalent in result to <c>new DateTime(dateTime.Year, dateTime.Month, 1).Ticks</c> but avoids object allocation and
    /// is optimised for internal date calculations.
    /// </para>
    /// </remarks>
    private static long GetFirstDayOfMonthTicks(DateTime dateTime) => DateTimeExtensions.GetDateTicks(dateTime.Year, dateTime.Month, 1);

    /// <summary>
    /// Returns the number of ticks at midnight on the first specified weekday in the given month.
    /// </summary>
    /// <param name="dateTime">A <see cref="System.DateTime"/> from which to extract the target month and year.</param>
    /// <param name="dayOfWeek">The <see cref="System.DayOfWeek"/> value to locate within the month.</param>
    /// <returns>
    /// A <see cref="long"/> value representing the number of ticks at 00:00:00.000 on the first <paramref name="dayOfWeek"/> occurring in
    /// the same month as <paramref name="dateTime"/>.
    /// </returns>
    /// <remarks>
    /// <para>The calculation starts at the first day of the month and advances forward to the first matching <paramref name="dayOfWeek"/>.</para>
    /// <para>
    /// This method performs no validation on <paramref name="dayOfWeek"/> and assumes it is a valid <see cref="DayOfWeek"/> enumeration value.
    /// </para>
    /// <para>It avoids allocation by returning a raw tick count and is suitable for high-performance date logic.</para>
    /// </remarks>
    private static long GetFirstDayOfWeekInMonthTicks(DateTime dateTime, DayOfWeek dayOfWeek)
    {
        long ticks = DateTimeExtensions.GetFirstDayOfMonthTicks(dateTime);
        ticks += ((dayOfWeek - DateTimeExtensions.GetDayOfWeekFromTicks(ticks) + 7) % 7) * DateTimeExtensions.TicksPerDay;
        return ticks;
    }

    /// <summary>
    /// Returns the number of ticks at midnight on the first specified weekday in the given year and month.
    /// </summary>
    /// <param name="year">The year component of the target month (expected to be in the range 1 through 9999).</param>
    /// <param name="month">The month component of the target month (expected to be in the range 1 through 12).</param>
    /// <param name="dayOfWeek">The <see cref="System.DayOfWeek"/> to locate within the specified month.</param>
    /// <returns>
    /// A <see cref="long"/> value representing the number of ticks at midnight on the first occurrence of <paramref name="dayOfWeek"/> in
    /// the specified month and year.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method starts from the first day of the given month and advances to the first matching <paramref name="dayOfWeek"/> using
    /// modulo arithmetic.
    /// </para>
    /// <para>The returned tick count corresponds to 00:00:00.000 of the computed date.</para>
    /// <para>
    /// This method performs no validation. It assumes the specified <paramref name="year"/>, <paramref name="month"/>, and
    /// <paramref name="dayOfWeek"/> are within valid ranges and form a valid calendar month.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetFirstDayOfWeekInMonthTicks(int year, int month, DayOfWeek dayOfWeek)
    {
        long ticks = DateTimeExtensions.GetDateTicks(year, month, 1);
        ticks += ((dayOfWeek - DateTimeExtensions.GetDayOfWeekFromTicks(ticks) + 7) % 7) * DateTimeExtensions.TicksPerDay;
        return ticks;
    }

    /// <summary>
    /// Computes the week number of the year using the <see cref="CalendarWeekRule.FirstDay"/> rule.
    /// </summary>
    /// <param name="dayOfYear">Zero-based day index within the year (e.g., Jan 1 = 0).</param>
    /// <param name="dayOfWeek">The day of the week of the specified date.</param>
    /// <param name="firstDayOfWeek">The starting day of the week as an integer (0–6).</param>
    /// <returns>The 1-based week number that contains the specified date.</returns>
    private static int GetFirstDayWeekOfYear(int dayOfYear, DayOfWeek dayOfWeek, int firstDayOfWeek)
    {
        // Determine the day of the week for Jan 1 by back-calculating from the current date
        int dayForJan1 = (int)dayOfWeek - (dayOfYear % 7);

        // Calculate offset to align with the first day of the week
        int offset = (dayForJan1 - firstDayOfWeek + 14) % 7;

        // Adjust day-of-year and compute week number
        return ((dayOfYear + offset) / 7) + 1;
    }

    /// <summary>
    /// Returns the number of ticks at midnight on the last specified weekday in the given month.
    /// </summary>
    /// <param name="dateTime">The <see cref="System.DateTime"/> value from which to determine the year and month.</param>
    /// <returns>
    /// A <see cref="long"/> value representing the number of ticks at 00:00:00.000 on the last day of the month for the given <paramref name="dateTime"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses the number of days in the specified month and year (via <c>dateTime.DaysInMonth()</c>) to identify the last
    /// calendar day.
    /// </para>
    /// <para>
    /// It is equivalent in result to <c>new DateTime(year, month, DaysInMonth()).Ticks</c>, but avoids object allocation and is optimised
    /// for internal calendar calculations.
    /// </para>
    /// </remarks>
    private static long GetLastDayOfMonthTicks(DateTime dateTime) => DateTimeExtensions.GetDateTicks(dateTime.Year, dateTime.Month, dateTime.DaysInMonth());

    /// <summary>
    /// Returns the tick count that represents the last day of week in the current month of the <see cref="System.DateTime"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetLastDayOfWeekInMonth(long ticks, DayOfWeek dayOfWeek)
    {
        ticks += ((dayOfWeek - DateTimeExtensions.GetDayOfWeekFromTicks(ticks) - 7) % 7) * DateTimeExtensions.TicksPerDay;
        return ticks;
    }

    /// <summary>
    /// Returns the tick count that represents the last day of week in the current month of the <see cref="System.DateTime"/>.
    /// </summary>
    private static long GetLastDayOfWeekInMonthAsTicks(DateTime dateTime, DayOfWeek dayOfWeek) => DateTimeExtensions.GetLastDayOfWeekInMonth(DateTimeExtensions.GetLastDayOfMonthTicks(dateTime), dayOfWeek);

    /// <summary>
    /// Returns the number of ticks at midnight on the last specified weekday in the given year and month.
    /// </summary>
    /// <param name="year">The calendar year (1 through 9999).</param>
    /// <param name="month">The calendar month (1 through 12).</param>
    /// <param name="dayOfWeek">The <see cref="System.DayOfWeek"/> to locate within the month.</param>
    /// <returns>
    /// A <see cref="long"/> value representing the number of ticks at 00:00:00.000 on the last occurrence of <paramref name="dayOfWeek"/>
    /// in the specified <paramref name="year"/> and <paramref name="month"/>.
    /// </returns>
    /// <remarks>
    /// <para>This method starts from the last day of the given month and steps backward to find the final occurrence of the specified <paramref name="dayOfWeek"/>.</para>
    /// <para>The result is a tick value normalised to midnight and avoids object allocation.</para>
    /// <para>No validation is performed; the caller is responsible for ensuring all parameters are within valid calendar ranges.</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetLastDayOfWeekInMonthAsTicks(int year, int month, DayOfWeek dayOfWeek)
    {
        long ticks = DateTimeExtensions.GetDateTicks(year, month, DateTime.DaysInMonth(year, month));
        ticks += ((dayOfWeek - DateTimeExtensions.GetDayOfWeekFromTicks(ticks) - 7) % 7) * DateTimeExtensions.TicksPerDay;
        return ticks;
    }

    /// <summary>
    /// Returns the number of ticks since midnight for the specified hour, minute, and second.
    /// </summary>
    /// <param name="hour">The hour component, in the range 0 through 23 inclusive.</param>
    /// <param name="minute">The minute component, in the range 0 through 59 inclusive.</param>
    /// <param name="second">The second component, in the range 0 through 59 inclusive.</param>
    /// <returns>
    /// A <see cref="long"/> value representing the number of ticks since midnight (00:00:00.000) for the specified time components. The
    /// result is always between 0 and <c>DateTimeExtensions.TicksPerDay - 1</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="hour"/> is not between 0 and 23, or <paramref name="minute"/> or <paramref name="second"/> is not
    /// between 0 and 59.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method calculates the total number of seconds from midnight using the specified hour, minute, and second values, and converts
    /// that total into ticks. It is equivalent in effect to <c>new TimeSpan(hour, minute, second).Ticks</c>, but avoids object allocation
    /// and is optimised for internal use.
    /// </para>
    /// </remarks>
    private static long GetTicksForTime(int hour, int minute, int second)
    {
        if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60 && second >= 0 && second < 60)
        {
            int t = (hour * 3600) + (minute * 60) + second;
            return t * DateTimeExtensions.TicksPerSecond;
        }

        throw new ArgumentOutOfRangeException(null, ResourceStrings.Arg_OutOfRange_BadHourMinuteSecond);
    }

    /// <summary>
    /// Returns the number of ticks representing the time-of-day portion of the specified <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="System.DateTime"/> instance from which to extract the time portion.</param>
    /// <returns>
    /// A <see cref="long"/> value representing the number of ticks since midnight (00:00:00.000) on the given
    /// <paramref name="dateTime"/>. The result is always in the range 0 to <c>DateTimeExtensions.TicksPerDay - 1</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method removes the date component from the <paramref name="dateTime"/> and returns only the fractional portion corresponding
    /// to the time of day. It is equivalent to <c>dateTime.TimeOfDay.Ticks</c> but avoids allocations.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetTimeTicks(DateTime dateTime) => dateTime.Ticks % DateTimeExtensions.TicksPerDay;

    /// <summary>
    /// Computes the calendar week number of the year for a given date using either <see cref="CalendarWeekRule.FirstFullWeek"/> or
    /// <see cref="CalendarWeekRule.FirstFourDayWeek"/>, depending on the <paramref name="fullDays"/> parameter.
    /// </summary>
    /// <param name="ticks">The tick count representing the target date.</param>
    /// <param name="dayOfYear">Zero-based day-of-year index of the target date.</param>
    /// <param name="firstDayOfWeek">The starting day of the week as an integer (0–6).</param>
    /// <param name="fullDays">The minimum number of days required in the first week (7 for FirstFullWeek, 4 for FirstFourDayWeek).</param>
    /// <returns>The 1-based week number for the date specified by <paramref name="ticks"/>.</returns>
    private static int GetWeekOfYearFullDays(long ticks, int dayOfYear, int firstDayOfWeek, int fullDays)
    {
        int dayOfWeek = (int)DateTimeExtensions.GetDayOfWeekFromTicks(ticks);
        int dayForJan1 = dayOfWeek - (dayOfYear % 7);
        int offset = (firstDayOfWeek - dayForJan1 + 14) % 7;

        if (offset != 0 && offset >= fullDays)
            offset -= 7;

        int adjustedDay = dayOfYear - offset;

        if (adjustedDay >= 0)
        {
            int weekNum = (adjustedDay / 7) + 1;

            // Check if this week straddles the year boundary and enough days
            // fall in the next year to make it belong to next year's week 1.
            var date = new DateTime(ticks, DateTimeKind.Unspecified);
            int daysInYear = DateTime.IsLeapYear(date.Year) ? 366 : 365;
            int daysInNextYear = (offset + weekNum * 7) - daysInYear;
            if (daysInNextYear >= fullDays)
                return 1;

            return weekNum;
        }

        // Date falls in the last week of the previous year — recurse backward.
        long previousTicks = ticks - ((dayOfYear + 1L) * DateTimeExtensions.TicksPerDay);

        if (previousTicks < DateTimeExtensions.MinTicks)
            return 1;

        DateTimeExtensions.GetDateParts(previousTicks, out int prevYear, out int prevMonth, out int prevDay);
        int prevDayOfYear = DateTimeExtensions.GetDayNumber(prevYear, prevMonth, prevDay)
            - DateTimeExtensions.GetDayNumber(prevYear, 1, 1);

        return GetWeekOfYearFullDays(previousTicks, prevDayOfYear, firstDayOfWeek, fullDays);
    }
}
