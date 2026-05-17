// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.Quarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the quarter number (1 – 4) of the year for the specified <see cref="DateOnly" />, using the standard
    /// calendar quarter definition.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns>
    /// An integer between 1 and 4 representing the calendar quarter that contains <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the standard calendar alignment defined by
    /// <see cref="CalendarQuarterDefinition.JanuaryToDecember" />: Q1 = Jan – Mar, Q2 = Apr – Jun, Q3 = Jul – Sep, Q4 =
    /// Oct – Dec.
    /// </para>
    /// </remarks>
    public static int Quarter(this DateOnly date) => GetQuarterForDate(date, GetQuarterDefinition(CalendarQuarterDefinition.JanuaryToDecember));

    /// <summary>
    /// Returns the quarter number (1 – 4) for the specified <see cref="DateOnly" />, using the supplied calendar
    /// quarter definition.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that determines how the year is segmented into quarters.
    /// </param>
    /// <returns>An integer between 1 and 4 representing the quarter that contains <paramref name="date" />.</returns>
    /// <remarks>
    /// <para>
    /// This overload supports both month-aligned and day-aligned quarter definitions. For provider-driven custom
    /// calendars (e.g. 4-4-5 retail calendars), use the <see cref="Quarter(DateOnly, IQuarterDefinitionProvider)" />
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
    public static int Quarter(this DateOnly date, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        return definition == CalendarQuarterDefinition.Custom
            ? throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Invalid_ProviderInterface, nameof(IQuarterDefinitionProvider)))
            : GetQuarterForDate(date, GetQuarterDefinition(definition));
    }

    /// <summary>
    /// Returns the quarter number (1 – 4) for the specified <see cref="DateOnly" />, using a custom
    /// <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="provider">
    /// The <see cref="IQuarterDefinitionProvider" /> that defines custom quarter boundaries. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>An integer between 1 and 4 representing the quarter that contains <paramref name="date" />.</returns>
    /// <remarks>
    /// <para>
    /// This overload supports advanced or domain-specific quarter systems by delegating to
    /// <see cref="IQuarterDefinitionProvider.GetQuarter(DateOnly)" /> — for example, 4-4-5 retail calendars or regional
    /// fiscal quarters.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the value returned by <paramref name="provider" /> is not in the range 1 – 4.
    /// </exception>
    public static int Quarter(this DateOnly date, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        var quarter = provider.GetQuarter(date);

        return quarter is < 1 or > 4
            ? throw new ArgumentOutOfRangeException(nameof(provider), ResourceStrings.Arg_OutOfRange_InvalidQuarterNumber)
            : quarter;
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
    /// years when the end of Q4 exceeds the anchor month.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeQuarterEndDayNumber(
        int year,
        int quarter,
        (uint defMonth, uint defDay) definition)
    {
        var q = (uint)(quarter - 1);
        var endMonth = ((((q + 1U) * 3U) + definition.defMonth - 1U) % 12U) + 1U;
        var endYear = (endMonth <= definition.defMonth) ? year + 1 : year;
        return DateTimeExtensions.GetDayNumberUnchecked(endYear, (int)endMonth, (int)definition.defDay) - 1;
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
    /// year starting in October will have Q1 starting in October of the previous year.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeQuarterStartDayNumber(
        int year,
        int quarter,
        (uint defMonth, uint defDay) definition)
    {
        var q = (uint)(quarter - 1);
        var startMonth = (((q * 3U) + definition.defMonth - 1U) % 12U) + 1U;
        var startYear = (startMonth < definition.defMonth) ? year + 1 : year;
        return DateTimeExtensions.GetDayNumberUnchecked(startYear, (int)startMonth, (int)definition.defDay);
    }

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
    /// If the calculated start of the resolved quarter is after <paramref name="referenceDate" />, the year is
    /// decremented to reflect the prior fiscal year.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int Year, int Quarter) GetQuarterAndYearFromDate(CalendarQuarterDefinition definition, DateOnly referenceDate)
    {
        (uint defMonth, uint defDay) defMonthDay = GetQuarterDefinition(definition);
        var q = GetQuarterForDate(referenceDate, defMonthDay);
        var y = referenceDate.Year;

        var startMonth = ((((uint)(q - 1) * 3U) + defMonthDay.defMonth - 1U) % 12U) + 1U;
        var startYear = (startMonth < defMonthDay.defMonth) ? y + 1 : y;
        var startDate = new DateOnly(startYear, (int)startMonth, (int)defMonthDay.defDay);

        if (startDate.DayNumber > referenceDate.DayNumber)
            y--;

        return (y, q);
    }

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
    /// This helper unpacks the encoded <see cref="CalendarQuarterDefinition" /> value for internal use in modular
    /// calculations.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (uint defMonth, uint defDay) GetQuarterDefinition(CalendarQuarterDefinition definition)
    {
        // Unpack MMDD value: e.g., 406 = 4/06
        var def = (uint)definition;
        var defMonth = def / 100U;
        var defDay = def % 100U;

        return new(defMonth, defDay);
    }

    /// <summary>
    /// Determines the quarter number (1 – 4) that includes the specified <see cref="DateOnly" />, based on a month-day
    /// anchor definition.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="definition">A tuple representing the start of Q1, encoded as (month, day).</param>
    /// <returns>An integer between 1 and 4 representing the resolved quarter number.</returns>
    /// <remarks>
    /// If <paramref name="date" /> falls before the anchor day in the quarter's first month, it is considered part of
    /// the previous quarter.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetQuarterForDate(this DateOnly date, (uint defMonth, uint defDay) definition)
    {
        // Compute quarter number using modular offset from anchor month
        var quarter = ((date.Month + 12 - (int)definition.defMonth) % 12 / 3) + 1;

        // If anchor day is not the 1st, check if we are in the quarter's start month but still before the anchor day - in that case, we
        // belong to the previous quarter
        if (definition.defDay != 1)
        {
            // Compute the actual start month for the resolved quarter
            var quarterStartMonth = ((((uint)(quarter - 1) * 3U) + definition.defMonth - 1U) % 12U) + 1U;

            // If we're in the quarter's start month but before the anchor day, back up a quarter
            if ((uint)date.Month == quarterStartMonth && (uint)date.Day < definition.defDay)
                quarter = quarter == 1 ? 4 : quarter - 1;
        }

        return quarter;
    }
}
