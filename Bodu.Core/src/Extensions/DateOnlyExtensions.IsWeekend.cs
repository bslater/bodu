// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsWeekend.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekend, using the default <see cref="CalendarWeekendDefinition.SaturdaySunday"/> rule.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> falls on Saturday or Sunday; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This overload uses the standard weekend definition where Saturday and Sunday are considered weekend days.</para>
    /// </remarks>
    public static bool IsWeekend(this DateOnly date) => DateTimeExtensions.IsWeekend(date.DayOfWeek, CalendarWeekendDefinition.SaturdaySunday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekend, using the supplied <see cref="CalendarWeekendDefinition"/> and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that determines which days are treated as weekend days.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> falls on a weekend day as defined by the supplied rule or provider; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This method supports alternative weekend definitions used in different cultures and regions, such as Friday/Saturday or Sunday-only.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a defined value of the <see cref="CalendarWeekendDefinition"/> enumeration,
    /// -or- <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsWeekend(this DateOnly date, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => DateTimeExtensions.IsWeekend(date.DayOfWeek, weekend, provider);
}
