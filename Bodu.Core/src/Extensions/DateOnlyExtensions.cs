// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

/// <summary>
/// Provides calendar-arithmetic operations over <see cref="DateOnly"/> — age, period anchors, weekday navigation, week and quarter
/// numbering, and culture-aware formatting helpers — that complement the small surface shipped with <see cref="DateOnly"/> itself.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DateOnly"/> intentionally exposes very little behaviour beyond a numeric day count, leaving callers to build calendar
/// logic by hand or to fall back to <see cref="DateTime"/>. This class supplies the period and weekday arithmetic that most
/// scheduling, reporting, and fiscal-calendar code needs, expressed directly on <see cref="DateOnly"/> so the time component
/// cannot leak in by accident.
/// </para>
/// <para>
/// The API surface clusters into four groups: relative period anchors (<c>FirstDateOfMonth</c>, <c>FirstDateOfQuarter</c>,
/// <c>FirstDateOfYear</c> and their <c>LastDateOf…</c> counterparts), weekday navigation (<c>NextDateOfWeek</c>,
/// <c>PreviousDateOfWeek</c>, <c>NearestDateOfWeek</c>, <c>NthDateOfWeekInMonth</c>), period predicates and counters
/// (<c>IsLeapYear</c>, <c>IsWeekend</c>, <c>IsInRange</c>, <c>WeekOfMonth</c>, <c>WeekOfYear</c>, <c>Quarter</c>), and culture-aware
/// formatting (<c>DayName</c>, <c>MonthName</c>).
/// </para>
/// <para>
/// Methods that emit text or read <see cref="System.Globalization.Calendar"/> data accept an optional culture or calendar
/// argument; when omitted they fall back to <see cref="System.Globalization.CultureInfo.CurrentCulture"/>, so the same call
/// produces different text on different threads. Methods that perform pure date arithmetic (period anchors, weekday navigation)
/// are culture-neutral, allocation-free, and deterministic. <see cref="ArgumentOutOfRangeException"/> is thrown when an
/// arithmetic operation would leave the supported <see cref="DateOnly"/> range.
/// </para>
/// <example>
/// <code language="csharp">
/// var date = new DateOnly(2025, 4, 30);
///
/// // Anchor to the first Monday in this month.
/// DateOnly firstMonday = date.FirstDateOfWeekInMonth(DayOfWeek.Monday);
/// // => 2025-04-07
///
/// // Walk to the previous Friday, even if today is already a Friday.
/// DateOnly priorFriday = date.PreviousDateOfWeek(DayOfWeek.Friday);
/// // => 2025-04-25
///
/// // Compute the calendar week within the month using ISO rules.
/// int weekOfMonth = date.WeekOfMonth(CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
/// // => 5
/// </code>
/// </example>
/// </remarks>
public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Extracts the year, month, and day components from the specified <see cref="DateOnly"/> instance.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly"/> value to extract components from.</param>
    /// <param name="year">Outputs the year component.</param>
    /// <param name="month">Outputs the month component (1–12).</param>
    /// <param name="day">Outputs the day component (1–31, depending on the month).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetDateParts(this DateOnly date, out int year, out int month, out int day)
    {
        year = date.Year;
        month = date.Month;
        day = date.Day;
    }

    /// <summary>
    /// Calculates the <see cref="System.DayOfWeek"/> for a date represented as a day number since 0001-01-01.
    /// </summary>
    /// <param name="days">The number of days since January 1, 0001 (day 0), in the proleptic Gregorian calendar.</param>
    /// <returns>
    /// A <see cref="DayOfWeek"/> value indicating the day of the week corresponding to the specified <paramref name="days"/> value, where
    /// 0 represents Sunday and 6 represents Saturday.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method computes the day of the week using modulo arithmetic and is equivalent in result to <see cref="DateTime.DayOfWeek"/>,
    /// but operates directly on day numbers without allocating a <see cref="DateTime"/> object.
    /// </para>
    /// <para>
    /// No argument validation is performed. The caller must ensure that <paramref name="days"/> falls within the valid
    /// <see cref="DateTime"/> range (0 to <c>DateTime.MaxValue.Ticks / TimeSpan.TicksPerDay</c>).
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DayOfWeek GetDayOfWeekFromDayNumber(int days) => (DayOfWeek)((days + 1) % 7);

    /// <summary>
    /// Calculates the day number of the first occurrence of a specified <see cref="DayOfWeek"/> in the given month and year.
    /// </summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1 through 12).</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> value to locate (e.g., <see cref="DayOfWeek.Monday"/> for the first Monday of the month).
    /// </param>
    /// <returns>
    /// The day number (number of days since 0001-01-01) of the first occurrence of <paramref name="dayOfWeek"/> within the specified month
    /// and year.
    /// </returns>
    /// <remarks>
    /// This method is useful for determining anchored recurrence patterns such as "the second Tuesday of the month" or for calculating
    /// scheduling boundaries tied to weekdays.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetFirstDateOfWeekInMonthDayNumber(int year, int month, DayOfWeek dayOfWeek) => DateTimeExtensions.GetDayNumberUnchecked(year, month, 1)
            + (((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(DateTimeExtensions.GetDayNumberUnchecked(year, month, 1)) + 7) % 7);

    /// <summary>
    /// Calculates the day number of the last occurrence of a specified <see cref="DayOfWeek"/> in the given month and year.
    /// </summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1 through 12).</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> value to locate (e.g., <see cref="DayOfWeek.Friday"/> for the last Friday of the month).
    /// </param>
    /// <returns>
    /// The day number (number of days since 0001-01-01) of the last occurrence of <paramref name="dayOfWeek"/> within the specified month
    /// and year.
    /// </returns>
    /// <remarks>
    /// This method is useful for determining scheduling constraints, such as "the last Sunday of the month", or for calendar-based
    /// alignment to business rules and event planning.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetLastDateOfWeekInMonthDayNumber(int year, int month, DayOfWeek dayOfWeek) => DateTimeExtensions.GetDayNumberUnchecked(year, month, DateTime.DaysInMonth(year, month))
            - (((int)GetDayOfWeekFromDayNumber(DateTimeExtensions.GetDayNumberUnchecked(year, month, DateTime.DaysInMonth(year, month))) - (int)dayOfWeek + 7) % 7);

    /// <summary>
    /// Calculates the day number corresponding to the date nearest to the specified <paramref name="dayOfWeek"/>, relative to the given <paramref name="dayNumber"/>.
    /// </summary>
    /// <param name="dayNumber">The day number (in the system's continuous day count) to compare against.</param>
    /// <param name="dayOfWeek">The target <see cref="DayOfWeek"/> to locate.</param>
    /// <returns>
    /// The day number of the nearest date that falls on the specified <paramref name="dayOfWeek"/>. If two dates are equally near (e.g., 3
    /// days before and after), the earlier date is returned.
    /// </returns>
    /// <remarks>
    /// This method is typically used in date calculations where you need to snap to the closest occurrence of a given day of the week
    /// relative to a reference date.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetNearestDayOfWeek(int dayNumber, DayOfWeek dayOfWeek)
    {
        var delta = ((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(dayNumber) + 7) % 7;
        return dayNumber + (delta > 3 ? delta - 7 : delta);
    }

    /// <summary>
    /// Calculates the number of days to add to a given day number to reach the next occurrence of the specified <see cref="DayOfWeek"/>.
    /// </summary>
    /// <param name="days">The reference day number (number of days since 0001-01-01).</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate (e.g., <see cref="DayOfWeek.Friday"/> to find the next Friday).</param>
    /// <returns>
    /// An integer in the range 0–6 representing the number of days to add to <paramref name="days"/> to reach the next occurrence of
    /// <paramref name="dayOfWeek"/>. Returns 0 if <paramref name="days"/> already falls on <paramref name="dayOfWeek"/>; callers that
    /// require a strictly forward result must substitute 7 when 0 is returned.
    /// </returns>
    /// <remarks>
    /// This method is useful for forward-aligned date calculations, such as determining the next occurrence of a specific weekday after a
    /// given date (e.g., "next Monday") in a calendar or recurrence rule context.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetNextDayOfWeekFromDayNumber(int days, DayOfWeek dayOfWeek) => ((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(days) + 7) % 7;

    /// <summary>
    /// Calculates the number of days to subtract from a given day number to reach the previous occurrence of the specified <see cref="DayOfWeek"/>.
    /// </summary>
    /// <param name="days">The reference day number (number of days since 0001-01-01).</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate (e.g., <see cref="DayOfWeek.Monday"/> to find the previous Monday).</param>
    /// <returns>
    /// A negative integer in the range −7 to −1 representing the number of days to subtract from <paramref name="days"/> to reach the
    /// previous occurrence of <paramref name="dayOfWeek"/>. Returns −7 when <paramref name="days"/> already falls on
    /// <paramref name="dayOfWeek"/>, consistent with the convention that the previous occurrence is always at least one day earlier.
    /// </returns>
    /// <remarks>
    /// This method is useful for backward-aligned date calculations, such as determining the most recent occurrence of a specific weekday
    /// before a given date (e.g., "last Thursday") in a calendar or scheduling context.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetPreviousDayOfWeekFromDayNumber(int days, DayOfWeek dayOfWeek)
    {
        var delta = ((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(days) - 7) % 7;

        // When dayOfWeek matches the current day, C# modulo yields 0 rather than -7.
        // Always return a non-zero negative value so callers reliably step backward.
        return delta == 0 ? -7 : delta;
    }
}
