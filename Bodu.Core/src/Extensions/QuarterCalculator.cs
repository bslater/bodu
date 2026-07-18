// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuarterCalculator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

/// <summary>
/// Provides the shared anchor-month quarter arithmetic consumed by the <see cref="DateTimeExtensions" /> and
/// <see cref="DateOnlyExtensions" /> quarter members.
/// </summary>
/// <remarks>
/// <para>
/// The quarter engine is expressed over calendar components (year, month, day) and day numbers — the number of days
/// since 0001-01-01, the common currency of <see cref="DateOnly.DayNumber" /> and <c>DateTime.Ticks / TicksPerDay</c> —
/// so both extension surfaces compute identical results from a single implementation. The <see cref="DateTime" /> twin
/// scales the returned day numbers by <see cref="DateTimeExtensions.TicksPerDay" />; the <see cref="DateOnly" /> twin
/// consumes them directly.
/// </para>
/// <para>
/// A <see cref="CalendarQuarterDefinition" /> value encodes its Q1 anchor as MMDD (e.g. <c>406</c> for April 6).
/// Quarters advance in three-month steps from that anchor, wrapping across the calendar-year boundary when the anchor
/// month is not January. No argument validation is performed here; callers validate the public inputs before
/// delegating.
/// </para>
/// </remarks>
internal static class QuarterCalculator
{
    /// <summary>
    /// Extracts the anchor month and day components from a <see cref="CalendarQuarterDefinition" /> value.
    /// </summary>
    /// <param name="definition">
    /// A <see cref="CalendarQuarterDefinition" /> value encoded as MMDD (e.g. 406 for April 6).
    /// </param>
    /// <returns>
    /// A tuple <c>(Month, Day)</c> representing the anchor month and day that define the start of Q1.
    /// </returns>
    /// <remarks>
    /// This helper unpacks the encoded <see cref="CalendarQuarterDefinition" /> value for use in the modular quarter
    /// calculations below. <see cref="CalendarQuarterDefinition.Custom" /> carries no anchor encoding and must be
    /// rejected by the caller before delegating.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (uint Month, uint Day) GetDefinition(CalendarQuarterDefinition definition)
    {
        // Unpack MMDD value: e.g., 406 = 4/06
        uint def = (uint)definition;
        uint defMonth = def / 100U;
        uint defDay = def % 100U;

        return new(defMonth, defDay);
    }

    /// <summary>
    /// Determines the quarter number (1 – 4) that includes the specified calendar month and day, based on a month-day
    /// anchor definition.
    /// </summary>
    /// <param name="month">The calendar month component (1 – 12) of the date to evaluate.</param>
    /// <param name="day">The calendar day component (1 – 31) of the date to evaluate.</param>
    /// <param name="definition">A tuple representing the start of Q1, encoded as (month, day).</param>
    /// <returns>An integer between 1 and 4 representing the resolved quarter number.</returns>
    /// <remarks>
    /// If the date falls before the anchor day in the quarter's first month, it is considered part of the previous
    /// quarter.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetQuarter(int month, int day, (uint Month, uint Day) definition)
    {
        // Compute quarter number using modular offset from anchor month
        int quarter = (((month + 12 - (int)definition.Month) % 12) / 3) + 1;

        // If anchor day is not the 1st, check if we are in the quarter's start month but still before the anchor day - in that case, we
        // belong to the previous quarter
        if (definition.Day != 1)
        {
            // Compute the actual start month for the resolved quarter
            uint quarterStartMonth = ((((uint)(quarter - 1) * 3U) + definition.Month - 1U) % 12U) + 1U;

            // If we're in the quarter's start month but before the anchor day, back up a quarter
            if ((uint)month == quarterStartMonth && (uint)day < definition.Day)
                quarter = quarter == 1 ? 4 : quarter - 1;
        }

        return quarter;
    }

    /// <summary>
    /// Computes the day number for the first day of the specified quarter, based on a month-day anchor definition.
    /// </summary>
    /// <param name="year">The fiscal or calendar year in which the quarter starts.</param>
    /// <param name="quarter">The 1-based quarter number (1 – 4).</param>
    /// <param name="definition">
    /// A tuple representing the anchor month and day that define the start of Q1 (e.g. (4, 6) for April 6).
    /// </param>
    /// <returns>The day number representing the first day of the specified quarter.</returns>
    /// <remarks>
    /// Accounts for non-standard quarter anchors that may wrap into the following calendar year. For example, a fiscal
    /// year starting in October will have Q3 and Q4 beginning in the following calendar year.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetStartDayNumber(int year, int quarter, (uint Month, uint Day) definition)
    {
        uint q = (uint)(quarter - 1);

        // Calculate the month where this quarter starts
        uint startMonth = (((q * 3U) + definition.Month - 1U) % 12U) + 1U;

        // If the start month is before the anchor month, it wraps into the *next calendar year*
        int startYear = (startMonth < definition.Month) ? year + 1 : year;

        return DateTimeExtensions.GetDayNumberUnchecked(startYear, (int)startMonth, (int)definition.Day);
    }

    /// <summary>
    /// Computes the day number for the last day of the specified quarter, based on a month-day anchor definition.
    /// </summary>
    /// <param name="year">The fiscal or calendar year in which the quarter ends.</param>
    /// <param name="quarter">The 1-based quarter number (1 – 4).</param>
    /// <param name="definition">
    /// A tuple representing the anchor month and day that define the start of Q1 (e.g. (4, 6) for April 6).
    /// </param>
    /// <returns>The day number representing the last day of the specified quarter.</returns>
    /// <remarks>
    /// The result is calculated by subtracting one day from the start of the next quarter, accounting for wrap-around
    /// years when the end of the quarter passes the anchor month.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetEndDayNumber(int year, int quarter, (uint Month, uint Day) definition)
    {
        uint q = (uint)(quarter - 1);

        // Calculate the month where this quarter ends
        uint endMonth = ((((q + 1U) * 3U) + definition.Month - 1U) % 12U) + 1U;

        // If the end month is less than or equal to the anchor month, it also wraps
        int endYear = (endMonth <= definition.Month) ? year + 1 : year;

        return DateTimeExtensions.GetDayNumberUnchecked(endYear, (int)endMonth, (int)definition.Day) - 1;
    }

    /// <summary>
    /// Determines the fiscal year and quarter that include the specified reference date, using the supplied quarter
    /// definition.
    /// </summary>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that defines quarter anchor points.
    /// </param>
    /// <param name="year">The calendar year component of the reference date.</param>
    /// <param name="month">The calendar month component (1 – 12) of the reference date.</param>
    /// <param name="day">The calendar day component (1 – 31) of the reference date.</param>
    /// <param name="dayNumber">The day number (days since 0001-01-01) of the reference date.</param>
    /// <returns>A tuple containing the resolved year and quarter number (1 – 4).</returns>
    /// <remarks>
    /// If the calculated start of the resolved quarter is after the reference date, the year is decremented to reflect
    /// the prior fiscal year.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int Year, int Quarter) GetYearAndQuarter(CalendarQuarterDefinition definition, int year, int month, int day, int dayNumber)
    {
        (uint Month, uint Day) def = GetDefinition(definition);

        // Determine the quarter number (1–4) for the provided reference date
        int q = GetQuarter(month, day, def);

        // Compute the actual calendar month when the resolved quarter starts
        uint startMonth = ((((uint)(q - 1) * 3U) + def.Month - 1U) % 12U) + 1U;

        // Adjust the start year if the start month falls in the next calendar year
        int startYear = (startMonth < def.Month) ? year + 1 : year;

        // Calculate the day number for the quarter start date
        int startDayNumber = DateTimeExtensions.GetDayNumberUnchecked(startYear, (int)startMonth, (int)def.Day);

        // If the start date is after the reference date, back up to the previous fiscal year
        if (startDayNumber > dayNumber)
            year--;

        return (year, q);
    }
}
