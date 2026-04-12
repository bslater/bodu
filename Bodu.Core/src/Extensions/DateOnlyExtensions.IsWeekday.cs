// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.IsWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly" /> falls on a weekday using the default
    /// <see cref="CalendarWeekendDefinition.SaturdaySunday" /> rule.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> to evaluate.</param>
    /// <returns><see langword="true" /> if the <paramref name="date" /> is not Saturday or Sunday; otherwise, <see langword="false" />.</returns>
    /// <remarks>A weekday is defined as any day not considered a weekend by the default rule.</remarks>
    public static bool IsWeekday(this DateOnly date) => !IsWeekend(date, CalendarWeekendDefinition.SaturdaySunday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateOnly" /> falls on a weekday, based on the provided
    /// <see cref="CalendarWeekendDefinition" /> rule.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> to evaluate.</param>
    /// <param name="weekend">A <see cref="CalendarWeekendDefinition" /> value that defines which days are considered part of the weekend.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider" /> used for custom weekend rules.</param>
    /// <returns>
    /// <see langword="true" /> if the <paramref name="date" /> is considered a weekday under the specified weekend rule; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend" /> is <see cref="CalendarWeekendDefinition.Custom" /> but <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWeekday(this DateOnly date, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider = null) => !IsWeekend(date, weekend, provider);
}