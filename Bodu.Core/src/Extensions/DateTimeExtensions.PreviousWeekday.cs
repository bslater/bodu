// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.PreviousWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the previous calendar weekday before the specified <paramref name="dateTime"/>,
    /// based on the provided <paramref name="weekend"/> definition.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime"/> from which to search backward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that defines which days are considered weekends.</param>
    /// <returns>
    /// An object whose value is set to the first calendar day before <paramref name="dateTime"/> that is not considered a weekend
    /// according to the specified <paramref name="weekend"/> rule.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a valid member of the <see cref="CalendarWeekendDefinition"/> enumeration.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="dateTime"/> already falls on a weekday, the result will be the most recent calendar day that is also a weekday
    /// under the given <paramref name="weekend"/> pattern.
    /// </para>
    /// <para>This method evaluates each preceding day until it finds one that is not designated as a weekend by the specified rule.</para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    public static DateTime PreviousWeekday(this DateTime dateTime, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        long ticks = dateTime.Ticks;
        do
        {
            ticks -= TicksPerDay;
        }
        while (IsWeekend(GetDayOfWeekFromTicks(ticks), weekend));

        return new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the previous calendar weekday before the specified <paramref name="dateTime"/>,
    /// using the given <paramref name="weekend"/> definition and optional custom <paramref name="provider"/> logic.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime"/> from which to search backward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that defines the weekend pattern.</param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="weekend"/> is set to <see cref="CalendarWeekendDefinition.Custom"/>.
    /// </param>
    /// <returns>
    /// An object whose value is set to the first calendar day before <paramref name="dateTime"/> that is considered a weekday according to
    /// the specified <paramref name="weekend"/> rule and the logic of <paramref name="provider"/> (if applicable).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method evaluates each preceding day prior to <paramref name="dateTime"/> until it finds one that is not considered a weekend
    /// under the specified <paramref name="weekend"/> pattern or the custom logic defined by <paramref name="provider"/>.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    public static DateTime PreviousWeekday(this DateTime dateTime, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        long ticks = dateTime.Ticks;
        do
        {
            ticks -= TicksPerDay;
        }
        while (IsWeekend(GetDayOfWeekFromTicks(ticks), weekend, provider));

        return new DateTime(ticks, dateTime.Kind);
    }
}
