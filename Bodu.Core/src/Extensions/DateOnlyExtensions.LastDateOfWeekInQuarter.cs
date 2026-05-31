// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.LastDateOfWeekInQuarter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the last occurrence of the specified <see cref="DayOfWeek" />
    /// within the quarter that contains the specified <paramref name="date" />, using the supplied calendar quarter
    /// definition.
    /// </summary>
    /// <param name="date">The date value used to determine the containing quarter.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the quarter. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the last Monday.
    /// </param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that determines how quarter boundaries are aligned.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the last occurrence of <paramref name="dayOfWeek" /> within the quarter
    /// that contains <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The end of the quarter is computed using <paramref name="definition" />, and the search proceeds backward to the
    /// last date that matches the specified <paramref name="dayOfWeek" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration, -or-
    /// <paramref name="definition" /> is not a defined value of the <see cref="CalendarQuarterDefinition" />
    /// enumeration.
    /// </exception>
    public static DateOnly LastDateOfWeekInQuarter(this DateOnly date, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        (var year, var quarter) = GetQuarterAndYearFromDate(definition, referenceDate: date);
        var dayNumber = ComputeQuarterEndDayNumber(year, quarter, GetQuarterDefinition(definition));
        dayNumber += (dayOfWeek - DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber) + 7) % 7;
        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the last occurrence of the specified <see cref="DayOfWeek" />
    /// within the specified <paramref name="quarter" /> and <paramref name="year" />, using the supplied calendar
    /// quarter definition.
    /// </summary>
    /// <param name="year">The calendar year of the result.</param>
    /// <param name="quarter">The quarter number, from 1 through 4.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the quarter. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the last Monday.
    /// </param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that determines how quarter boundaries are aligned.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the last occurrence of <paramref name="dayOfWeek" /> within the specified
    /// quarter and year.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The end of the quarter is computed using <paramref name="definition" />, and the search proceeds backward to the
    /// last date that matches the specified <paramref name="dayOfWeek" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="quarter" /> is less than 1 or greater than 4, -or- <paramref name="dayOfWeek" /> is
    /// not a defined value of the <see cref="DayOfWeek" /> enumeration, -or- <paramref name="definition" /> is not a
    /// defined value of the <see cref="CalendarQuarterDefinition" /> enumeration.
    /// </exception>
    public static DateOnly GetLastDateOfWeekInQuarter(int year, int quarter, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        var dayNumber = ComputeQuarterEndDayNumber(year, quarter, GetQuarterDefinition(definition));
        dayNumber += (dayOfWeek - DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber) + 7) % 7;
        return DateOnly.FromDayNumber(dayNumber);
    }
}
