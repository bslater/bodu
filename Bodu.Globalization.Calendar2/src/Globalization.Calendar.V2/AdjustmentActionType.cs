// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentActionType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Identifies how an adjustment policy transforms a calculated occurrence once its trigger is active.
/// </summary>
/// <remarks>
/// <para>
/// The first cut of the v2 engine implements the day-shifting actions required by the minimal cookbook. Working-day
/// aware shifting honours weekends only; a pluggable non-working-day calendar is reserved for a later phase.
/// </para>
/// </remarks>
public enum AdjustmentActionType
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
    /// Suppress the occurrence entirely.
    /// </summary>
    Suppress,
}
