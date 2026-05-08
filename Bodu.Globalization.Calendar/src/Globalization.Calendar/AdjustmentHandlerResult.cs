// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerResult.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Conveys the outcome of an <see cref="IAdjustmentHandler" /> evaluation back to the adjuster pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The result tells the adjuster three things, with downstream effects on the emitted <see cref="NotableDate" />:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Activated" /> indicates whether the handler considered the trigger condition met. When
/// <see langword="false" />, the original calculated date is preserved and no extra <see cref="NotableDate" /> is emitted; an
/// <see cref="AdjustmentReason" /> is not attached.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="AdjustedDate" /> supplies the relocated date. When this differs from the originally calculated date the adjuster
/// emits an additional <see cref="NotableDate" /> with <see cref="NotableDate.AdjustmentReason" /> populated, leaving the
/// original occurrence in place so consumers can still see the unshifted date if needed.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="IsNonWorkingOverride" /> optionally overrides the non-working flag inherited from the rule or adjustment. Use it
/// when the handler's outcome materially changes the observance status — for example marking a substitute day off as
/// non-working even though the rule itself is observance-only.
/// </description>
/// </item>
/// </list>
/// <para>
/// To signal "no change", return <c>new AdjustmentHandlerResult(Activated: false, AdjustedDate: context.Date)</c>. To signal a
/// shift without changing the working-day status, leave <see cref="IsNonWorkingOverride" /> as <see langword="null" />.
/// </para>
/// </remarks>
/// <param name="Activated">Whether the handler considered its trigger satisfied.</param>
/// <param name="AdjustedDate">The new date when <paramref name="Activated" /> is <see langword="true" />, otherwise the unchanged date.</param>
/// <param name="IsNonWorkingOverride">Optional override for the resulting date's non-working flag. <see langword="null" /> preserves the existing value.</param>
public sealed record AdjustmentHandlerResult(bool Activated, DateTime AdjustedDate, bool? IsNonWorkingOverride = null);
