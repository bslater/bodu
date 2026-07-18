// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.Quarter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the quarter number (1 – 4) of the year for the specified <see cref="DateTime" />, using the standard
    /// calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns>
    /// An integer between 1 and 4 representing the calendar quarter that contains <paramref name="dateTime" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the standard calendar alignment defined by
    /// <see cref="CalendarQuarterDefinition.JanuaryToDecember" />: Q1 = Jan – Mar, Q2 = Apr – Jun, Q3 = Jul – Sep, Q4 =
    /// Oct – Dec.
    /// </para>
    /// </remarks>
    public static int Quarter(this DateTime dateTime) => GetQuarterForDate(dateTime, GetQuarterDefinition(CalendarQuarterDefinition.JanuaryToDecember));

    /// <summary>
    /// Returns the quarter number (1 – 4) for the specified <see cref="DateTime" />, using the supplied calendar
    /// quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that determines how the year is segmented into quarters.
    /// </param>
    /// <returns>
    /// An integer between 1 and 4 representing the quarter that contains <paramref name="dateTime" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload supports both month-aligned and day-aligned quarter definitions. For provider-driven custom
    /// calendars (e.g. 4-4-5 retail calendars), use the <see cref="Quarter(DateTime, IQuarterDefinitionProvider)" />
    /// overload.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="definition" /> is not a defined value of the <see cref="CalendarQuarterDefinition" />
    /// enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />; use the
    /// provider-based overload instead.
    /// </exception>
    public static int Quarter(this DateTime dateTime, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        return definition == CalendarQuarterDefinition.Custom
            ? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ProviderInterface, nameof(IQuarterDefinitionProvider)))
            : GetQuarterForDate(dateTime, GetQuarterDefinition(definition));
    }

    /// <summary>
    /// Returns the quarter number (1 – 4) for the specified <see cref="DateTime" />, using a custom
    /// <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="provider">
    /// The <see cref="IQuarterDefinitionProvider" /> that defines custom quarter boundaries. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// An integer between 1 and 4 representing the quarter that contains <paramref name="dateTime" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload supports advanced or domain-specific quarter systems by delegating to
    /// <see cref="IQuarterDefinitionProvider.GetQuarter(DateTime)" /> — for example, 4-4-5 retail calendars or regional
    /// fiscal quarters.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the value returned by <paramref name="provider" /> is not in the range 1 – 4.
    /// </exception>
    public static int Quarter(this DateTime dateTime, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        int quarter = provider.GetQuarter(dateTime);

        return quarter is < 1 or > 4
            ? throw new ArgumentOutOfRangeException(nameof(provider), ResourceStrings.Arg_OutOfRange_InvalidQuarterNumber)
            : quarter;
    }

    /// <summary>
    /// Computes the tick value for the end of the specified quarter, based on a month-day anchor definition.
    /// </summary>
    /// <param name="year">The year in which the quarter ends.</param>
    /// <param name="quarter">The 1-based quarter number (1 – 4).</param>
    /// <param name="definition">
    /// A tuple representing the anchor month and day that define the start of Q1 (e.g. (4, 6) for April 6).
    /// </param>
    /// <returns>The number of ticks representing midnight on the last day of the quarter.</returns>
    /// <remarks>
    /// Delegates to <see cref="QuarterCalculator.GetEndDayNumber(int, int, ValueTuple{uint, uint})" /> — the shared
    /// quarter engine — and scales the resulting day number to ticks.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long ComputeQuarterEndTicks(
        int year,
        int quarter,
        (uint defMonth, uint defDay) definition) =>
        QuarterCalculator.GetEndDayNumber(year, quarter, definition) * TicksPerDay;

    /// <summary>
    /// Computes the tick value for the start of the specified quarter, based on a month-day anchor definition.
    /// </summary>
    /// <param name="year">The year in which the quarter begins.</param>
    /// <param name="quarter">The 1-based quarter number (1 – 4).</param>
    /// <param name="definition">
    /// A tuple representing the anchor month and day that define the start of Q1 (e.g. (4, 6) for April 6).
    /// </param>
    /// <returns>The number of ticks representing midnight on the first day of the quarter.</returns>
    /// <remarks>
    /// Delegates to <see cref="QuarterCalculator.GetStartDayNumber(int, int, ValueTuple{uint, uint})" /> — the shared
    /// quarter engine — and scales the resulting day number to ticks.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long ComputeQuarterStartTicks(
        int year,
        int quarter,
        (uint defMonth, uint defDay) definition) =>
        QuarterCalculator.GetStartDayNumber(year, quarter, definition) * TicksPerDay;

    /// <summary>
    /// Determines the fiscal year and quarter that include the specified <paramref name="referenceDate" />, using the
    /// supplied quarter definition.
    /// </summary>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that defines quarter anchor points.
    /// </param>
    /// <param name="referenceDate">The date to evaluate.</param>
    /// <returns>A tuple containing the resolved year and quarter number (1 – 4).</returns>
    /// <remarks>
    /// Delegates to <see cref="QuarterCalculator.GetYearAndQuarter(CalendarQuarterDefinition, int, int, int, int)" /> —
    /// the shared quarter engine — passing the date components and day number of <paramref name="referenceDate" />; any
    /// time-of-day component is discarded by the day-number conversion.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int Year, int Quarter) GetQuarterAndYearFromDate(CalendarQuarterDefinition definition, DateTime referenceDate) =>
        QuarterCalculator.GetYearAndQuarter(definition, referenceDate.Year, referenceDate.Month, referenceDate.Day, GetDayNumber(referenceDate));

    /// <summary>
    /// Extracts the anchor month and day components from a <see cref="CalendarQuarterDefinition" /> value.
    /// </summary>
    /// <param name="definition">
    /// A <see cref="CalendarQuarterDefinition" /> value encoded as MMDD (e.g. 406 for April 6).
    /// </param>
    /// <returns>
    /// A tuple <c>(defMonth, defDay)</c> representing the anchor month and day that define the start of Q1.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="QuarterCalculator.GetDefinition(CalendarQuarterDefinition)" /> — the shared quarter
    /// engine.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint defMonth, uint defDay) GetQuarterDefinition(CalendarQuarterDefinition definition) =>
        QuarterCalculator.GetDefinition(definition);

    /// <summary>
    /// Determines the quarter number (1 – 4) that includes the specified <see cref="DateTime" />, based on a month-day
    /// anchor definition.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="definition">A tuple representing the start of Q1, encoded as (month, day).</param>
    /// <returns>An integer between 1 and 4 representing the resolved quarter number.</returns>
    /// <remarks>
    /// Delegates to <see cref="QuarterCalculator.GetQuarter(int, int, ValueTuple{uint, uint})" /> — the shared quarter
    /// engine — passing the month and day components of <paramref name="dateTime" />.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetQuarterForDate(this DateTime dateTime, (uint defMonth, uint defDay) definition) =>
        QuarterCalculator.GetQuarter(dateTime.Month, dateTime.Day, definition);
}
