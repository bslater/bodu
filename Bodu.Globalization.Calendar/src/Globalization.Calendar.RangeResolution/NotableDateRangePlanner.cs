// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePlanner.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Builds a per-request <see cref="NotableDateRangePlan" /> from a <see cref="NotableDateRangeRequest" /> and a
/// <see cref="RuleStaticAnalysis" />.
/// </summary>
/// <remarks>
/// <para>
/// The planner is the deterministic, side-effect-free step that decides which rules and which civil years are eligible to be
/// materialised for the request. It expands the request window by the rule set's static reach to admit collateral dates that may
/// roll into or out of the request through observance adjustments.
/// </para>
/// </remarks>
internal sealed class NotableDateRangePlanner
{
	private readonly RuleStaticAnalysis _analysis;

	/// <summary>
	/// Initialises a new instance of the <see cref="NotableDateRangePlanner" /> class.
	/// </summary>
	/// <param name="analysis">The static rule analysis to use when planning requests.</param>
	/// <exception cref="ArgumentNullException"><paramref name="analysis" /> is <see langword="null" />.</exception>
	public NotableDateRangePlanner(RuleStaticAnalysis analysis)
	{
		_analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
	}

	/// <summary>
	/// Builds a <see cref="NotableDateRangePlan" /> for the supplied request.
	/// </summary>
	/// <param name="request">The originating range request.</param>
	/// <returns>The constructed plan.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
	public NotableDateRangePlan Plan(NotableDateRangeRequest request)
	{
		if (request is null) throw new ArgumentNullException(nameof(request));

		// To find rule anchors that may land inside the request window after observance adjustment / duration:
		//   An anchor with a forward-most shift of GlobalMaxReach can land as late as anchor + GlobalMaxReach. For it to reach
		//   request.Start, the anchor must be on or after request.Start - GlobalMaxReach.
		//   An anchor with a backward-most shift of GlobalMinReach (negative) can land as early as anchor + GlobalMinReach. For it
		//   to be on or before request.End, the anchor must be on or before request.End - GlobalMinReach.
		DateTime effectiveStart = AddDaysClamped(request.StartDate, -_analysis.GlobalMaxReach);
		DateTime effectiveEnd = AddDaysClamped(request.EndDate, -_analysis.GlobalMinReach);

		List<RuleStaticProfile> eligible = new();
		foreach (RuleStaticProfile profile in _analysis.Profiles)
		{
			if (IsRuleEligible(profile, request))
				eligible.Add(profile);
		}

		List<int> candidateYears = new();
		for (int year = effectiveStart.Year; year <= effectiveEnd.Year; year++)
			candidateYears.Add(year);

		Dictionary<string, IReadOnlyList<int>> anchorYearsByName = new(StringComparer.OrdinalIgnoreCase);

		foreach (RuleStaticProfile profile in eligible)
		{
			if (!profile.DependsOnAlgorithmicAnchor) continue;
			if (string.IsNullOrWhiteSpace(profile.RootAnchorRuleName)) continue;

			string anchorName = profile.RootAnchorRuleName!;
			if (!anchorYearsByName.ContainsKey(anchorName))
				anchorYearsByName[anchorName] = ComputeAnchorYears(request, profile);
		}

		return new NotableDateRangePlan(
			request,
			effectiveStart,
			effectiveEnd,
			eligible,
			candidateYears,
			anchorYearsByName);
	}

	/// <summary>
	/// Computes the civil years for which an algorithmic anchor must be resolved so that every eligible dependent has access to it.
	/// </summary>
	/// <param name="request">The originating request.</param>
	/// <param name="profile">A profile that depends on the algorithmic anchor.</param>
	/// <returns>The civil years that must be resolved.</returns>
	/// <remarks>
	/// <para>
	/// The conservative default is <c>[request.Year − 1, request.Year, request.Year + 1, …, request.EndYear + 1]</c>. This covers
	/// the practical envelope of every algorithmic anchor in use today (Easter, Lunar New Year, Vesak, Asalha Puja, etc.) without
	/// requiring per-algorithm bounds metadata. Year-by-year over-computation is bounded because <see cref="NotableDateRangeResolutionCache.ResolveAnchor" />
	/// caches the result.
	/// </para>
	/// </remarks>
	private static IReadOnlyList<int> ComputeAnchorYears(NotableDateRangeRequest request, RuleStaticProfile profile)
	{
		_ = profile;
		List<int> years = new();
		int startYear = request.StartDate.Year - 1;
		int endYear = request.EndDate.Year + 1;

		for (int year = startYear; year <= endYear; year++)
			years.Add(year);

		return years;
	}

	/// <summary>
	/// Determines whether the rule may contribute to the request based on territory, calendar, year bounds, and filter eligibility.
	/// </summary>
	/// <param name="profile">The profile under test.</param>
	/// <param name="request">The originating request.</param>
	/// <returns><see langword="true" /> when the rule is eligible; otherwise, <see langword="false" />.</returns>
	private static bool IsRuleEligible(RuleStaticProfile profile, NotableDateRangeRequest request)
	{
		NotableDateRule rule = profile.Rule;

		if (request.Filter is not null && !request.Filter.IsRuleEligible(rule))
			return false;

		if (request.CalendarType is not null && rule.CalendarType is not null && rule.CalendarType != request.CalendarType)
			return false;

		if (!RuleMayApplyToTerritory(rule, request.TerritoryCode))
			return false;

		// Year applicability is left to the resolver per (rule, year) since it depends on FirstYear/LastYear/OccurrenceYears.
		return true;
	}

	/// <summary>
	/// Returns <see langword="true" /> when the rule's authored territory list intersects the requested territory using parent /
	/// child containment semantics.
	/// </summary>
	/// <param name="rule">The rule to check.</param>
	/// <param name="requestedTerritory">The requested territory, or <see langword="null" /> for any.</param>
	/// <returns><see langword="true" /> when the rule may apply.</returns>
	private static bool RuleMayApplyToTerritory(NotableDateRule rule, string? requestedTerritory)
	{
		if (string.IsNullOrEmpty(rule.TerritoryCode)) return true;
		if (string.IsNullOrEmpty(requestedTerritory)) return true;

		if (!TerritoryCode.TryParse(requestedTerritory, out TerritoryCode requested))
			return false;

		foreach (TerritoryCode owned in TerritoryCode.ParseList(rule.TerritoryCode))
		{
			if (owned.Contains(requested) || requested.Contains(owned))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Adds days to a date, clamping to the <see cref="DateTime" /> bounds.
	/// </summary>
	/// <param name="date">The source date.</param>
	/// <param name="days">The number of days to add (may be negative).</param>
	/// <returns>The resulting date, clamped to the supported range.</returns>
	private static DateTime AddDaysClamped(DateTime date, int days)
	{
		DateTime value = date.Date;

		if (days < 0 && value <= DateTime.MinValue.AddDays(-days))
			return DateTime.MinValue.Date;
		if (days > 0 && value >= DateTime.MaxValue.AddDays(-days))
			return DateTime.MaxValue.Date;

		return value.AddDays(days);
	}
}
