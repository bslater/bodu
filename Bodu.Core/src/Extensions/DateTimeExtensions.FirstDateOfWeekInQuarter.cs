// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.FirstDateOfWeekInQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first occurrence of the specified
    /// <see cref="DayOfWeek" /> within the calendar quarter that contains the specified <paramref name="dateTime" />,
    /// using the standard calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing quarter.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the quarter. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the first Monday.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek" />
    /// within the quarter, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the standard calendar alignment defined by
    /// <see cref="CalendarQuarterDefinition.JanuaryToDecember" />. The search begins on the first day of the quarter
    /// and proceeds forward to locate the first matching weekday.
    /// </para>
    /// <para>
    /// The returned value has its time component normalized to midnight (00:00:00), and the original
    /// <see cref="DateTime.Kind" /> is retained.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateTime FirstDateOfWeekInQuarter(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        (var year, var quarter) = DateTimeExtensions.GetQuarterAndYearFromDate(CalendarQuarterDefinition.JanuaryToDecember, dateTime);
        return DateTimeExtensions.GetFirstDateOfWeekInQuarterInternal(
            year,
            quarter,
            dayOfWeek,
            CalendarQuarterDefinition.JanuaryToDecember,
            dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first occurrence of the specified
    /// <see cref="DayOfWeek" /> within the quarter that contains the specified <paramref name="dateTime" />, using the
    /// supplied calendar quarter definition.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing quarter.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the quarter. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the first Monday.
    /// </param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that determines how quarter boundaries are aligned.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek" />
    /// within the quarter, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The start of the quarter is computed using <paramref name="definition" />, and the search proceeds forward to
    /// the first date that matches the specified <paramref name="dayOfWeek" />.
    /// </para>
    /// <para>
    /// The returned value has its time component normalized to midnight (00:00:00), and the original
    /// <see cref="DateTime.Kind" /> is retained.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration, -or-
    /// <paramref name="definition" /> is not a defined value of the <see cref="CalendarQuarterDefinition" />
    /// enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />; use the
    /// provider-based overload instead.
    /// </exception>
    public static DateTime FirstDateOfWeekInQuarter(this DateTime dateTime, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        if (definition == CalendarQuarterDefinition.Custom)
            throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Invalid_ProviderInterface, nameof(IQuarterDefinitionProvider)));

        (var year, var quarter) = DateTimeExtensions.GetQuarterAndYearFromDate(definition, dateTime);
        return DateTimeExtensions.GetFirstDateOfWeekInQuarterInternal(
            year,
            quarter,
            dayOfWeek,
            definition,
            dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first occurrence of the specified
    /// <see cref="DayOfWeek" /> within the quarter that contains the specified <paramref name="dateTime" />, using a
    /// custom <see cref="IQuarterDefinitionProvider" />.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing quarter.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the quarter. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the first Monday.
    /// </param>
    /// <param name="provider">
    /// The <see cref="IQuarterDefinitionProvider" /> that defines custom quarter boundaries. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek" />
    /// within the quarter, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The start of the quarter is determined by the supplied <paramref name="provider" />, and the search proceeds
    /// forward to the first date that matches the specified <paramref name="dayOfWeek" />.
    /// </para>
    /// <para>
    /// The returned value has its time component normalized to midnight (00:00:00), and the original
    /// <see cref="DateTime.Kind" /> is retained.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateTime FirstDateOfWeekInQuarter(this DateTime dateTime, DayOfWeek dayOfWeek, IQuarterDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        DateTime dt = provider.GetQuarterStart(dateTime);
        return new DateTime(dt.Ticks + DateTimeExtensions.GetTicksUntilNextOrSameDayOfWeek(dt, dayOfWeek), dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first occurrence of the specified
    /// <see cref="DayOfWeek" /> within the specified calendar <paramref name="quarter" /> and <paramref name="year" />,
    /// using the standard calendar quarter definition.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the result. Must be between the <c>Year</c> property values of
    /// <see cref="DateTime.MinValue" /> and <see cref="DateTime.MaxValue" />, inclusive.
    /// </param>
    /// <param name="quarter">The quarter number, from 1 (Jan – Mar) through 4 (Oct – Dec).</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the quarter. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the first Monday.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek" />
    /// within the quarter, using <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the standard calendar alignment defined by
    /// <see cref="CalendarQuarterDefinition.JanuaryToDecember" />.
    /// </para>
    /// <para>
    /// The returned value is normalized to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateTime.MinValue" /> or greater
    /// than that of <see cref="DateTime.MaxValue" />, -or- <paramref name="quarter" /> is less than 1 or greater than
    /// 4, -or- <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateTime GetFirstDateOfWeekInQuarter(int year, int quarter, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTimeExtensions.MinYear, DateTimeExtensions.MaxYear);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return DateTimeExtensions.GetFirstDateOfWeekInQuarterInternal(
            year,
            quarter,
            dayOfWeek,
            CalendarQuarterDefinition.JanuaryToDecember,
            DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first occurrence of the specified
    /// <see cref="DayOfWeek" /> within the specified <paramref name="quarter" /> and <paramref name="year" />, using
    /// the supplied calendar quarter definition.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the result. Must be between the <c>Year</c> property values of
    /// <see cref="DateTime.MinValue" /> and <see cref="DateTime.MaxValue" />, inclusive.
    /// </param>
    /// <param name="quarter">The quarter number, from 1 through 4.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the quarter. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the first Monday.
    /// </param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that determines how quarter boundaries are aligned.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek" />
    /// within the quarter, using <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The start of the quarter is computed using <paramref name="definition" />, and the search proceeds forward to
    /// the first date that matches the specified <paramref name="dayOfWeek" />.
    /// </para>
    /// <para>
    /// The returned value is normalized to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateTime.MinValue" /> or greater
    /// than that of <see cref="DateTime.MaxValue" />, -or- <paramref name="quarter" /> is less than 1 or greater than
    /// 4, -or- <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration, -or-
    /// <paramref name="definition" /> is not a defined value of the <see cref="CalendarQuarterDefinition" />
    /// enumeration.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="definition" /> is <see cref="CalendarQuarterDefinition.Custom" />; use the
    /// provider-based overload instead.
    /// </exception>
    public static DateTime GetFirstDateOfWeekInQuarter(int year, int quarter, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTimeExtensions.MinYear, DateTimeExtensions.MaxYear);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        return definition == CalendarQuarterDefinition.Custom
            ? throw new InvalidOperationException(
                string.Format(ResourceStrings.Arg_Invalid_ProviderInterface, nameof(IQuarterDefinitionProvider)))
            : DateTimeExtensions.GetFirstDateOfWeekInQuarterInternal(
            year,
            quarter,
            dayOfWeek,
            definition,
            DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Returns the first occurrence of the specified <paramref name="dayOfWeek" /> within the given
    /// <paramref name="quarter" /> and <paramref name="year" />, using a prevalidated calendar quarter
    /// <paramref name="definition" /> and applying the supplied <paramref name="kind" />.
    /// </summary>
    /// <param name="year">The calendar year that contains the quarter.</param>
    /// <param name="quarter">The quarter number, from 1 through 4.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate. The method searches forward from the start of the quarter to find the
    /// first occurrence.
    /// </param>
    /// <param name="definition">
    /// A defined <see cref="CalendarQuarterDefinition" /> that is not <see cref="CalendarQuarterDefinition.Custom" />.
    /// </param>
    /// <param name="kind">The <see cref="DateTimeKind" /> applied to the resulting <see cref="DateTime" />.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek" />
    /// within the specified quarter, using <paramref name="kind" />.
    /// </returns>
    /// <remarks>
    /// This helper performs no validation and is intended for internal use where all arguments are known to be valid.
    /// </remarks>
    private static DateTime GetFirstDateOfWeekInQuarterInternal(int year, int quarter, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition, DateTimeKind kind)
    {
        var ticks = ComputeQuarterStartTicks(year, quarter, GetQuarterDefinition(definition));
        ticks += (dayOfWeek - GetDayOfWeekFromTicks(ticks) + 7) % 7 * TicksPerDay;
        return new DateTime(ticks, kind);
    }
}
