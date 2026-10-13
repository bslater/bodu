// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentAction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies how an adjustment policy transforms a calculated occurrence once its trigger is active.
/// </summary>
/// <remarks>
/// <para>
/// Working-day aware actions treat the days outside the resource's working week as non-working. Whether they also step
/// over days already claimed by other non-working occurrences is controlled by the owning policy's
/// <see cref="AdjustmentPolicy.SkipNonWorkingDates" /> flag.
/// </para>
/// </remarks>
/// <seealso cref="AdjustmentTrigger" /> <seealso cref="AdjustmentPolicy" />
/// <seealso href="../guides/calendar/adjustment-rules.html">Observance adjustment rules (guide)</seealso>
public enum AdjustmentAction
{
    /// <summary>
    /// Leave the occurrence unchanged.
    /// </summary>
    None = 0,

    /// <summary>
    /// Shift the occurrence by a fixed number of days.
    /// </summary>
    AddDays,

    /// <summary>
    /// Move the occurrence to the configured weekday (<see cref="AdjustmentPolicy.ActionWeekday" />) on or after the
    /// calculated date; without a configured weekday the date is unchanged.
    /// </summary>
    MoveToNextWeekday,

    /// <summary>
    /// Move the occurrence to the configured weekday (<see cref="AdjustmentPolicy.ActionWeekday" />) on or before the
    /// calculated date; without a configured weekday the date is unchanged.
    /// </summary>
    MoveToPreviousWeekday,

    /// <summary>
    /// Move the occurrence forward, starting the day after the calculated date, to the next day the policy's skip rules
    /// do not block (by default, the next working-week day).
    /// </summary>
    MoveToNextWorkingDay,

    /// <summary>
    /// Move the occurrence backward, starting the day before the calculated date, to the previous day the policy's skip
    /// rules do not block (by default, the previous working-week day).
    /// </summary>
    MoveToPreviousWorkingDay,

    /// <summary>
    /// Replace the occurrence date with the date of another rule resolved for the same year, identified by the policy's
    /// notable-date and rule references.
    /// </summary>
    ReplaceWithRule,

    /// <summary>
    /// Suppress the occurrence entirely.
    /// </summary>
    Suppress,

    /// <summary>
    /// Delegate the observed-date computation to a caller-supplied <see cref="IAdjustmentHandler" /> resolved through
    /// the policy's handler key.
    /// </summary>
    Custom,
}
