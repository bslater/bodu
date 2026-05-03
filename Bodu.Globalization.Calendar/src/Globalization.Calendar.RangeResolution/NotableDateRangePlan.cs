// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePlan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Captures the per-request resolution plan computed by <see cref="NotableDateRangePlanner" /> from a
/// <see cref="NotableDateRangeRequest" /> and a <see cref="RuleStaticAnalysis" />.
/// </summary>
/// <remarks>
/// <para>
/// The plan holds the effective resolution range (the request window expanded by the global static reach), the rules eligible to
/// contribute to the request, and the precise civil years per algorithmic anchor that must be computed. The pipeline iterates the
/// plan exactly — no rule is processed and no algorithm is invoked outside of what the plan authorises.
/// </para>
/// </remarks>
internal sealed class NotableDateRangePlan
{
	private readonly Dictionary<string, IReadOnlyList<int>> _anchorYearsByName;

	/// <summary>
	/// Initialises a new instance of the <see cref="NotableDateRangePlan" /> class.
	/// </summary>
	/// <param name="request">The originating request.</param>
	/// <param name="effectiveRangeStart">The inclusive start of the effective range.</param>
	/// <param name="effectiveRangeEnd">The inclusive end of the effective range.</param>
	/// <param name="eligibleRules">The rule profiles that may contribute to the request.</param>
	/// <param name="candidateYears">The civil years considered by the planner for direct rule materialisation.</param>
	/// <param name="anchorYearsByName">The civil years per algorithmic anchor that must be computed.</param>
	public NotableDateRangePlan(
		NotableDateRangeRequest request,
		DateTime effectiveRangeStart,
		DateTime effectiveRangeEnd,
		IReadOnlyList<RuleStaticProfile> eligibleRules,
		IReadOnlyList<int> candidateYears,
		Dictionary<string, IReadOnlyList<int>> anchorYearsByName)
	{
		Request = request;
		EffectiveRangeStart = effectiveRangeStart;
		EffectiveRangeEnd = effectiveRangeEnd;
		EligibleRules = eligibleRules;
		CandidateYears = candidateYears;
		_anchorYearsByName = anchorYearsByName;
	}

	/// <summary>
	/// Gets the originating request.
	/// </summary>
	public NotableDateRangeRequest Request { get; }

	/// <summary>
	/// Gets the inclusive start of the effective range — the request start expanded by the global static reach so that all rules
	/// whose adjustments may roll into the request window are visible.
	/// </summary>
	public DateTime EffectiveRangeStart { get; }

	/// <summary>
	/// Gets the inclusive end of the effective range — the request end expanded by the global static reach.
	/// </summary>
	public DateTime EffectiveRangeEnd { get; }

	/// <summary>
	/// Gets the rule profiles that may contribute to the request, in input order.
	/// </summary>
	public IReadOnlyList<RuleStaticProfile> EligibleRules { get; }

	/// <summary>
	/// Gets the civil years considered for direct rule materialisation (one per year between the effective range start and end).
	/// </summary>
	public IReadOnlyList<int> CandidateYears { get; }

	/// <summary>
	/// Gets the civil years that must be resolved for each algorithmic anchor name.
	/// </summary>
	/// <param name="anchorRuleName">The algorithmic anchor rule name.</param>
	/// <returns>The required civil years; an empty list when the anchor has no dependents that touch the request window.</returns>
	public IReadOnlyList<int> GetAnchorYears(string anchorRuleName) =>
		_anchorYearsByName.TryGetValue(anchorRuleName, out IReadOnlyList<int>? list)
			? list
			: Array.Empty<int>();

	/// <summary>
	/// Enumerates the algorithmic anchor names whose computation is demanded by at least one eligible rule.
	/// </summary>
	/// <returns>The required anchor names.</returns>
	public IEnumerable<string> RequiredAnchorNames() => _anchorYearsByName.Keys;
}
