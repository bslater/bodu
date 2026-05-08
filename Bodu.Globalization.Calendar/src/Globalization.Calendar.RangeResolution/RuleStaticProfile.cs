// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleStaticProfile.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Captures the static, year-independent characteristics of a <see cref="NotableDateRule" /> used by the chronological
/// range-resolution pipeline to scope per-request processing.
/// </summary>
/// <remarks>
/// <para>
/// A profile records the rule's processing tier, transitive root anchor (when offset-derived), the base offset relative to that
/// anchor, and the minimum and maximum day deltas that any observance adjustment can produce. Profiles are built once at service
/// construction by <see cref="RuleStaticAnalysis" /> and re-used across every range-resolution request.
/// </para>
/// <para>
/// <see cref="MinObservedReach" /> and <see cref="MaxObservedReach" /> express the day-delta envelope of the rule's observable date
/// space relative to the rule's <em>own anchor date</em>. They include every adjustment's worst-case shift and the duration of the
/// observable span. The pipeline uses this envelope to decide which civil years and which rules can possibly contribute to a
/// requested chronological window.
/// </para>
/// </remarks>
internal sealed record RuleStaticProfile(
	NotableDateRule Rule,
	RuleTier Tier,
	string? RootAnchorRuleName,
	int OffsetFromRoot,
	int MinObservedReach,
	int MaxObservedReach)
{
	/// <summary>
	/// Gets a value indicating whether this rule depends transitively on an algorithmic anchor.
	/// </summary>
	public bool DependsOnAlgorithmicAnchor =>
		Tier == RuleTier.Algorithmic || Tier == RuleTier.OffsetFromAlgorithmic;

	/// <summary>
	/// Gets the inclusive upper bound on how far an observed date for this rule can move in the positive direction relative to its
	/// own anchor.
	/// </summary>
	public int MaxForwardReach => MaxObservedReach;

	/// <summary>
	/// Gets the inclusive lower bound on how far an observed date for this rule can move in the negative direction relative to its
	/// own anchor.
	/// </summary>
	public int MinBackwardReach => MinObservedReach;
}
