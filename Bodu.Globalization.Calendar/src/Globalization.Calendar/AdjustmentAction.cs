// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentAction.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies the action an <see cref="ObservanceAdjustment" /> performs once its <see cref="AdjustmentTrigger" /> has
/// activated.
/// </summary>
/// <remarks>
/// This enumeration replaces <c>NotableDateAdjustmentActionType</c>, dropping the redundant <c>...Type</c> suffix. Each
/// value maps to a branch in <see cref="NotableDateAdjuster" />.
/// </remarks>
public enum AdjustmentAction
{
    /// <summary>
    /// Leaves the date unchanged. Useful when an adjustment is used purely to flag an observance (for example, marking
    /// the date as a non-working day) without rescheduling it.
    /// </summary>
    None = 0,

    /// <summary>
    /// Adds <see cref="ObservanceAdjustment.OffsetDays" /> to the calculated date. Negative values move backwards.
    /// </summary>
    AddDays,

    /// <summary>
    /// Moves the date forward to the next weekday as defined by the configured <see cref="Bodu.WorkingDaysOfWeek" />.
    /// </summary>
    MoveToNextWeekday,

    /// <summary>
    /// Moves the date backward to the previous weekday as defined by the configured
    /// <see cref="Bodu.WorkingDaysOfWeek" />.
    /// </summary>
    MoveToPreviousWeekday,

    /// <summary>
    /// Moves the date forward, skipping all days flagged as non-working for the active territory (weekends and other
    /// notable non-working dates).
    /// </summary>
    MoveToNextNonWorkingDay,

    /// <summary>
    /// Replaces the date with the resolved date of another notable date rule named by
    /// <see cref="ObservanceAdjustment.TargetRuleName" />.
    /// </summary>
    ReplaceWithNamedDate,

    /// <summary>
    /// Delegates the adjustment to a registered <see cref="IAdjustmentHandler" /> looked up by
    /// <see cref="ObservanceAdjustment.HandlerKey" />.
    /// </summary>
    Custom,
}
