// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Contains unit tests for <see cref="AdjustmentPolicy" /> members called directly, outside the resolution pipeline
/// that <see cref="AdjustmentTriggerMatrixTests" /> drives.
/// </summary>
[TestClass]
public sealed partial class AdjustmentPolicyTests
{
    /// <summary>
    /// Creates a policy that fires on the nth occurrence of a weekday in the month.
    /// </summary>
    /// <param name="ordinal">The configured week ordinal, which may be <see langword="null" /> or undefined.</param>
    /// <param name="weekdays">The configured trigger weekdays.</param>
    /// <returns>The policy.</returns>
    private static AdjustmentPolicy NthOccurrencePolicy(WeekOrdinal? ordinal, params DayOfWeek[] weekdays) =>
        new(
            "nth",
            priority: 0,
            AdjustmentScope.Global,
            AdjustmentTrigger.IfNthOccurrenceInMonth,
            weekdays,
            AdjustmentAction.AddDays,
            actionWeekday: null,
            actionDays: 1,
            maxSearchDays: null,
            skipWeekends: false,
            skipNonWorkingDates: false,
            EmissionMode.ObservedOnly,
            reason: null,
            nonWorking: null,
            triggerWeekOrdinal: ordinal);
}
