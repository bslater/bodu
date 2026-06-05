// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleRemoval.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies that a notable date rule loaded from a base <see cref="INotableDateRuleProvider" /> should be suppressed
/// by an <see cref="INotableDateRuleOverrideProvider" />, optionally scoped to a year range and territory.
/// </summary>
/// <remarks>
/// <para>
/// Rule removals operate at the resolution stage rather than at load time: the base rule remains in the merged rule
/// set, but the service skips it when generating <see cref="NotableDate" /> instances for any year and territory
/// matched by the removal. This makes removals composable with additions from the same override provider — a single
/// provider can replace a rule by emitting a removal alongside a new <see cref="NotableDateRule" /> bearing the same
/// name.
/// </para>
/// <para>
/// All filtering parameters are optional. <paramref name="RuleName" /> matches the canonical
/// <see cref="NotableDateRule.Name" />; leaving every other filter unset suppresses every variant of that name
/// unconditionally, while any combination of <paramref name="RuleVariant" />, <paramref name="CalendarType" />, year
/// bounds, and territory narrows the suppression.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Suppress a holiday for a single year, and permanently remove a regional rule for one subdivision:
/// </para>
/// <code>
///<![CDATA[
/// Suppress Boxing Day for the 2026 calendar year only:
/// RuleRemoval boxingDay2026 = new RuleRemoval(
///     RuleName: "Boxing Day",
///     FromYear: 2026,
///     ToYear: 2026);
///
/// Permanently remove Picnic Day for Northern Territory only (other AU subdivisions unaffected):
/// RuleRemoval picnicDayNT = new RuleRemoval(
///     RuleName: "Picnic Day",
///     TerritoryCode: "AU-NT");
///
/// Returned from an INotableDateRuleOverrideProvider:
/// public IEnumerable<RuleRemoval> GetRemovals()
/// {
///     yield return boxingDay2026;
///     yield return picnicDayNT;
/// }
///]]>
/// </code>
/// </example>
/// <param name="RuleName">
/// The name of the rule to suppress. Matched case-insensitively against <see cref="NotableDateRule.Name" />.
/// </param>
/// <param name="FromYear">
/// Optional inclusive first year of the suppression. <see langword="null" /> for no lower bound.
/// </param>
/// <param name="ToYear">
/// Optional inclusive last year of the suppression. <see langword="null" /> for no upper bound.
/// </param>
/// <param name="TerritoryCode">
/// Optional territory scope. When supplied, suppression applies only when the active territory falls within the
/// supplied scope (a country-level scope covers all of its subdivisions).
/// </param>
/// <param name="RuleVariant">
/// Optional rule-level variant filter, matched case-insensitively against <see cref="NotableDateRule.RuleName" />. When
/// supplied, only the matching variant is suppressed; when <see langword="null" />, every variant sharing the canonical
/// <see cref="NotableDateRule.Name" /> is suppressed.
/// </param>
/// <param name="CalendarType">
/// Optional calendar-type filter, matched against <see cref="NotableDateRule.CalendarType" />. When
/// <see langword="null" />, the removal applies regardless of calendar.
/// </param>
public sealed record RuleRemoval(
    string RuleName,
    int? FromYear = null,
    int? ToYear = null,
    string? TerritoryCode = null,
    string? RuleVariant = null,
    Type? CalendarType = null);
