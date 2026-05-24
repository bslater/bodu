// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentApplyResult.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Captures the outcome of an <see cref="NotableDateAdjuster.Apply" /> invocation.
/// </summary>
/// <param name="Activated">Whether the adjustment fired.</param>
/// <param name="AdjustedDate">
/// The resulting date. Equal to the original date when <paramref name="Activated" /> is <see langword="false" />.
/// </param>
/// <param name="Trigger">The trigger that produced the result, when activated.</param>
/// <param name="Action">The action that produced the result, when activated.</param>
/// <param name="HandlerKey">The custom handler key invoked, when applicable.</param>
/// <param name="IsNonWorkingOverride">An optional override for the resulting date's non-working flag.</param>
internal readonly record struct AdjustmentApplyResult(
    bool Activated,
    DateTime AdjustedDate,
    AdjustmentTrigger Trigger = default,
    AdjustmentAction Action = default,
    string? HandlerKey = null,
    bool? IsNonWorkingOverride = null)
{
    /// <summary>
    /// Creates a result indicating that the adjustment did not activate.
    /// </summary>
    /// <param name="originalDate">The unchanged date.</param>
    /// <returns>An inactive result.</returns>
    public static AdjustmentApplyResult NotActivated(DateTime originalDate) => new(false, originalDate);
}
