// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.FirstDayOfWeekInQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the
    /// calendar quarter that contains the specified instance, using the standard calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the quarter.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> value to locate within the quarter.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek"/> in the quarter.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses <see cref="CalendarQuarterDefinition.JanuaryToDecember"/> to determine quarter boundaries. The result is
    /// calculated by starting at the first day of the quarter and searching forward to the first occurrence of the specified <paramref name="dayOfWeek"/>.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value.
    /// </exception>
    public static DateTime FirstDayOfWeekInQuarter(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        (int year, int quarter) = DateTimeExtensions.GetQuarterAndYearFromDate(CalendarQuarterDefinition.JanuaryToDecember, dateTime);
        return DateTimeExtensions.GetFirstDayOfWeekInQuarterInternal(
            year,
            quarter,
            dayOfWeek,
            CalendarQuarterDefinition.JanuaryToDecember,
            dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the quarter
    /// that contains the specified instance, using the given quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the quarter.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> value to locate within the quarter.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition"/> used to define quarter boundaries.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek"/> in the quarter.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The result is calculated by identifying the first day of the quarter based on <paramref name="definition"/> and searching forward
    /// to the specified <paramref name="dayOfWeek"/>.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value,
    /// -or- <paramref name="definition"/> is not a valid <see cref="CalendarQuarterDefinition"/> value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>. Use the provider-based overload instead.
    /// </exception>
    public static DateTime FirstDayOfWeekInQuarter(this DateTime dateTime, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (int year, int quarter) = DateTimeExtensions.GetQuarterAndYearFromDate(definition, dateTime);
        return DateTimeExtensions.GetFirstDayOfWeekInQuarterInternal(
            year,
            quarter,
            dayOfWeek,
            definition,
            dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the quarter
    /// that contains the specified instance, using a custom quarter definition provider.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the quarter.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> value to locate within the quarter.</param>
    /// <param name="provider">The <see cref="IQuarterDefinitionProvider"/> implementation that defines quarter boundaries.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek"/> in the quarter.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The result is determined by starting at the beginning of the quarter as defined by <paramref name="provider"/> and searching
    /// forward to the specified <paramref name="dayOfWeek"/>.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value.
    /// </exception>
    public static DateTime FirstDayOfWeekInQuarter(this DateTime dateTime, DayOfWeek dayOfWeek, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        DateTime dt = provider.GetQuarterStart(dateTime);
        return new DateTime(dt.Ticks + DateTimeExtensions.GetTicksUntilNextOrSameDayOfWeek(dt, dayOfWeek), dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the
    /// specified quarter and year, using the standard calendar quarter definition.
    /// </summary>
    /// <param name="year">The calendar year. Must be between 1 and 9999, inclusive.</param>
    /// <param name="quarter">The quarter number, from 1 to 4.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> value to locate within the quarter.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek"/> in the quarter.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses <see cref="CalendarQuarterDefinition.JanuaryToDecember"/> to define quarter boundaries, and computes the first
    /// date within the quarter that matches <paramref name="dayOfWeek"/>.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance is <see cref="DateTimeKind.Unspecified"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than 1 or greater than 9999,
    /// -or- <paramref name="quarter"/> is less than 1 or greater than 4,
    /// -or- <paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value.
    /// </exception>
    public static DateTime GetFirstDayOfWeekInQuarter(int year, int quarter, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTimeExtensions.MinYear, DateTimeExtensions.MaxYear);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return DateTimeExtensions.GetFirstDayOfWeekInQuarterInternal(
            year,
            quarter,
            dayOfWeek,
            CalendarQuarterDefinition.JanuaryToDecember,
            DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> within the
    /// specified quarter and year, using the specified quarter definition.
    /// </summary>
    /// <param name="year">The calendar year. Must be between 1 and 9999, inclusive.</param>
    /// <param name="quarter">The quarter number, from 1 to 4.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> value to locate within the quarter.</param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition"/> used to define quarter boundaries.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek"/> in the quarter.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The start of the quarter is computed using <paramref name="definition"/>, and the search proceeds forward to the first date that
    /// matches the specified <paramref name="dayOfWeek"/>.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance is <see cref="DateTimeKind.Unspecified"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than 1 or greater than 9999,
    /// -or- <paramref name="quarter"/> is less than 1 or greater than 4,
    /// -or- <paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value,
    /// -or- <paramref name="definition"/> is not a valid <see cref="CalendarQuarterDefinition"/> value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition"/> is <see cref="CalendarQuarterDefinition.Custom"/>. Use the provider-based overload instead.
    /// </exception>
    public static DateTime GetFirstDayOfWeekInQuarter(int year, int quarter, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTimeExtensions.MinYear, DateTimeExtensions.MaxYear);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Required_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        return DateTimeExtensions.GetFirstDayOfWeekInQuarterInternal(
            year,
            quarter,
            dayOfWeek,
            definition,
            DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Computes the first occurrence of the specified <see cref="DayOfWeek"/> within the given quarter and year, based on the provided
    /// <see cref="CalendarQuarterDefinition"/>, and returns a <see cref="DateTime"/> value with the specified <see cref="DateTimeKind"/>.
    /// </summary>
    /// <param name="year">The calendar year that contains the quarter. Must be within the valid range of <see cref="DateTime"/>.</param>
    /// <param name="quarter">The quarter number (1 to 4) within the specified year.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> to locate. The method searches forward from the start of the quarter to find the first occurrence.
    /// </param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition"/> that defines how the year is divided into quarters.</param>
    /// <param name="kind">The <see cref="DateTimeKind"/> to apply to the resulting <see cref="DateTime"/> value.</param>
    /// <returns>
    /// A <see cref="DateTime"/> representing midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek"/> within the
    /// specified quarter, using the specified <paramref name="kind"/>.
    /// </returns>
    /// <remarks>
    /// This method is used internally to calculate the first matching weekday in a quarter, with tick-level precision. It assumes that all
    /// arguments have been validated by the caller.
    /// </remarks>
    private static DateTime GetFirstDayOfWeekInQuarterInternal(int year, int quarter, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition, DateTimeKind kind)
    {
        long ticks = ComputeQuarterStartTicks(year, quarter, GetQuarterDefinition(definition));
        ticks += ((dayOfWeek - GetDayOfWeekFromTicks(ticks) + 7) % 7) * TicksPerDay;
        return new DateTime(ticks, kind);
    }
}
