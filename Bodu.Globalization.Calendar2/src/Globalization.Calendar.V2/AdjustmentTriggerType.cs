// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Identifies the condition that determines whether an adjustment policy applies to a calculated occurrence.
/// </summary>
/// <remarks>
/// <para>
/// The first cut of the v2 engine supports unconditional and weekday-membership triggers. Additional trigger types
/// (working-day tests, fixed-date comparisons, collisions, custom handlers) are reserved for a later phase.
/// </para>
/// </remarks>
public enum AdjustmentTriggerType
{
    /// <summary>
    /// The adjustment always applies, regardless of the calculated occurrence.
    /// </summary>
    Always = 0,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls on one of the configured weekdays.
    /// </summary>
    FallsOn,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls on a Saturday or Sunday.
    /// </summary>
    IfWeekend,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls on a Monday through Friday.
    /// </summary>
    IfWeekday,
}
