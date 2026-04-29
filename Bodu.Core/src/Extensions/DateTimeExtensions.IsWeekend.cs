// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsWeekend.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateTime"/> falls on a weekend, using the default <see cref="CalendarWeekendDefinition.SaturdaySunday"/> rule.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> falls on Saturday or Sunday; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This overload uses the standard weekend definition where Saturday and Sunday are considered weekend days.</para>
    /// </remarks>
    public static bool IsWeekend(this DateTime dateTime) => IsWeekend(dateTime.DayOfWeek, CalendarWeekendDefinition.SaturdaySunday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateTime"/> falls on a weekend, using the supplied <see cref="CalendarWeekendDefinition"/> and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are treated as weekend days.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> falls on a weekend day as defined by the supplied rule or provider; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This method supports alternative weekend definitions used in different cultures and regions, such as Friday/Saturday or Sunday-only.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> and <paramref name="provider"/> is <see langword="null"/>, or if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration.</exception>
    public static bool IsWeekend(this DateTime dateTime, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => IsWeekend(dateTime.DayOfWeek, weekend, provider);

    /// <summary>
    /// Determines whether the specified <see cref="DayOfWeek"/> is considered a weekend day, using the supplied <see cref="CalendarWeekendDefinition"/> and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> value to evaluate.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are considered weekend days.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="dayOfWeek"/> is considered a weekend day; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This overload supports custom weekend evaluation logic via <paramref name="provider"/> when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration, or if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration, or if <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.</exception>
    public static bool IsWeekend(DayOfWeek dayOfWeek, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return weekend switch
        {
            CalendarWeekendDefinition.SaturdaySunday => dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            CalendarWeekendDefinition.FridaySaturday => dayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday,
            CalendarWeekendDefinition.ThursdayFriday => dayOfWeek is DayOfWeek.Thursday or DayOfWeek.Friday,
            CalendarWeekendDefinition.SundayOnly => dayOfWeek == DayOfWeek.Sunday,
            CalendarWeekendDefinition.FridayOnly => dayOfWeek == DayOfWeek.Friday,
            CalendarWeekendDefinition.None => false,
            CalendarWeekendDefinition.Custom when provider is not null => provider.IsWeekend(dayOfWeek),

            _ => throw new ArgumentOutOfRangeException(
                nameof(weekend),
                string.Format(ResourceStrings.Arg_OutOfRangeException_EnumValue, weekend, nameof(CalendarWeekendDefinition)))
        };
    }
}
