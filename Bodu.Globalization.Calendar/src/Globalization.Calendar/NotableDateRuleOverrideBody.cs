// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleOverrideBody.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies the override payload carried by the optional <c>&lt;Rule&gt;</c> child of a <c>&lt;Use&gt;</c> cherry-pick
/// directive, used to extend or replace fields on the inherited <see cref="NotableDateRule" /> without redeclaring the
/// rule in full.
/// </summary>
/// <remarks>
/// <para>
/// Every property is nullable or a default-empty immutable collection. Any property left at its default is interpreted
/// as "inherit from the source rule"; any property with a value overrides the corresponding field during merge:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Tags" /> are merged additively with the inherited set (set semantics, duplicates coalesced).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Adjustments" /> are merged by <see cref="ObservanceAdjustment.Key" />: matching keys replace the
/// inherited entry in place, new keys append to the end.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Strategy" /> plus its attendant fields replace the inherited strategy wholesale when present.
/// </description>
/// </item>
/// <item>
/// <description>
/// All scalar fields win over the flat attributes declared directly on the enclosing <c>&lt;Use&gt;</c>; this is the
/// innermost-wins rule.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed record NotableDateRuleOverrideBody
{
    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.Name" />.
    /// </summary>
    /// <remarks>
    /// Reserved for programmatic construction; the XML parser does not populate this from the inner <c>&lt;Rule&gt;</c>
    /// 's <c>name</c> attribute (use the directive's <c>as</c> attribute to rename inherited rules).
    /// </remarks>
    /// <returns>
    /// The replacement notable date title, or <see langword="null" /> to inherit <see cref="NotableDateRule.Name" />.
    /// </returns>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the rule-level identifier used to target a specific inherited rule for override when the source notable
    /// date contains more than one <see cref="NotableDateRule" />.
    /// </summary>
    /// <remarks>
    /// When set, the merger applies this override only to the inherited rule whose
    /// <see cref="NotableDateRule.RuleName" /> matches (case-insensitive). When <see langword="null" /> the override
    /// applies to every inherited rule sharing the directive's canonical name. Populated by the parser from the inner
    /// <c>&lt;Rule&gt;</c>'s <c>name</c> attribute.
    /// </remarks>
    /// <returns>
    /// The rule-level identifier, or <see langword="null" /> to broadcast the override to every matching rule.
    /// </returns>
    public string? RuleName { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.Category" />.
    /// </summary>
    /// <returns>
    /// The replacement <see cref="NotableDateCategory" />, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public NotableDateCategory? Category { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.TerritoryCode" />.
    /// </summary>
    /// <returns>
    /// The replacement comma-separated territory list, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public string? TerritoryCode { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.IsNonWorkingDay" />.
    /// </summary>
    /// <returns>A nullable boolean replacement, or <see langword="null" /> to inherit the source value.</returns>
    public bool? IsNonWorkingDay { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.FirstYear" />.
    /// </summary>
    /// <returns>
    /// The replacement civil year lower bound, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public int? FirstYear { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.LastYear" />.
    /// </summary>
    /// <returns>
    /// The replacement civil year upper bound, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public int? LastYear { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.OccurrenceYears" />.
    /// </summary>
    /// <returns>
    /// The replacement recurrence cadence in years, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public int? OccurrenceYears { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.DurationDays" />.
    /// </summary>
    /// <returns>The replacement positive day count, or <see langword="null" /> to inherit the source value.</returns>
    public int? DurationDays { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.Priority" />.
    /// </summary>
    /// <returns>
    /// The replacement integer priority (lower wins), or <see langword="null" /> to inherit the source value.
    /// </returns>
    public int? Priority { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.Comment" />.
    /// </summary>
    /// <returns>The replacement comment text, or <see langword="null" /> to inherit the source value.</returns>
    public string? Comment { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.CalendarType" />.
    /// </summary>
    /// <returns>
    /// The replacement calendar <see cref="Type" />, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public Type? CalendarType { get; init; }

    /// <summary>
    /// Gets the replacement <see cref="DateResolutionStrategy" />, or <see langword="null" /> to inherit the source
    /// strategy.
    /// </summary>
    /// <returns>
    /// One of the defined <see cref="DateResolutionStrategy" /> values, or <see langword="null" /> to inherit.
    /// </returns>
    public DateResolutionStrategy? Strategy { get; init; }

    /// <summary>
    /// Gets the month component used by <see cref="DateResolutionStrategy.Fixed" /> and
    /// <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
    /// </summary>
    /// <returns>
    /// A month number in the range <c>1</c>..<c>12</c>, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public int? Month { get; init; }

    /// <summary>
    /// Gets the day-of-month used by <see cref="DateResolutionStrategy.Fixed" />.
    /// </summary>
    /// <returns>
    /// A day-of-month in the range <c>1</c>..<c>31</c>, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public int? Day { get; init; }

    /// <summary>
    /// Gets a value indicating whether gets the override for <see cref="NotableDateRule.SkipLeapMonth" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> to advance past intercalary months when ordinal mapping; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool SkipLeapMonth { get; init; }

    /// <summary>
    /// Gets a value indicating whether gets the override for <see cref="NotableDateRule.SweepCalendarYears" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> to evaluate both candidate calendar years; otherwise <see langword="false" />.
    /// </returns>
    public bool SweepCalendarYears { get; init; }

    /// <summary>
    /// Gets the override for <see cref="NotableDateRule.CalendarMonthAlias" />.
    /// </summary>
    /// <returns>The replacement month alias token, or <see langword="null" /> to inherit the source value.</returns>
    public string? CalendarMonthAlias { get; init; }

    /// <summary>
    /// Gets the day-of-week used by <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
    /// </summary>
    /// <returns>
    /// A <see cref="System.DayOfWeek" /> value, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public DayOfWeek? DayOfWeek { get; init; }

    /// <summary>
    /// Gets the week ordinal used by <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
    /// </summary>
    /// <returns>
    /// One of the defined <see cref="WeekOfMonthOrdinal" /> values, or <see langword="null" /> to inherit the source
    /// value.
    /// </returns>
    public WeekOfMonthOrdinal? WeekOrdinal { get; init; }

    /// <summary>
    /// Gets the direction used by <see cref="DateResolutionStrategy.WeekdayNearDate" /> to position the target
    /// <see cref="DayOfWeek" /> relative to the reference <see cref="Month" /> and <see cref="Day" />.
    /// </summary>
    /// <returns>
    /// One of the defined <see cref="Bodu.Globalization.Calendar.WeekdayProximity" /> values, or <see langword="null" />
    /// to inherit the source value.
    /// </returns>
    public WeekdayProximity? WeekdayProximity { get; init; }

    /// <summary>
    /// Gets the anchor rule name used by <see cref="DateResolutionStrategy.OffsetFromAnchor" />.
    /// </summary>
    /// <returns>
    /// The anchor rule's <see cref="NotableDateRule.Name" />, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public string? AnchorRuleName { get; init; }

    /// <summary>
    /// Gets the day offset used by <see cref="DateResolutionStrategy.OffsetFromAnchor" />.
    /// </summary>
    /// <returns>A signed day offset, or <see langword="null" /> to inherit the source value.</returns>
    public int? OffsetDays { get; init; }

    /// <summary>
    /// Gets the algorithm registry key used by <see cref="DateResolutionStrategy.Algorithm" />.
    /// </summary>
    /// <returns>
    /// The replacement algorithm registry key, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public string? AlgorithmKey { get; init; }

    /// <summary>
    /// Gets the algorithm CLR type used by <see cref="DateResolutionStrategy.Algorithm" />.
    /// </summary>
    /// <returns>
    /// The replacement algorithm <see cref="Type" />, or <see langword="null" /> to inherit the source value.
    /// </returns>
    public Type? AlgorithmType { get; init; }

    /// <summary>
    /// Gets the month token passed to the algorithm constructor for <see cref="DateResolutionStrategy.Algorithm" />
    /// rules whose <see cref="AlgorithmType" /> takes a <c>(month, day)</c> pair.
    /// </summary>
    /// <returns>
    /// The replacement month token forwarded to the algorithm constructor, or <see langword="null" /> to inherit the
    /// source value.
    /// </returns>
    public string? AlgorithmMonth { get; init; }

    /// <summary>
    /// Gets the day-of-month value passed to the algorithm constructor alongside <see cref="AlgorithmMonth" />.
    /// </summary>
    /// <returns>
    /// The replacement day-of-month forwarded to the algorithm constructor, or <see langword="null" /> to inherit the
    /// source value.
    /// </returns>
    public int? AlgorithmDay { get; init; }

    /// <summary>
    /// Gets the tags authored on the override. These merge additively with the inherited rule's tags.
    /// </summary>
    /// <returns>
    /// An immutable array of additive tag values. Empty when no override tags are authored; never
    /// <see langword="default" />.
    /// </returns>
    public ImmutableArray<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets the adjustments authored on the override. These merge with inherited adjustments by
    /// <see cref="ObservanceAdjustment.Key" />.
    /// </summary>
    /// <returns>
    /// An immutable array of <see cref="ObservanceAdjustment" /> entries merged into the inherited list. Empty when no
    /// override adjustments are authored.
    /// </returns>
    public ImmutableArray<ObservanceAdjustment> Adjustments { get; init; } = [];
}
