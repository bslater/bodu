// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTrigger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies the condition that determines whether an adjustment policy applies to a calculated occurrence.
/// </summary>
/// <seealso cref="AdjustmentAction" />
/// <seealso cref="AdjustmentPolicy" />
/// <seealso href="../guides/calendar/adjustment-rules.html">Observance adjustment rules (guide)</seealso>
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
    /// The adjustment applies when the calculated occurrence falls on a weekend: a day outside the resource's working
    /// week (Saturday or Sunday by default).
    /// </summary>
    IfWeekend,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls on a working day within the resource's working week
    /// (Monday through Friday by default).
    /// </summary>
    IfWeekday,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls on a non-working day: a day outside the resource's
    /// working week, or a day already claimed by another non-working occurrence.
    /// </summary>
    IfNonWorkingDay,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls on a working day within the resource's working week
    /// that is not claimed by another non-working occurrence.
    /// </summary>
    IfWorkingDay,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls in a Gregorian leap year.
    /// </summary>
    IfLeapYear,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls before the policy's comparison month and day in the
    /// same year.
    /// </summary>
    IfBeforeFixedDate,

    /// <summary>
    /// The adjustment applies when the calculated occurrence falls after the policy's comparison month and day in the
    /// same year.
    /// </summary>
    IfAfterFixedDate,

    /// <summary>
    /// The adjustment applies when the calculated occurrence is the configured nth weekday of its month.
    /// </summary>
    IfNthOccurrenceInMonth,

    /// <summary>
    /// The adjustment applies when a caller-supplied <see cref="IAdjustmentTriggerHandler" />, bound through the
    /// policy's trigger handler key, reports that it should fire.
    /// </summary>
    Custom,
}
