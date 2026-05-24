// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateResolutionStrategy.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies the strategy a <see cref="NotableDateRule" /> uses to resolve its anchor date for a given year.
/// </summary>
/// <remarks>
/// This enumeration replaces <c>NotableDateDefinitionType</c>. Each value corresponds to a distinct branch in the
/// <see cref="NotableDateRuleResolver" /> and to a distinct <c>&lt;Definition&gt;</c> child element in the XML schema.
/// </remarks>
public enum DateResolutionStrategy
{
    /// <summary>
    /// Resolved from a fixed month and day, identical every year.
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// Resolved as the n-th occurrence of a specified weekday within a specified month (e.g. the second Monday of
    /// March).
    /// </summary>
    DayOfWeekInMonth,

    /// <summary>
    /// Resolved by an algorithmic <see cref="INotableDateAlgorithm" /> implementation looked up via key in the
    /// algorithm registry.
    /// </summary>
    Algorithm,

    /// <summary>
    /// Resolved as a fixed integer day offset from another notable date rule referenced by name (e.g. Easter Monday =
    /// Easter Sunday + 1).
    /// </summary>
    OffsetFromAnchor,
}
