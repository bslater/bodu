// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDayOfWeekInQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the first occurrence of the specified <see cref="DayOfWeek" /> within the quarter that contains the specified
    /// <see cref="DateOnly" />, using the provided <see cref="CalendarQuarterDefinition" /> to determine the quarter boundaries.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly" /> used to identify the calendar quarter.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> value to locate within the quarter. For example, <see cref="DayOfWeek.Monday" /> returns the first
    /// Monday in the quarter.
    /// </param>
    /// <param name="definition">The <see cref="CalendarQuarterDefinition" /> that defines how quarters are segmented.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value representing the first occurrence of <paramref name="dayOfWeek" /> in the quarter that includes <paramref name="date" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> or <paramref name="definition" /> is not a defined enumeration value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The result is calculated by first determining the start day number of the quarter containing <paramref name="date" /> (as defined by
    /// <paramref name="definition" />), and then finding the first occurrence of <paramref name="dayOfWeek" /> on or after that day.
    /// </para>
    /// <para>The returned date is guaranteed to fall within the same quarter as <paramref name="date" />.</para>
    /// </remarks>
    public static DateOnly FirstDayOfWeekInQuarter(this DateOnly date, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);

        (int year, int quarter) = GetQuarterAndYearFromDate(definition, referenceDate: date);
        int days = ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(definition));
        days += (dayOfWeek - DateOnlyExtensions.GetDayOfWeekFromDayNumber(days) + 7) % 7;
        return DateOnly.FromDayNumber(days);
    }

    /// <summary>
    /// Returns the first occurrence of the specified <see cref="DayOfWeek" /> within the given calendar quarter and year, using the
    /// provided <see cref="CalendarQuarterDefinition" /> to determine quarter boundaries.
    /// </summary>
    /// <param name="year">The calendar year in which the specified quarter occurs.</param>
    /// <param name="quarter">The 1-based quarter number (1 through 4).</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> value to locate within the quarter. For example, <see cref="DayOfWeek.Monday" /> returns the first
    /// Monday in the quarter.
    /// </param>
    /// <param name="definition">
    /// The <see cref="CalendarQuarterDefinition" /> that defines how quarters are segmented, such as calendar year or fiscal year.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> representing the first occurrence of <paramref name="dayOfWeek" /> within the specified quarter and year,
    /// as defined by <paramref name="definition" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="quarter" /> is not between 1 and 4 inclusive, or if <paramref name="definition" /> or
    /// <paramref name="dayOfWeek" /> is not a defined enumeration value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The calculation begins from the first day number of the quarter (as defined by <paramref name="definition" />) and advances forward
    /// to find the first occurrence of the specified <paramref name="dayOfWeek" />.
    /// </para>
    /// <para>The returned value is based on <see cref="DateOnly" /> and does not include any time or kind component.</para>
    /// </remarks>
    public static DateOnly FirstDayOfWeekInQuarter(int year, int quarter, DayOfWeek dayOfWeek, CalendarQuarterDefinition definition)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(definition);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        int days = ComputeQuarterStartDayNumber(year, quarter, GetQuarterDefinition(definition));
        days += (dayOfWeek - DateOnlyExtensions.GetDayOfWeekFromDayNumber(days) + 7) % 7;
        return DateOnly.FromDayNumber(days);
    }
}
