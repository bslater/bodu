// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleOverrideBody.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies the override payload carried by the optional <c>&lt;Rule&gt;</c> child of a <c>&lt;Use&gt;</c> cherry-pick directive, used
/// to extend or replace fields on the inherited <see cref="NotableDateRule" /> without redeclaring the rule in full.
/// </summary>
/// <remarks>
/// <para>
/// Every property is nullable or a default-empty immutable collection. Any property left at its default is interpreted as
/// "inherit from the source rule"; any property with a value overrides the corresponding field during merge:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Tags" /> are merged additively with the inherited set (set semantics, duplicates coalesced).</description></item>
/// <item><description><see cref="Adjustments" /> are merged by <see cref="ObservanceAdjustment.Key" />: matching keys replace the inherited entry in place, new keys append to the end.</description></item>
/// <item><description><see cref="Strategy" /> plus its attendant fields replace the inherited strategy wholesale when present.</description></item>
/// <item><description>All scalar fields win over the flat attributes declared directly on the enclosing <c>&lt;Use&gt;</c>; this is the innermost-wins rule.</description></item>
/// </list>
/// </remarks>
public sealed record NotableDateRuleOverrideBody
{
	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.Name" />.
	/// </summary>
	public string? Name { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.Category" />.
	/// </summary>
	public NotableDateCategory? Category { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.TerritoryCode" />.
	/// </summary>
	public string? TerritoryCode { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.IsNonWorkingDay" />.
	/// </summary>
	public bool? IsNonWorkingDay { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.FirstYear" />.
	/// </summary>
	public int? FirstYear { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.LastYear" />.
	/// </summary>
	public int? LastYear { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.OccurrenceYears" />.
	/// </summary>
	public int? OccurrenceYears { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.DurationDays" />.
	/// </summary>
	public int? DurationDays { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.Priority" />.
	/// </summary>
	public int? Priority { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.Comment" />.
	/// </summary>
	public string? Comment { get; init; }

	/// <summary>
	/// Gets the override for <see cref="NotableDateRule.CalendarType" />.
	/// </summary>
	public Type? CalendarType { get; init; }

	/// <summary>
	/// Gets the replacement <see cref="DateResolutionStrategy" />, or <see langword="null" /> to inherit the source strategy.
	/// </summary>
	public DateResolutionStrategy? Strategy { get; init; }

	/// <summary>
	/// Gets the month component used by <see cref="DateResolutionStrategy.Fixed" /> and <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
	/// </summary>
	public int? Month { get; init; }

	/// <summary>
	/// Gets the day-of-month used by <see cref="DateResolutionStrategy.Fixed" />.
	/// </summary>
	public int? Day { get; init; }

	/// <summary>
	/// Gets the day-of-week used by <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
	/// </summary>
	public DayOfWeek? DayOfWeek { get; init; }

	/// <summary>
	/// Gets the week ordinal used by <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
	/// </summary>
	public WeekOfMonthOrdinal? WeekOrdinal { get; init; }

	/// <summary>
	/// Gets the anchor rule name used by <see cref="DateResolutionStrategy.OffsetFromAnchor" />.
	/// </summary>
	public string? AnchorRuleName { get; init; }

	/// <summary>
	/// Gets the day offset used by <see cref="DateResolutionStrategy.OffsetFromAnchor" />.
	/// </summary>
	public int? OffsetDays { get; init; }

	/// <summary>
	/// Gets the calculator registry key used by <see cref="DateResolutionStrategy.Calculator" />.
	/// </summary>
	public string? CalculatorKey { get; init; }

	/// <summary>
	/// Gets the calculator CLR type used by <see cref="DateResolutionStrategy.Calculator" />.
	/// </summary>
	public Type? CalculatorType { get; init; }

	/// <summary>
	/// Gets the tags authored on the override. These merge additively with the inherited rule's tags.
	/// </summary>
	public ImmutableArray<string> Tags { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// Gets the adjustments authored on the override. These merge with inherited adjustments by <see cref="ObservanceAdjustment.Key" />.
	/// </summary>
	public ImmutableArray<ObservanceAdjustment> Adjustments { get; init; } = ImmutableArray<ObservanceAdjustment>.Empty;
}
