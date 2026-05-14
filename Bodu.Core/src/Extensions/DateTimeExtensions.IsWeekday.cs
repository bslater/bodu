// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateTime"/> falls on a weekday, using the default <see cref="CalendarWeekendDefinition.SaturdaySunday"/> rule.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> does not fall on Saturday or Sunday; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>A weekday is any day not considered a weekend under the <see cref="CalendarWeekendDefinition.SaturdaySunday"/> rule. This overload is equivalent to calling <see cref="IsWeekday(DateTime, CalendarWeekendDefinition, IWeekendDefinitionProvider?)"/> with <see cref="CalendarWeekendDefinition.SaturdaySunday"/> and no custom provider.</para>
    /// </remarks>
    public static bool IsWeekday(this DateTime dateTime) => !IsWeekend(dateTime, CalendarWeekendDefinition.SaturdaySunday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateTime"/> falls on a weekday, using the supplied <see cref="CalendarWeekendDefinition"/> and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are considered weekends.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> is not a weekend under the supplied rule or provider; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>The method evaluates whether the <see cref="DateTime.DayOfWeek"/> of <paramref name="dateTime"/> is excluded from the weekend definition supplied by <paramref name="weekend"/> and optionally refined by <paramref name="provider"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration,
    /// -or- <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsWeekday(this DateTime dateTime, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => !IsWeekend(dateTime, weekend, provider);

    /// <summary>
    /// Determines whether the specified <see cref="DayOfWeek"/> is considered a weekday, using the supplied <see cref="CalendarWeekendDefinition"/> and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> value to evaluate.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are considered weekends.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="dayOfWeek"/> is not a weekend under the supplied rule or provider; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This method is equivalent to <c>!IsWeekend(dayOfWeek, weekend, provider)</c>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration,
    /// -or- <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsWeekday(DayOfWeek dayOfWeek, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => !IsWeekend(dayOfWeek, weekend, provider);
}
