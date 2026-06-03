// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTrigger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Identifies the condition that determines whether an adjustment policy applies to a calculated occurrence.
/// </summary>
public enum AdjustmentTrigger
{
    /// <summary>
    /// The adjustment always applies, regardless of the calculated occurrence.
    /// </summary>
    Always = 0,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls on one of the policy's configured weekdays.
    /// </summary>
    IfDayOfWeek,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls on a Saturday or Sunday.
    /// </summary>
    IfWeekend,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls on a Monday through Friday.
    /// </summary>
    IfWeekday,
}
