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
/// Working-day aware actions treat Saturday and Sunday as non-working days. A pluggable non-working-day calendar that
/// also skips holidays is reserved for a later phase.
/// </para>
/// </remarks>
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
    /// Move the occurrence forward to the next instance of the configured weekday.
    /// </summary>
    MoveToNextWeekday,

    /// <summary>
    /// Move the occurrence backward to the previous instance of the configured weekday.
    /// </summary>
    MoveToPreviousWeekday,

    /// <summary>
    /// Move the occurrence forward to the next working day, skipping weekends.
    /// </summary>
    MoveToNextWorkingDay,

    /// <summary>
    /// Move the occurrence backward to the previous working day, skipping weekends.
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
    /// Delegate the observed-date computation to a caller-supplied <see cref="IAdjustmentHandler" /> resolved through the
    /// policy's handler key.
    /// </summary>
    Custom,
}
