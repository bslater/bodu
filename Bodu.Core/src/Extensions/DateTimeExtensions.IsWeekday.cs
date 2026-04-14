// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.IsWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime"/> falls on a weekday using the default
    /// <see cref="CalendarWeekendDefinition.SaturdaySunday"/> rule.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to evaluate.</param>
    /// <returns><see langword="true"/> if the date is not a Saturday or Sunday; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// A weekday is defined as any day not considered part of the weekend under the <see cref="CalendarWeekendDefinition.SaturdaySunday"/> rule.
    /// </para>
    /// <para>
    /// This method is equivalent to calling <see cref="IsWeekday(DateTime, CalendarWeekendDefinition, IWeekendDefinitionProvider?)"/> with
    /// <see cref="CalendarWeekendDefinition.SaturdaySunday"/> and no custom provider.
    /// </para>
    /// </remarks>
    public static bool IsWeekday(this DateTime dateTime) => !IsWeekend(dateTime, CalendarWeekendDefinition.SaturdaySunday, null);

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime"/> falls on a weekday, based on the provided
    /// <see cref="CalendarWeekendDefinition"/> rule.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to evaluate.</param>
    /// <param name="weekend">A <see cref="CalendarWeekendDefinition"/> value that defines which days are considered weekends.</param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider"/> used to resolve weekend days when <paramref name="weekend"/> is set to <see cref="CalendarWeekendDefinition.Custom"/>.
    /// </param>
    /// <returns><see langword="true"/> if the date is not considered part of the weekend; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method checks whether the day represented by <paramref name="dateTime"/> is excluded from the weekend definition provided by
    /// <paramref name="weekend"/> and optionally refined by <paramref name="provider"/>.
    /// </para>
    /// <para>If the input falls outside the weekend range, it is considered a weekday.</para>
    /// </remarks>
    public static bool IsWeekday(this DateTime dateTime, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => !IsWeekend(dateTime, weekend, provider);

    /// <summary>
    /// Returns an indication whether the specified <see cref="DayOfWeek"/> value is considered a weekday based on the given weekend rule
    /// or provider.
    /// </summary>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> value to evaluate.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that defines which days are treated as part of the weekend.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> used when <paramref name="weekend"/> is set to <see cref="CalendarWeekendDefinition.Custom"/>.</param>
    /// <returns><see langword="true"/> if the day is not considered part of the weekend; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method evaluates whether <paramref name="dayOfWeek"/> is excluded from the weekend days as defined by
    /// <paramref name="weekend"/>. If a custom weekend rule is specified, the evaluation is delegated to the given <paramref name="provider"/>.
    /// </para>
    /// <para>This method is equivalent to <c>!IsWeekend(dayOfWeek, weekend, provider)</c>.</para>
    /// </remarks>
#pragma warning disable S2190 // Loops and recursions should not be infinite

    public static bool IsWeekday(DayOfWeek dayOfWeek, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => !IsWeekday(dayOfWeek, weekend, provider);

#pragma warning restore S2190 // Loops and recursions should not be infinite
}
