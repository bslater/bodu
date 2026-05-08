// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservanceAdjustment.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies a conditional adjustment that may shift the calculated date of a <see cref="NotableDateRule" /> based on contextual
/// factors such as the day of week, the active territory, or the calendar system.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="ObservanceAdjustment" /> consists of a <see cref="Trigger" /> describing the activation condition and an
/// <see cref="Action" /> describing the resulting modification. Adjustments are evaluated in <see cref="Priority" /> order during date
/// generation and may be scoped to a single territory, calendar, or year range.
/// </para>
/// <para>
/// Multiple adjustments on a single rule allow layered logic — for example, one entry rolls a Saturday observance to Friday and a
/// second entry rolls a Sunday observance to Monday — while <see cref="Key" /> identifiers allow individual entries to be
/// targeted and overridden by <c>&lt;Use&gt;</c> directives when the rule is inherited by another resource.
/// </para>
/// <para>
/// When an adjustment activates the <see cref="NotableDateService" /> emits an <em>additional</em> <see cref="NotableDate" />
/// at the adjusted date, leaving the original calculated date in place so consumers see both the actual and the observed
/// occurrence. The shifted occurrence carries an <see cref="AdjustmentReason" /> on its
/// <see cref="NotableDate.AdjustmentReason" /> property, naming the trigger and action that caused the shift; this also flips
/// <see cref="NotableDate.WasAdjusted" /> to <see langword="true" />. When <see cref="IsNonWorkingDay" /> is set on the
/// adjustment, that value overrides the rule's non-working flag for the shifted occurrence only.
/// </para>
/// <para>
/// This type replaces the earlier <c>NotableDateAdjustmentRule</c>. The new name distinguishes the adjustment <em>specification</em>
/// from the resolution <em>rule</em> that owns it.
/// </para>
/// </remarks>
/// <example>
/// <para>Roll a public holiday to Monday when it falls on a weekend, scoped to all Australian territories:</para>
/// <code>
/// ObservanceAdjustment rollToMonday = new ObservanceAdjustment
/// {
///     Key = "weekend-to-monday",
///     Trigger = AdjustmentTrigger.IfWeekend,
///     Action = AdjustmentAction.MoveToNextMonday,
///     TerritoryCode = "AU",
///     IsNonWorkingDay = true,
/// };
///
/// // In Western Australia only, move ANZAC Day observance from Sunday to Monday:
/// ObservanceAdjustment waSubstitute = new ObservanceAdjustment
/// {
///     Key = "anzac-day-wa-sub",
///     Trigger = AdjustmentTrigger.IfDayOfWeek,
///     DayOfWeek = DayOfWeek.Sunday,
///     Action = AdjustmentAction.MoveToNextMonday,
///     TerritoryCode = "AU-WA",
///     IsNonWorkingDay = true,
///     Priority = 10,
/// };
/// </code>
/// </example>
public sealed record ObservanceAdjustment
{
	/// <summary>
	/// Gets the identifier used to merge this adjustment with inherited adjustments during <c>&lt;Use&gt;</c> directive resolution.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When a <c>&lt;Use&gt;</c> override declares an adjustment whose <see cref="Key" /> matches one already on the inherited rule, the
	/// override replaces the inherited entry at its current position; otherwise the override is appended. Keys are author-defined
	/// strings, unique within a single rule's <see cref="NotableDateRule.Adjustments" /> sequence, and are by convention kebab-case
	/// (for example <c>"weekend-roll"</c>, <c>"boxing-day-monday-sub"</c>, <c>"anzac-day-wa-observance"</c>).
	/// </para>
	/// <para>
	/// The value is informational at run time — the <see cref="NotableDateAdjuster" /> evaluation pipeline does not consult
	/// <see cref="Key" /> once the rule set has been flattened.
	/// </para>
	/// </remarks>
	public required string Key { get; init; }

	/// <summary>
	/// Gets the condition that determines whether the adjustment activates for a given calculated date.
	/// </summary>
	public AdjustmentTrigger Trigger { get; init; }

	/// <summary>
	/// Gets the action applied when the adjustment activates.
	/// </summary>
	public AdjustmentAction Action { get; init; }

	/// <summary>
	/// Gets the day of week required by <see cref="AdjustmentTrigger.IfDayOfWeek" />, or <see langword="null" /> when not applicable.
	/// </summary>
	public DayOfWeek? DayOfWeek { get; init; }

	/// <summary>
	/// Gets a value overriding whether the adjusted date is treated as a non-working day. When <see langword="null" />, the
	/// non-working flag from the owning rule is preserved.
	/// </summary>
	public bool? IsNonWorkingDay { get; init; }

	/// <summary>
	/// Gets the integer day offset applied by <see cref="AdjustmentAction.AddDays" />. Negative values move the date backwards.
	/// </summary>
	public int OffsetDays { get; init; }

	/// <summary>
	/// Gets the comma-separated list of territory codes that scope the adjustment. When <see langword="null" /> or empty, the
	/// adjustment applies in every territory the owning rule applies to.
	/// </summary>
	public string? TerritoryCode { get; init; }

	/// <summary>
	/// Gets the calendar system that scopes the adjustment, or <see langword="null" /> for any calendar.
	/// </summary>
	public Type? CalendarType { get; init; }

	/// <summary>
	/// Gets the inclusive minimum year for which the adjustment is effective, or <see langword="null" /> for no lower bound.
	/// </summary>
	public int? EffectiveFromYear { get; init; }

	/// <summary>
	/// Gets the inclusive maximum year for which the adjustment is effective, or <see langword="null" /> for no upper bound.
	/// </summary>
	public int? EffectiveToYear { get; init; }

	/// <summary>
	/// Gets the comparison date used by <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> and
	/// <see cref="AdjustmentTrigger.IfAfterFixedDate" />.
	/// </summary>
	/// <remarks>
	/// The year component is replaced with the year being resolved at evaluation time, so authors only need to supply a month and day.
	/// </remarks>
	public DateTime? ComparisonDate { get; init; }

	/// <summary>
	/// Gets the ordinal occurrence required by <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" />, or <see langword="null" /> when
	/// not applicable.
	/// </summary>
	public WeekOfMonthOrdinal? WeekOrdinal { get; init; }

	/// <summary>
	/// Gets the name of another <see cref="NotableDateRule" /> referenced by <see cref="AdjustmentAction.ReplaceWithNamedDate" />.
	/// </summary>
	public string? TargetRuleName { get; init; }

	/// <summary>
	/// Gets the evaluation priority. Lower values are evaluated first; the default of 100 leaves room for both higher- and
	/// lower-priority adjustments to be inserted later.
	/// </summary>
	public int Priority { get; init; } = 100;

	/// <summary>
	/// Gets the registry key used to look up an <see cref="IAdjustmentHandler" /> when <see cref="Trigger" /> or <see cref="Action" />
	/// is <see cref="AdjustmentTrigger.Custom" />/<see cref="AdjustmentAction.Custom" />.
	/// </summary>
	public string? HandlerKey { get; init; }

	/// <summary>
	/// Gets an optional dictionary of parameters forwarded to the registered <see cref="IAdjustmentHandler" />.
	/// </summary>
	public IReadOnlyDictionary<string, string>? HandlerParameters { get; init; }

	/// <summary>
	/// Gets the optional explicit upper bound on how far this adjustment can shift the calculated date in either direction,
	/// expressed in days. When set, the chronological range-resolution pipeline uses this value to size the per-rule and global
	/// fringe envelope so that adjustments whose actual reach exceeds the action's default heuristic are admitted by the fringe
	/// pass.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This property is consumed only by the prototype range-resolution pipeline (<c>NotableDateService.ResolveNotableDatesInRange</c>).
	/// Authors should set it when:
	/// </para>
	/// <list type="bullet">
	///   <item><description><see cref="Action" /> is <see cref="AdjustmentAction.Custom" /> and the custom handler shifts more than ±31 days.</description></item>
	///   <item><description><see cref="Action" /> is <see cref="AdjustmentAction.ReplaceWithNamedDate" /> and the named replacement is more than ±31 days from the original date.</description></item>
	///   <item><description>The author wants to declare a tighter envelope than the default heuristic for performance reasons.</description></item>
	/// </list>
	/// <para>
	/// The value is interpreted as a symmetric absolute envelope: the adjustment may shift the date by at most <c>±value</c> days.
	/// When <see langword="null" />, the pipeline falls back to action-specific defaults (for example, <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> ≈ +7 days).
	/// </para>
	/// </remarks>
	public int? MaxAdjustmentReachDays { get; init; }
}
