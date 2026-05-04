// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipeline.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BoduExt = Bodu.Extensions;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Orchestrates the chronological range-resolution pipeline: planning, tiered occurrence materialisation, observance adjustment,
/// and emission ordering.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline is the prototype replacement for <see cref="NotableDateResolutionEngine" /> and
/// <see cref="NotableDateResolutionAdjustmentProcessor" />. It is intentionally implemented as a single class so the four tiers and
/// the adjustment phase are visible together; production code may decompose this into separate processors.
/// </para>
/// <para>
/// Tiered processing order (each tier reads from the cache populated by earlier tiers):
/// </para>
/// <list type="number">
///   <item><description><see cref="RuleTier.Fixed" /> — direct fixed-date and day-of-week-in-month rules.</description></item>
///   <item><description><see cref="RuleTier.OffsetFromFixed" /> — offset rules whose root anchor is a fixed-date rule.</description></item>
///   <item><description><see cref="RuleTier.Algorithmic" /> — algorithm-backed anchors, computed once per (anchor name, year).</description></item>
///   <item><description><see cref="RuleTier.OffsetFromAlgorithmic" /> — offset rules whose root anchor is algorithmic (for example <c>Start of Lent</c> = <c>Easter Sunday − 46</c>).</description></item>
/// </list>
/// </remarks>
internal sealed class NotableDateRangePipeline
{
	private readonly RuleStaticAnalysis _analysis;
	private readonly NotableDateRuleResolver _ruleResolver;
	private readonly NotableDateRangePlanner _planner;
	private readonly BoduExt.CalendarWeekendDefinition _weekendDefinition;
	private readonly BoduExt.IWeekendDefinitionProvider? _weekendProvider;
	private readonly IAdjustmentHandlerRegistry? _handlerRegistry;

	/// <summary>
	/// Initialises a new instance of the <see cref="NotableDateRangePipeline" /> class.
	/// </summary>
	/// <param name="analysis">The static rule analysis.</param>
	/// <param name="ruleResolver">The resolver used to compute fixed and algorithmic anchor dates.</param>
	/// <param name="weekendDefinition">The weekend definition used during adjustment evaluation.</param>
	/// <param name="weekendProvider">The optional custom weekend provider.</param>
	/// <param name="handlerRegistry">The optional custom adjustment handler registry.</param>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="analysis" /> or <paramref name="ruleResolver" /> is <see langword="null" />.
	/// </exception>
	public NotableDateRangePipeline(
		RuleStaticAnalysis analysis,
		NotableDateRuleResolver ruleResolver,
		BoduExt.CalendarWeekendDefinition weekendDefinition,
		BoduExt.IWeekendDefinitionProvider? weekendProvider = null,
		IAdjustmentHandlerRegistry? handlerRegistry = null)
	{
		_analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
		_ruleResolver = ruleResolver ?? throw new ArgumentNullException(nameof(ruleResolver));
		_planner = new NotableDateRangePlanner(analysis);
		_weekendDefinition = weekendDefinition;
		_weekendProvider = weekendProvider;
		_handlerRegistry = handlerRegistry;
	}

	/// <summary>
	/// Resolves notable dates whose observed date intersects the supplied request window.
	/// </summary>
	/// <param name="request">The range request.</param>
	/// <returns>The resolved notable dates, ordered by observed date.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// Resolution runs in two passes followed by an adjustment phase:
	/// </para>
	/// <list type="number">
	///   <item><description><b>Main pass</b> — every eligible rule is materialised for the civil years that the request range spans. Rules whose resolved date falls inside the request window enter the cache in <see cref="NotableDateCacheState.InWindow" />; those just outside enter as <see cref="NotableDateCacheState.Computed" /> for adjustment context.</description></item>
	///   <item><description><b>Fringe pass</b> — for each adjacent civil year the request window touches inside the planner's fringe distance, every <see cref="RuleTier.Fixed" /> rule with at least one adjustment is materialised when its resolved date falls inside the fringe window. This catches cross-year roll-overs such as <c>31 Dec</c> rolling forward to <c>3 Jan</c> without a global reach expansion.</description></item>
	///   <item><description><b>Adjustment phase</b> — observance adjustments are applied using the cache as the non-working day context. Adjusted dates that intersect the request promote the entry to <see cref="NotableDateCacheState.Adjusted" /> and supersede the base on emission.</description></item>
	/// </list>
	/// </remarks>
	public IReadOnlyList<NotableDate> Resolve(NotableDateRangeRequest request)
	{
		if (request is null) throw new ArgumentNullException(nameof(request));

		NotableDateRangePlan plan = _planner.Plan(request);
		NotableDateRangeResolutionCache cache = new();

		// Pass 1 — main: rules whose resolved date for years the request spans may intersect (or directly feed) the window.

		// Tier 1: Fixed and DayOfWeekInMonth.
		foreach (RuleStaticProfile profile in plan.EligibleRules)
		{
			if (profile.Tier != RuleTier.Fixed) continue;
			ProcessDirect(profile, plan, cache);
		}

		// Tier 2: OffsetFromFixed — anchor already computed in Tier 1.
		foreach (RuleStaticProfile profile in plan.EligibleRules)
		{
			if (profile.Tier != RuleTier.OffsetFromFixed) continue;
			ProcessOffsetFromCached(profile, plan, cache);
		}

		// Tier 3: Algorithmic anchors — compute exactly the demanded years (request years ∪ fringe years).
		ProcessAlgorithmicAnchors(plan, cache);

		// Tier 4: OffsetFromAlgorithmic — anchor available in the cache from Tier 3.
		foreach (RuleStaticProfile profile in plan.EligibleRules)
		{
			if (profile.Tier != RuleTier.OffsetFromAlgorithmic) continue;
			ProcessOffsetFromCached(profile, plan, cache);
		}

		// Pass 2 — fringe: scan adjacent year boundaries for Tier 1 rules with adjustments that may roll into the window.
		ProcessFringePass(plan, cache);

		// Adjustment phase — uses the cache as non-working day context.
		ApplyAdjustments(plan, cache);

		return BuildEmissionList(plan, cache);
	}

	/// <summary>
	/// Materialises Tier 1 rules with at least one observance adjustment for adjacent civil years inside the planner's fringe
	/// distance, admitting only those whose resolved date sits within the fringe window.
	/// </summary>
	/// <param name="plan">The active resolution plan.</param>
	/// <param name="cache">The shared cache being populated.</param>
	/// <remarks>
	/// <para>
	/// This pass handles cross-year roll-overs. For example, a request <c>[3 Jan 2023, 3 Jan 2023]</c> needs to know about the
	/// prior-year <c>31 Dec 2022</c> rule (whose resolved date is in the fringe window <c>[27 Dec 2022, 10 Jan 2023]</c>) so that
	/// the adjustment phase can roll it forward to <c>3 Jan 2023</c>. Rules without adjustments are skipped because they cannot
	/// contribute through this mechanism.
	/// </para>
	/// <para>
	/// Algorithmic and offset-from-algorithmic tiers do not need a separate fringe pass — the planner already unions fringe years
	/// into <see cref="NotableDateRangePlan.GetAnchorYears" />, so Tier 3 / Tier 4 in the main pass cover those cases.
	/// </para>
	/// <para>
	/// <b>Limitation:</b> offset-from-fixed rules with adjustments in fringe years are not handled here because their root anchor
	/// in the fringe year is not materialised by the main pass. Add the rule's fixed anchor to the rule set's main-year processing
	/// or extend this pass if such a configuration becomes necessary.
	/// </para>
	/// </remarks>
	private void ProcessFringePass(NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
	{
		if (plan.FringeYears.Count == 0) return;

		foreach (RuleStaticProfile profile in plan.EligibleRules)
		{
			if (profile.Tier != RuleTier.Fixed) continue;
			if (profile.Rule.Adjustments.IsDefaultOrEmpty) continue;

			foreach (int year in plan.FringeYears)
			{
				if (!NotableDateRuleResolver.IsApplicable(profile.Rule, year))
					continue;

				DateTime? anchor;
				try
				{
					anchor = _ruleResolver.ResolveAnchorDate(profile.Rule, year);
				}
				catch (InvalidOperationException)
				{
					continue;
				}

				if (anchor is null) continue;

				// Only admit fringe candidates close enough to the request edges that an adjustment could shift them inside.
				DateTime resolved = anchor.Value.Date;
				if (resolved < plan.FringeStartDate || resolved > plan.FringeEndDate) continue;

				AddEntries(profile, year, anchor.Value, plan, cache);
			}
		}
	}

	/// <summary>
	/// Materialises Tier 1 (Fixed / DayOfWeekInMonth) rules across every candidate year and territory.
	/// </summary>
	/// <param name="profile">The rule profile to materialise.</param>
	/// <param name="plan">The active resolution plan.</param>
	/// <param name="cache">The shared cache being populated.</param>
	private void ProcessDirect(RuleStaticProfile profile, NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
	{
		foreach (int year in plan.CandidateYears)
		{
			if (!NotableDateRuleResolver.IsApplicable(profile.Rule, year))
				continue;

			DateTime? anchor;
			try
			{
				anchor = _ruleResolver.ResolveAnchorDate(profile.Rule, year);
			}
			catch (InvalidOperationException)
			{
				continue;
			}

			if (anchor is null) continue;

			AddEntries(profile, year, anchor.Value, plan, cache);
		}
	}

	/// <summary>
	/// Materialises Tier 3 algorithmic rules for the years demanded by <see cref="NotableDateRangePlan.GetAnchorYears" />.
	/// </summary>
	/// <param name="plan">The active resolution plan.</param>
	/// <param name="cache">The shared cache being populated.</param>
	private void ProcessAlgorithmicAnchors(NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
	{
		foreach (string anchorName in plan.RequiredAnchorNames())
		{
			if (!_analysis.TryGetProfile(anchorName, out RuleStaticProfile profile)) continue;
			if (profile.Tier != RuleTier.Algorithmic) continue;

			IReadOnlyList<int> years = plan.GetAnchorYears(anchorName);
			foreach (int year in years)
			{
				if (!NotableDateRuleResolver.IsApplicable(profile.Rule, year))
					continue;

				DateTime? anchor;
				try
				{
					anchor = _ruleResolver.ResolveAnchorDate(profile.Rule, year);
				}
				catch (InvalidOperationException)
				{
					continue;
				}

				if (anchor is null) continue;

				AddEntries(profile, year, anchor.Value, plan, cache);
			}
		}
	}

	/// <summary>
	/// Materialises a Tier 2 or Tier 4 offset rule by reading its root anchor from the cache and applying the static
	/// <see cref="RuleStaticProfile.OffsetFromRoot" />.
	/// </summary>
	/// <param name="profile">The offset rule profile.</param>
	/// <param name="plan">The active resolution plan.</param>
	/// <param name="cache">The shared cache being populated.</param>
	private void ProcessOffsetFromCached(RuleStaticProfile profile, NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
	{
		if (string.IsNullOrWhiteSpace(profile.RootAnchorRuleName)) return;

		IEnumerable<int> years = profile.Tier == RuleTier.OffsetFromAlgorithmic
			? plan.GetAnchorYears(profile.RootAnchorRuleName!)
			: plan.CandidateYears;

		foreach (int year in years)
		{
			if (!NotableDateRuleResolver.IsApplicable(profile.Rule, year))
				continue;

			DateTime? anchor = cache.ResolveAnchor(profile.RootAnchorRuleName!, year);
			if (anchor is null) continue;

			DateTime occurrence;
			try
			{
				occurrence = anchor.Value.Date.AddDays(profile.OffsetFromRoot);
			}
			catch (ArgumentOutOfRangeException)
			{
				continue;
			}

			AddEntries(profile, year, occurrence, plan, cache);
		}
	}

	/// <summary>
	/// Builds and adds cache entries for the supplied rule, anchor year, and anchor date, expanding the rule's authored territory
	/// list into one entry per territory that matches the request context.
	/// </summary>
	/// <param name="profile">The rule profile being materialised.</param>
	/// <param name="year">The anchor year.</param>
	/// <param name="anchorDate">The resolved anchor date.</param>
	/// <param name="plan">The active resolution plan.</param>
	/// <param name="cache">The shared cache being populated.</param>
	private static void AddEntries(
		RuleStaticProfile profile,
		int year,
		DateTime anchorDate,
		NotableDateRangePlan plan,
		NotableDateRangeResolutionCache cache)
	{
		foreach (string? territory in EnumerateApplicableTerritories(profile.Rule, plan.Request.TerritoryCode))
		{
			NotableDate notable = BuildNotableDate(profile.Rule, anchorDate, territory, adjustmentReason: null);

			NotableDateCacheState state = NotableDateCacheState.Computed;
			if (Intersects(plan.Request.StartDate, plan.Request.EndDate, notable.Date, notable.EndDate))
			{
				if (plan.Request.Filter is null || plan.Request.Filter.IsMatch(notable))
					state = NotableDateCacheState.InWindow;
			}

			NotableDateCacheEntry entry = new(profile, year, notable, state);
			cache.Add(entry);
		}
	}

	/// <summary>
	/// Applies observance adjustments to every cache entry that has at least one configured adjustment.
	/// </summary>
	/// <param name="plan">The active resolution plan.</param>
	/// <param name="cache">The shared cache.</param>
	private void ApplyAdjustments(NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
	{
		NotableDateAdjuster adjuster = new(
			IsWeekend,
			(date, territory, calendar) => IsNonWorkingDay(cache, date, territory, calendar),
			_weekendDefinition,
			_weekendProvider,
			_handlerRegistry,
			(name, year, territory, calendar) => cache.ResolveObservedByName(name, year, territory, calendar));

		// Snapshot first — entries can be mutated as adjustments are applied.
		List<NotableDateCacheEntry> snapshot = cache.Entries.ToList();

		foreach (NotableDateCacheEntry entry in snapshot)
		{
			if (entry.Rule.Adjustments.IsDefaultOrEmpty) continue;

			foreach (ObservanceAdjustment adjustment in entry.Rule.Adjustments.OrderBy(a => a.Priority))
			{
				if (!NotableDateAdjuster.IsInScope(adjustment, entry.AnchorYear, entry.BaseNotable.TerritoryCode, entry.Rule.CalendarType))
					continue;

				AdjustmentApplyResult result = adjuster.Apply(
					adjustment,
					entry.Rule,
					entry.BaseNotable.Date,
					entry.BaseNotable.TerritoryCode,
					entry.Rule.CalendarType);

				if (!result.Activated || result.AdjustedDate.Date == entry.BaseNotable.Date.Date)
					continue;

				string? emittedTerritory = !string.IsNullOrEmpty(adjustment.TerritoryCode)
					? adjustment.TerritoryCode
					: entry.BaseNotable.TerritoryCode;

				bool isNonWorking = result.IsNonWorkingOverride ?? entry.Rule.IsNonWorkingDay ?? false;
				AdjustmentReason reason = new(entry.BaseNotable.Date, result.Trigger, result.Action, result.HandlerKey);
				NotableDate adjusted = BuildNotableDate(entry.Rule, result.AdjustedDate, emittedTerritory, reason, isNonWorking);

				entry.Adjusted = adjusted;

				bool adjustedIntersects = Intersects(plan.Request.StartDate, plan.Request.EndDate, adjusted.Date, adjusted.EndDate);
				bool filterMatch = plan.Request.Filter is null || plan.Request.Filter.IsMatch(adjusted);

				// Always promote to Adjusted when the adjusted date lands inside the request window. The emission step
				// independently checks whether the base date also intersects, so we never lose the base when both are visible.
				if (adjustedIntersects && filterMatch)
					entry.State = NotableDateCacheState.Adjusted;
				else if (entry.State == NotableDateCacheState.Computed)
					entry.State = NotableDateCacheState.AdjustedBlocker;
			}
		}
	}

	/// <summary>
	/// Builds the final emission list from emissable cache entries.
	/// </summary>
	/// <param name="plan">The active resolution plan.</param>
	/// <param name="cache">The shared cache.</param>
	/// <returns>The emission list, ordered by observed date and rule name.</returns>
	/// <remarks>
	/// <para>
	/// Emission policy: an observance adjustment replaces the base anchor — when an entry's <see cref="NotableDateCacheState" /> is
	/// <see cref="NotableDateCacheState.Adjusted" /> only the <see cref="NotableDateCacheEntry.Adjusted" /> form is emitted, never
	/// the underlying anchor. This guarantees that the emitted count equals the notable-date count for the requested window.
	/// </para>
	/// </remarks>
	private static IReadOnlyList<NotableDate> BuildEmissionList(NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
	{
		List<NotableDate> emitted = new();

		foreach (NotableDateCacheEntry entry in cache.EmissableEntries())
		{
			if (entry.State == NotableDateCacheState.Adjusted && entry.Adjusted is not null)
			{
				// Adjusted form supersedes the base — the original anchor is recorded on the adjusted date's AdjustmentReason
				// rather than emitted as a separate entry.
				emitted.Add(entry.Adjusted);
				continue;
			}

			if (entry.State == NotableDateCacheState.InWindow)
				emitted.Add(entry.BaseNotable);
		}

		// Defensive: never emit a notable date whose span lies outside the requested window. The state transitions enforce this on
		// their own, but the explicit guard catches any future regression in the state machine.
		return emitted
			.Where(n => Intersects(plan.Request.StartDate, plan.Request.EndDate, n.Date, n.EndDate))
			.OrderBy(n => n.Date)
			.ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(n => n.TerritoryCode, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	/// <summary>
	/// Determines whether the supplied date is a non-working day from the cache's perspective. Falls back to weekend evaluation.
	/// </summary>
	/// <param name="cache">The active cache.</param>
	/// <param name="date">The date to test.</param>
	/// <param name="territoryCode">The territory context.</param>
	/// <param name="calendarType">The calendar context.</param>
	/// <returns><see langword="true" /> when the date is non-working in context.</returns>
	private bool IsNonWorkingDay(NotableDateRangeResolutionCache cache, DateTime date, string? territoryCode, Type? calendarType)
	{
		if (IsWeekend(date)) return true;
		return cache.IsNonWorkingDay(date, territoryCode, calendarType);
	}

	/// <summary>
	/// Determines whether the supplied date falls on a weekend under the configured weekend definition.
	/// </summary>
	/// <param name="date">The date to test.</param>
	/// <returns><see langword="true" /> when the date is a weekend; otherwise, <see langword="false" />.</returns>
	private bool IsWeekend(DateTime date) =>
		BoduExt.DateTimeExtensions.IsWeekend(date, _weekendDefinition, _weekendProvider);

	/// <summary>
	/// Constructs a <see cref="NotableDate" /> from a rule, its resolved date, and any observance-adjustment metadata.
	/// </summary>
	/// <param name="rule">The originating notable-date rule.</param>
	/// <param name="date">The resolved observed date for the rule.</param>
	/// <param name="territory">The territory code, or <see langword="null" />.</param>
	/// <param name="adjustmentReason">The adjustment reason, or <see langword="null" /> when the date is the base anchor.</param>
	/// <param name="isNonWorkingOverride">An optional override for the non-working flag.</param>
	/// <returns>The constructed <see cref="NotableDate" />.</returns>
	private static NotableDate BuildNotableDate(
		NotableDateRule rule,
		DateTime date,
		string? territory,
		AdjustmentReason? adjustmentReason,
		bool? isNonWorkingOverride = null) =>
		new()
		{
			Date = date.Date,
			Name = rule.Name,
			Category = rule.Category,
			DurationDays = Math.Max(1, rule.DurationDays),
			IsNonWorkingDay = isNonWorkingOverride ?? rule.IsNonWorkingDay ?? false,
			CalendarType = rule.CalendarType,
			TerritoryCode = territory,
			Tags = rule.Tags,
			Comment = rule.Comment,
			AdjustmentReason = adjustmentReason,
		};

	/// <summary>
	/// Splits a comma-separated territory list and returns the entries that overlap the requested territory under parent / child
	/// containment, or yields a single <see langword="null" /> when the rule is territory-neutral.
	/// </summary>
	/// <param name="rule">The rule whose territory list is being expanded.</param>
	/// <param name="requestedTerritory">The requested territory, or <see langword="null" />.</param>
	/// <returns>The applicable territory codes.</returns>
	private static IEnumerable<string?> EnumerateApplicableTerritories(NotableDateRule rule, string? requestedTerritory)
	{
		if (string.IsNullOrEmpty(rule.TerritoryCode))
		{
			yield return null;
			yield break;
		}

		TerritoryCode? requested = null;
		if (!string.IsNullOrEmpty(requestedTerritory) &&
			TerritoryCode.TryParse(requestedTerritory, out TerritoryCode parsed))
		{
			requested = parsed;
		}

		foreach (TerritoryCode owned in TerritoryCode.ParseList(rule.TerritoryCode))
		{
			if (requested is null || owned.Contains(requested.Value) || requested.Value.Contains(owned))
				yield return owned.ToString();
		}
	}

	/// <summary>
	/// Determines whether two inclusive date spans intersect.
	/// </summary>
	/// <param name="leftStart">The first span's start date.</param>
	/// <param name="leftEnd">The first span's end date.</param>
	/// <param name="rightStart">The second span's start date.</param>
	/// <param name="rightEnd">The second span's end date.</param>
	/// <returns><see langword="true" /> when the spans intersect.</returns>
	private static bool Intersects(DateTime leftStart, DateTime leftEnd, DateTime rightStart, DateTime rightEnd) =>
		rightStart.Date <= leftEnd.Date && rightEnd.Date >= leftStart.Date;
}
