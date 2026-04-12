// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.IsWeekend.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> falls on a weekend using the default
    /// <see cref="CalendarWeekendDefinition.SaturdaySunday" /> rule.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <returns><see langword="true" /> if the date falls on a Saturday or Sunday; otherwise, <see langword="false" />.</returns>
    /// <remarks>This method uses the standard weekend definition, where Saturday and Sunday are considered weekend days.</remarks>
    public static bool IsWeekend(this DateTime dateTime) => IsWeekend(dateTime.DayOfWeek, CalendarWeekendDefinition.SaturdaySunday, null);

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> falls on a weekend using the provided
    /// <see cref="CalendarWeekendDefinition" /> rule and optional custom provider.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="weekend">A <see cref="CalendarWeekendDefinition" /> value that defines which days are treated as weekend days.</param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider" /> that provides custom logic for evaluating weekend days when
    /// <paramref name="weekend" /> is set to <see cref="CalendarWeekendDefinition.Custom" />.
    /// </param>
    /// <returns><see langword="true" /> if the date falls on a weekend day as defined by the rule or provider; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend" /> is <see cref="CalendarWeekendDefinition.Custom" /> and <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This method supports alternative weekend definitions used in different cultures and regions, such as Friday/Saturday or Sunday-only.
    /// </remarks>
    public static bool IsWeekend(this DateTime dateTime, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => IsWeekend(dateTime.DayOfWeek, weekend, provider);

    /// <summary>
    /// Returns an indication whether the specified <see cref="DayOfWeek" /> value is considered a weekend day based on the specified
    /// <see cref="CalendarWeekendDefinition" /> and optional custom provider.
    /// </summary>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek" /> value to evaluate.</param>
    /// <param name="weekend">The weekend rule that defines which days are considered part of the weekend.</param>
    /// <param name="provider">
    /// A custom <see cref="IWeekendDefinitionProvider" /> used only when <paramref name="weekend" /> is set to <see cref="CalendarWeekendDefinition.Custom" />.
    /// </param>
    /// <returns><see langword="true" /> if the day is considered part of the weekend; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend" /> is <see cref="CalendarWeekendDefinition.Custom" /> and <paramref name="provider" /> is
    /// <see langword="null" />, or if <paramref name="weekend" /> is not a defined value.
    /// </exception>
    /// <remarks>This method supports custom weekend evaluation logic via <paramref name="provider" /> when using <see cref="CalendarWeekendDefinition.Custom" />.</remarks>
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
            CalendarWeekendDefinition.Custom when provider is not null => provider.IsWeekend(dayOfWeek),

            _ => throw new ArgumentOutOfRangeException(
                nameof(weekend),
                string.Format(ResourceStrings.Arg_OutOfRangeException_EnumValue, weekend, nameof(CalendarWeekendDefinition)))
        };
    }
}
