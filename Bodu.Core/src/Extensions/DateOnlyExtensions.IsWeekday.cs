// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekday, using the default <see cref="CalendarWeekendDefinition.SaturdaySunday"/> rule.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> does not fall on Saturday or Sunday; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>A weekday is any day not considered a weekend under the <see cref="CalendarWeekendDefinition.SaturdaySunday"/> rule. This overload is equivalent to calling <see cref="IsWeekday(DateOnly, CalendarWeekendDefinition, IWeekendDefinitionProvider?)"/> with <see cref="CalendarWeekendDefinition.SaturdaySunday"/> and no custom provider.</para>
    /// </remarks>
    public static bool IsWeekday(this DateOnly date) => !IsWeekend(date, CalendarWeekendDefinition.SaturdaySunday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekday, using the supplied <see cref="CalendarWeekendDefinition"/> and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are considered weekends.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> is not a weekend under the supplied rule or provider; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>The method evaluates whether the <see cref="DateOnly.DayOfWeek"/> of <paramref name="date"/> is excluded from the weekend definition supplied by <paramref name="weekend"/> and optionally refined by <paramref name="provider"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration,
    /// -or- <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsWeekday(this DateOnly date, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => !IsWeekend(date, weekend, provider);
}
