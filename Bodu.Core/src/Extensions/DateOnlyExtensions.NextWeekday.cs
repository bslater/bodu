// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.NextWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the next calendar weekday after the given <paramref name="date" />, based on
    /// the specified <paramref name="weekend" /> definition.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to search forward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition" /> that defines which days are considered weekends.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> representing the next weekday after <paramref name="date" />, according to the specified
    /// <paramref name="weekend" /> pattern.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend" /> is not a valid <see cref="CalendarWeekendDefinition" /> value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="date" /> is a weekday, the result will be the next calendar day that is also a weekday based on the weekend definition.
    /// </para>
    /// <para>The returned value is a pure calendar date and does not include any time-of-day or kind metadata.</para>
    /// </remarks>
    public static DateOnly NextWeekday(this DateOnly date, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        int dayNumber = date.DayNumber;
        do
        {
            dayNumber += 1;
        }
        while (DateTimeExtensions.IsWeekend(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber), weekend));

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the next calendar weekday after the given <paramref name="date" />, based on
    /// the specified <paramref name="weekend" /> definition and optional <paramref name="provider" />.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to search forward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition" /> that defines the default pattern of weekend days.</param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider" /> that can override the weekend definition with custom logic. If
    /// <c>null</c>, the behavior falls back to the rules defined by <paramref name="weekend" />.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> representing the next weekday after <paramref name="date" />, according to the specified
    /// <paramref name="weekend" /> definition and optional <paramref name="provider" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend" /> is not a valid value of the <see cref="CalendarWeekendDefinition" /> enumeration.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="date" /> is a weekday, the result will be the next calendar day that is also a weekday, as determined by
    /// either the default <paramref name="weekend" /> pattern or the logic provided by <paramref name="provider" />.
    /// </para>
    /// <para>The returned value is a calendar-only result with no time or kind metadata.</para>
    /// </remarks>
    public static DateOnly NextWeekday(this DateOnly date, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        int dayNumber = date.DayNumber;
        do
        {
            dayNumber += 1;
        }
        while (DateTimeExtensions.IsWeekend(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber), weekend, provider));

        return DateOnly.FromDayNumber(dayNumber);
    }
}