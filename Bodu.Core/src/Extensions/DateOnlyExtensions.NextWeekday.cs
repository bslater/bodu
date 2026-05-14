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
    /// Returns a new <see cref="DateOnly"/> representing the next calendar weekday after the specified <paramref name="date"/>, based on the supplied <paramref name="weekend"/> definition.
    /// </summary>
    /// <param name="date">The starting date value from which to search forward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are considered weekends.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first calendar day after <paramref name="date"/> that is not a weekend under the specified <paramref name="weekend"/> rule.</returns>
    /// <remarks>
    /// <para>The method evaluates each successive day until it finds one that is not designated as a weekend by the specified rule. The original <paramref name="date"/> is never returned, even if it already falls on a weekday.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration.</exception>
    public static DateOnly NextWeekday(this DateOnly date, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        var dayNumber = date.DayNumber;
        do
        {
            dayNumber += 1;
        }
        while (DateTimeExtensions.IsWeekend(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber), weekend));

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the next calendar weekday after the specified <paramref name="date"/>, using the supplied <paramref name="weekend"/> definition and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="date">The starting date value from which to search forward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are considered weekends.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>. If <see langword="null"/>, the default behavior for the supplied <paramref name="weekend"/> applies.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first calendar day after <paramref name="date"/> that is not a weekend under the specified <paramref name="weekend"/> rule and the logic of <paramref name="provider"/>.</returns>
    /// <remarks>
    /// <para>The method evaluates each successive day following <paramref name="date"/> until it finds one that is not considered a weekend, either by the supplied <paramref name="weekend"/> pattern or by the custom logic of <paramref name="provider"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration.</exception>
    public static DateOnly NextWeekday(this DateOnly date, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        var dayNumber = date.DayNumber;
        do
        {
            dayNumber += 1;
        }
        while (DateTimeExtensions.IsWeekend(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber), weekend, provider));

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the next day after <paramref name="date" /> whose
    /// <see cref="DayOfWeek" /> is selected in the supplied <paramref name="workingWeek" />.
    /// </summary>
    /// <param name="date">The starting date value from which to search forward.</param>
    /// <param name="workingWeek">The working-week pattern that determines which days are considered working days.</param>
    /// <returns>The first calendar day strictly after <paramref name="date" /> whose day-of-week is selected in <paramref name="workingWeek" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="workingWeek" /> is <see cref="WeekPattern.Empty" />.</exception>
    public static DateOnly NextWeekday(this DateOnly date, WeekPattern workingWeek)
    {
        if (workingWeek.Count == 0) throw new ArgumentOutOfRangeException(nameof(workingWeek), "The working week must select at least one day.");

        var dayNumber = date.DayNumber;
        do
        {
            dayNumber += 1;
        }
        while (!workingWeek.Contains(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber)));

        return DateOnly.FromDayNumber(dayNumber);
    }
}
