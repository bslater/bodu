// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleRemoval.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
/// All filtering parameters are optional. Leaving every filter unset suppresses the rule unconditionally; any
/// combination of year bounds and territory narrows the suppression to a specific window.
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
public sealed record RuleRemoval(
    string RuleName,
    int? FromYear = null,
    int? ToYear = null,
    string? TerritoryCode = null);
