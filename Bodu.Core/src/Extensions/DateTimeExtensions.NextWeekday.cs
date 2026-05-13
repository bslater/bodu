// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.NextWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the next calendar weekday after the specified <paramref name="dateTime"/>, based on the supplied <paramref name="weekend"/> definition.
    /// </summary>
    /// <param name="dateTime">The starting date and time value from which to search forward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are considered weekends.</param>
    /// <returns>An object whose value is set to the first calendar day after <paramref name="dateTime"/> that is not a weekend under the specified <paramref name="weekend"/> rule, with the original time-of-day and <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>The method evaluates each successive day until it finds one that is not designated as a weekend by the specified rule. The original <paramref name="dateTime"/> is never returned, even if it already falls on a weekday.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration.</exception>
    public static DateTime NextWeekday(this DateTime dateTime, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        var ticks = dateTime.Ticks;
        do
        {
            ticks += TicksPerDay;
        }
        while (IsWeekend(GetDayOfWeekFromTicks(ticks), weekend));

        return new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the next calendar weekday after the specified <paramref name="dateTime"/>, using the supplied <paramref name="weekend"/> definition and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="dateTime">The starting date and time value from which to search forward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are considered weekends.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>. If <see langword="null"/>, the default behavior for the supplied <paramref name="weekend"/> applies.</param>
    /// <returns>An object whose value is set to the first calendar day after <paramref name="dateTime"/> that is not a weekend under the specified <paramref name="weekend"/> rule and the logic of <paramref name="provider"/>, with the original time-of-day and <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>The method evaluates each successive day following <paramref name="dateTime"/> until it finds one that is not considered a weekend, either by the supplied <paramref name="weekend"/> pattern or by the custom logic of <paramref name="provider"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration.</exception>
    public static DateTime NextWeekday(this DateTime dateTime, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        var ticks = dateTime.Ticks;
        do
        {
            ticks += TicksPerDay;
        }
        while (IsWeekend(GetDayOfWeekFromTicks(ticks), weekend, provider));

        return new DateTime(ticks, dateTime.Kind);
    }
}
