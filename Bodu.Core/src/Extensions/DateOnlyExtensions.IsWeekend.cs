// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsWeekend.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekend using the default
    /// <see cref="CalendarWeekendDefinition.SaturdaySunday"/> rule.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly"/> to evaluate.</param>
    /// <returns><see langword="true"/> if the <paramref name="date"/> occurs on Saturday or Sunday; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This method assumes the standard Western weekend definition, where weekends are Saturday and Sunday.</remarks>
    public static bool IsWeekend(this DateOnly date) => DateTimeExtensions.IsWeekend(date.DayOfWeek, CalendarWeekendDefinition.SaturdaySunday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekend, based on the provided
    /// <see cref="CalendarWeekendDefinition"/> rule.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly"/> to evaluate.</param>
    /// <param name="weekend">
    /// The <see cref="CalendarWeekendDefinition"/> value that defines which days are considered part of the weekend.
    /// </param>
    /// <param name="provider">
    /// An optional custom <see cref="IWeekendDefinitionProvider"/> used when <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="date"/> occurs on a weekend day according to the specified rule or custom
    /// provider; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is <see cref="CalendarWeekendDefinition.Custom"/> but no <paramref name="provider"/> is supplied.
    /// </exception>
    /// <remarks>
    /// Use this method to evaluate weekend status under different global or cultural definitions, such as Friday/Saturday in the Middle
    /// East or Sunday-only in some business contexts.
    /// </remarks>
    public static bool IsWeekend(this DateOnly date, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => DateTimeExtensions.IsWeekend(date.DayOfWeek, weekend, provider);
}