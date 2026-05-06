// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDateOfWeekInQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the calendar quarter that contains the specified <paramref name="date"/>, using the standard calendar quarter definition.
    /// </summary>
    /// <param name="date">The date value used to determine the containing quarter.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the quarter. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first occurrence of <paramref name="dayOfWeek"/> within the quarter that contains <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>This overload uses the standard calendar alignment defined by <see cref="CalendarQuarterDefinition.JanuaryToDecember"/>. The search begins on the first day of the quarter and proceeds forward to locate the first matching weekday.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.</exception>
    public static DateOnly FirstDateOfWeekInQuarter(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        (int year, int quarter) = GetQuarterAndYearFromDate(CalendarQuarterDefinition.JanuaryToDecember, referenceDate: date);
        return GetFirstDateOfWeekInQuarterInternal(year, quarter, dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the quarter that contains the specified <paramref name="date"/>, using the supplied calendar quarter definition.
    /// </summary>
    /// <param name="date">The date value used to determine the containing quarter.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the quarter. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition"/> that determines how quarter boundaries are aligned.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first occurrence of <paramref name="dayOfWeek"/> within the quarter that contains <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>The start of the quarter is computed using <paramref name="definition"/>, and the search proceeds forward to the first date that matches the specified <paramref name="dayOfWeek"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration,
    /// -or- <paramref name="definition"/> is not a defined value of the <see cref="CalendarQuarterDefinition"/> enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>; use the provider-based overload instead.</exception>
    public static DateOnly FirstDateOfWeekInQuarter(this DateOnly date, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (int year, int quarter) = GetQuarterAndYearFromDate(definition, referenceDate: date);
        return GetFirstDateOfWeekInQuarterInternal(year, quarter, dayOfWeek, definition);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the quarter that contains the specified <paramref name="date"/>, using a custom <see cref="IQuarterDefinitionProvider"/>.
    /// </summary>
    /// <param name="date">The date value used to determine the containing quarter.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the quarter. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday.</param>
    /// <param name="provider">The <see cref="IQuarterDefinitionProvider"/> that defines custom quarter boundaries. Must not be <see langword="null"/>.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first occurrence of <paramref name="dayOfWeek"/> within the quarter that contains <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>The start of the quarter is determined by the supplied <paramref name="provider"/>, and the search proceeds forward to the first date that matches the specified <paramref name="dayOfWeek"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.</exception>
    public static DateOnly FirstDateOfWeekInQuarter(this DateOnly date, DayOfWeek dayOfWeek, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        DateOnly start = provider.GetQuarterStartDate(date);
        int days = start.DayNumber + ((dayOfWeek - start.DayOfWeek + 7) % 7);
        return DateOnly.FromDayNumber(days);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the specified calendar <paramref name="quarter"/> and <paramref name="year"/>, using the standard calendar quarter definition.
    /// </summary>
    /// <param name="year">The calendar year of the result. Must be between the <c>Year</c> property values of <see cref="DateOnly.MinValue"/> and <see cref="DateOnly.MaxValue"/>, inclusive.</param>
    /// <param name="quarter">The quarter number, from 1 (Jan – Mar) through 4 (Oct – Dec).</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the quarter. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first occurrence of <paramref name="dayOfWeek"/> within the specified quarter and year.</returns>
    /// <remarks>
    /// <para>This overload uses the standard calendar alignment defined by <see cref="CalendarQuarterDefinition.JanuaryToDecember"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than the <c>Year</c> of <see cref="DateOnly.MinValue"/> or greater than that of <see cref="DateOnly.MaxValue"/>,
    /// -or- <paramref name="quarter"/> is less than 1 or greater than 4,
    /// -or- <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.
    /// </exception>
    public static DateOnly GetFirstDateOfWeekInQuarter(int year, int quarter, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateOnly.MinValue.Year, DateOnly.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return GetFirstDateOfWeekInQuarterInternal(year, quarter, dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the specified <paramref name="quarter"/> and <paramref name="year"/>, using the supplied calendar quarter definition.
    /// </summary>
    /// <param name="year">The calendar year of the result. Must be between the <c>Year</c> property values of <see cref="DateOnly.MinValue"/> and <see cref="DateOnly.MaxValue"/>, inclusive.</param>
    /// <param name="quarter">The quarter number, from 1 through 4.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the quarter. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition"/> that determines how quarter boundaries are aligned.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first occurrence of <paramref name="dayOfWeek"/> within the specified quarter and year.</returns>
    /// <remarks>
    /// <para>The start of the quarter is computed using <paramref name="definition"/>, and the search proceeds forward to the first date that matches the specified <paramref name="dayOfWeek"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than the <c>Year</c> of <see cref="DateOnly.MinValue"/> or greater than that of <see cref="DateOnly.MaxValue"/>,
    /// -or- <paramref name="quarter"/> is less than 1 or greater than 4,
    /// -or- <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration,
    /// -or- <paramref name="definition"/> is not a defined value of the <see cref="CalendarQuarterDefinition"/> enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>; use the provider-based overload instead.</exception>
    public static DateOnly GetFirstDateOfWeekInQuarter(int year, int quarter, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateOnly.MinValue.Year, DateOnly.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        return GetFirstDateOfWeekInQuarterInternal(year, quarter, dayOfWeek, definition);
    }

    /// <summary>
    /// Returns the first occurrence of the specified <paramref name="dayOfWeek"/> within the given <paramref name="quarter"/> and <paramref name="year"/>, using a prevalidated calendar quarter <paramref name="definition"/>.
    /// </summary>
    /// <param name="year">The calendar year that contains the quarter.</param>
    /// <param name="quarter">The quarter number, from 1 through 4.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate. The method searches forward from the start of the quarter to find the first occurrence.</param>
    /// <param name="definition">A defined <see cref="CalendarQuarterDefinition"/> that is not <see cref="CalendarQuarterDefinition.Custom"/>.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first occurrence of <paramref name="dayOfWeek"/> within the specified quarter.</returns>
    /// <remarks>This helper performs no validation and is intended for internal use where all arguments are known to be valid.</remarks>
    private static DateOnly GetFirstDateOfWeekInQuarterInternal(int year, int quarter, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        int days = ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(definition));
        days += (dayOfWeek - GetDayOfWeekFromDayNumber(days) + 7) % 7;
        return DateOnly.FromDayNumber(days);
    }
}
