// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleUseDirective.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies a single cherry-pick of a <see cref="NotableDateRule" /> from another resource, optionally renaming the
/// rule, overriding scalar properties on the inherited rule, and carrying a richer <see cref="OverrideBody" /> that can
/// replace the strategy, add tags, or merge adjustments by key.
/// </summary>
/// <remarks>
/// <para>
/// A directive is the per-rule entry inside a <see cref="NotableDateRuleUseGroup" />. It corresponds to a single
/// <c>&lt;Rule&gt;</c> child of a <c>&lt;Use&gt;</c> element in XML (or its JSON equivalent), and tells the loader:
/// "import the rule named <see cref="SourceRuleName" /> from the group's source resource and apply these overrides on
/// top of it." The directive does not declare a new rule from scratch; for that, author a top-level <c>&lt;Rule&gt;</c>
/// in the document.
/// </para>
/// <para>
/// Override precedence is <em>innermost wins</em>. The flat scalar attributes on the directive — for example
/// <see cref="TerritoryCode" />, <see cref="IsNonWorkingDay" />, and <see cref="Priority" /> — are applied first, then
/// the nested <see cref="OverrideBody" /> wins over them when both are present. Each override is treated as either
/// "replace" or "merge" by kind:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Scalar fields (<see cref="Category" />, <see cref="TerritoryCode" />, <see cref="IsNonWorkingDay" />, year bounds,
/// …) — replace the inherited value when set.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NotableDateRuleOverrideBody.Tags" /> — merge additively with inherited tags unless
/// <see cref="ClearTags" /> is <see langword="true" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NotableDateRuleOverrideBody.Adjustments" /> — merge by <see cref="ObservanceAdjustment.Key" /> (matching
/// keys replace in place; new keys append) unless <see cref="ClearAdjustments" /> is <see langword="true" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// Strategy fields (<see cref="NotableDateRuleOverrideBody.Strategy" /> + its attendants) — replace the inherited
/// strategy wholesale when present.
/// </description>
/// </item>
/// </list>
/// <para>
/// The merged rule is what eventually drives a <see cref="NotableDate" /> at resolution time, so directives are the
/// right place to adapt an inherited rule to a host's territory, weekend roll behavior, or non-working flag without
/// redeclaring the rule in full.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Inherit Anzac Day from a global definition, retarget it to Western Australia, and override the weekend-roll
/// adjustment by key so the substitute lands on Tuesday instead of Monday:
/// </para>
/// <code>
///<![CDATA[
/// NotableDateRuleUseDirective directive = new NotableDateRuleUseDirective(
///     SourceRuleName: "Anzac Day",
///     TerritoryCode: "AU-WA",
///     IsNonWorkingDay: true,
///     OverrideBody: new NotableDateRuleOverrideBody
///     {
///         Adjustments = ImmutableArray.Create(new ObservanceAdjustment
///         {
///             Key = "weekend-roll",                        // matches the inherited entry by key
///             Trigger = AdjustmentTrigger.IfWeekend,
///             Action  = AdjustmentAction.AddDays,
///             OffsetDays = 2,                              // shift to Tuesday rather than Monday
///             IsNonWorkingDay = true,
///         }),
///     });
///
/// // Equivalent XML:
/// // <Use source="Bodu/Globalization/Calendar/Resources/au-public.xml">
/// //   <Rule sourceName="Anzac Day" territoryCode="AU-WA" isNonWorkingDay="true">
/// //     <Adjustment key="weekend-roll" trigger="IfWeekend" action="AddDays" offsetDays="2" isNonWorkingDay="true" />
/// //   </Rule>
/// // </Use>
///]]>
/// </code>
/// </example>
/// <param name="SourceRuleName">
/// The name of the rule to pull from the source resource. Matched case-insensitively.
/// </param>
/// <param name="LocalName">
/// Optional local name. When supplied, the imported rule is exposed under this name instead of
/// <paramref name="SourceRuleName" />.
/// </param>
/// <param name="Category">Optional category override applied at the <c>&lt;Use&gt;</c> flat-attribute level.</param>
/// <param name="TerritoryCode">
/// Optional territory override applied at the <c>&lt;Use&gt;</c> flat-attribute level.
/// </param>
/// <param name="IsNonWorkingDay">
/// Optional non-working day override applied at the <c>&lt;Use&gt;</c> flat-attribute level.
/// </param>
/// <param name="FirstYear">Optional inclusive first year override.</param>
/// <param name="LastYear">Optional inclusive last year override.</param>
/// <param name="OccurrenceYears">Optional recurrence cadence override.</param>
/// <param name="DurationDays">Optional duration-in-days override.</param>
/// <param name="Priority">Optional collision priority override.</param>
/// <param name="Comment">Optional comment override.</param>
/// <param name="ClearTags">
/// When <see langword="true" />, inherited tags are discarded before the override's tags are applied.
/// </param>
/// <param name="ClearAdjustments">
/// When <see langword="true" />, inherited adjustments are discarded before the override's adjustments are applied.
/// </param>
/// <param name="ClearInherited">
/// When <see langword="true" />, every inherited rule sharing the directive's canonical name is dropped before the
/// override is applied; the override body alone defines the resulting rule(s). When <see langword="false" /> (default),
/// inherited rules pass through and the override body — if present — replaces only the inherited rule whose
/// <see cref="NotableDateRule.RuleName" /> matches the body's <see cref="NotableDateRuleOverrideBody.RuleName" />,
/// applying to every match when the body's identifier is omitted.
/// </param>
/// <param name="OverrideBody">
/// Optional override body supplied via a nested <c>&lt;Rule&gt;</c> child. Fields on the body win over the flat
/// attributes where both are present.
/// </param>
public sealed record NotableDateRuleUseDirective(
    string SourceRuleName,
    string? LocalName = null,
    NotableDateCategory? Category = null,
    string? TerritoryCode = null,
    bool? IsNonWorkingDay = null,
    int? FirstYear = null,
    int? LastYear = null,
    int? OccurrenceYears = null,
    int? DurationDays = null,
    int? Priority = null,
    string? Comment = null,
    bool ClearTags = false,
    bool ClearAdjustments = false,
    bool ClearInherited = false,
    NotableDateRuleOverrideBody? OverrideBody = null);
