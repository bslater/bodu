// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleUseDirective.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;


/// <summary>
/// Specifies a single cherry-pick of a <see cref="NotableDateRule" /> from another resource, optionally renaming the rule,
/// overriding scalar properties on the inherited rule, and carrying a richer <see cref="OverrideBody" /> that can replace the
/// strategy, add tags, or merge adjustments by key.
/// </summary>
/// <param name="SourceRuleName">The name of the rule to pull from the source resource. Matched case-insensitively.</param>
/// <param name="LocalName">Optional local name. When supplied, the imported rule is exposed under this name instead of <paramref name="SourceRuleName" />.</param>
/// <param name="Category">Optional category override applied at the <c>&lt;Use&gt;</c> flat-attribute level.</param>
/// <param name="TerritoryCode">Optional territory override applied at the <c>&lt;Use&gt;</c> flat-attribute level.</param>
/// <param name="IsNonWorkingDay">Optional non-working day override applied at the <c>&lt;Use&gt;</c> flat-attribute level.</param>
/// <param name="FirstYear">Optional inclusive first year override.</param>
/// <param name="LastYear">Optional inclusive last year override.</param>
/// <param name="OccurrenceYears">Optional recurrence cadence override.</param>
/// <param name="DurationDays">Optional duration-in-days override.</param>
/// <param name="Priority">Optional collision priority override.</param>
/// <param name="Comment">Optional comment override.</param>
/// <param name="ClearTags">When <see langword="true" />, inherited tags are discarded before the override's tags are applied.</param>
/// <param name="ClearAdjustments">When <see langword="true" />, inherited adjustments are discarded before the override's adjustments are applied.</param>
/// <param name="ClearInherited">
/// When <see langword="true" />, every inherited rule sharing the directive's canonical name is dropped before the override is
/// applied; the override body alone defines the resulting rule(s). When <see langword="false" /> (default), inherited rules
/// pass through and the override body — if present — replaces only the inherited rule whose
/// <see cref="NotableDateRule.RuleName" /> matches the body's <see cref="NotableDateRuleOverrideBody.RuleName" />, applying to
/// every match when the body's identifier is omitted.
/// </param>
/// <param name="OverrideBody">Optional override body supplied via a nested <c>&lt;Rule&gt;</c> child. Fields on the body win over the flat attributes where both are present.</param>
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
