// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleUseDirective.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;


/// <summary>
/// Specifies a single cherry-pick of a <see cref="NotableDateRule" /> from another resource, optionally renaming and overriding
/// scalar properties on the inherited rule.
/// </summary>
/// <param name="SourceRuleName">The name of the rule to pull from the source resource. Matched case-insensitively.</param>
/// <param name="LocalName">Optional local name. When supplied, the imported rule is exposed under this name instead of <paramref name="SourceRuleName" />.</param>
/// <param name="Category">Optional category override.</param>
/// <param name="TerritoryCode">Optional territory override.</param>
/// <param name="IsNonWorkingDay">Optional non-working day override.</param>
/// <param name="FirstYear">Optional inclusive first year override.</param>
/// <param name="LastYear">Optional inclusive last year override.</param>
/// <param name="OccurrenceYears">Optional recurrence cadence override.</param>
/// <param name="DurationDays">Optional duration-in-days override.</param>
/// <param name="Priority">Optional collision priority override.</param>
/// <param name="Comment">Optional comment override.</param>
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
	string? Comment = null);
