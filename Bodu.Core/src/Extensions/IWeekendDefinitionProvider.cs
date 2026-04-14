// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IWeekendDefinitionProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

/// <summary>
/// Defines a provider interface for determining whether a given <see cref="DayOfWeek"/> is considered part of the weekend.
/// </summary>
/// <remarks>
/// Implement this interface to supply custom weekend definitions that are not covered by the built-in
/// <see cref="CalendarWeekendDefinition"/> values. This allows support for region-specific, cultural, or organizational weekend
/// patterns beyond standard configurations such as <see cref="CalendarWeekendDefinition.SaturdaySunday"/> or <see cref="CalendarWeekendDefinition.FridaySaturday"/>.
/// </remarks>
public interface IWeekendDefinitionProvider
{
    /// <summary>
    /// Determines whether the specified <see cref="DayOfWeek"/> is considered part of the weekend.
    /// </summary>
    /// <param name="dayOfWeek">The day of the week to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the specified <paramref name="dayOfWeek"/> is considered part of the weekend; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method applies the logic defined by the implementing provider, which may differ from the commonly used
    /// <see cref="CalendarWeekendDefinition"/> values such as <see cref="CalendarWeekendDefinition.SaturdaySunday"/> or
    /// <see cref="CalendarWeekendDefinition.FridayOnly"/>. Custom implementations enable support for hybrid, rotating, or
    /// domain-specific weekend rules.
    /// </remarks>
    bool IsWeekend(DayOfWeek dayOfWeek);
}