// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePlanner.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Builds a per-request <see cref="NotableDateRangePlan" /> from a <see cref="NotableDateRangeRequest" /> and a
/// <see cref="RuleStaticAnalysis" />.
/// </summary>
/// <remarks>
/// <para>
/// Rule selection is deliberately simple: a rule is eligible when it passes the static territory, calendar, and filter
/// checks. The main pass evaluates each eligible rule for every civil year that the request window spans, materializing
/// the rule's resolved date and admitting it when it intersects the window.
/// </para>
/// <para>
/// Cross-year roll-overs (for example, <c>31 Dec</c> with a forward observance adjustment landing on <c>3 Jan</c>) are
/// handled by a separate fringe pass at the pipeline level. The planner identifies which adjacent civil years that pass
/// needs to scan and the fringe day-window itself; the actual fringe materialization lives in the pipeline.
/// </para>
/// </remarks>
internal sealed class NotableDateRangePlanner
{
    /// <summary>
    /// The default fringe distance in days, applied either side of the request window when scanning for
    /// adjustment-driven candidates. Most observance adjustments shift dates by less than a week (weekend roll-forward,
    /// weekday substitute), so seven days is a safe envelope that covers
    /// <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> chains in typical rule sets without inflating the search
    /// space.
    /// </summary>
    public const int DefaultFringeDays = 7;

    private readonly RuleStaticAnalysis _analysis;
    private readonly int _fringeDays;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRangePlanner" /> class with the default fringe distance.
    /// </summary>
    /// <param name="analysis">The static rule analysis to use when planning requests.</param>
    /// <exception cref="ArgumentNullException"><paramref name="analysis" /> is <see langword="null" />.</exception>
    public NotableDateRangePlanner(RuleStaticAnalysis analysis)
        : this(analysis, DefaultFringeDays)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRangePlanner" /> class with an explicit fringe distance.
    /// </summary>
    /// <param name="analysis">The static rule analysis to use when planning requests.</param>
    /// <param name="fringeDays">
    /// The fringe distance in days, applied either side of the request window when scanning for adjustment-driven
    /// candidates.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="analysis" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fringeDays" /> is negative.</exception>
    public NotableDateRangePlanner(RuleStaticAnalysis analysis, int fringeDays)
    {
        _analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        if (fringeDays < 0) throw new ArgumentOutOfRangeException(nameof(fringeDays), fringeDays, CalendarResourceStrings.Arg_OutOfRange_FringeDistanceNegative);
        _fringeDays = fringeDays;
    }

    /// <summary>
    /// Builds a <see cref="NotableDateRangePlan" /> for the supplied request.
    /// </summary>
    /// <param name="request">The originating range request.</param>
    /// <returns>The constructed plan.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    public NotableDateRangePlan Plan(NotableDateRangeRequest request)
    {
        ThrowHelper.ThrowIfNull(request);

        // Eligible rules: filtered by static territory, calendar, and filter checks. Year applicability is deferred to per-(rule,
        // year) materialization since FirstYear / LastYear / OccurrenceYears bounds depend on the year being resolved.
        List<RuleStaticProfile> eligible = [];
        foreach (RuleStaticProfile profile in _analysis.Profiles)
        {
            if (IsRuleEligible(profile, request))
                eligible.Add(profile);
        }

        // Same-name territory shadowing: when several rules share a Name and at least one of them targets the requested
        // territory at-or-narrower-than the request, suppress any same-name rule whose territory is strictly broader than
        // the request. This lets data packs author a canonical AU-wide rule alongside per-state variants that override
        // it for specific subdivisions, without producing duplicate emissions on parent-containment matches.
        eligible = ApplySameNameTerritoryShadowing(eligible, request);

        // Main pass candidate years: just the years that the request window itself spans. No global reach expansion.
        List<int> candidateYears = [];
        for (var year = request.StartDate.Year; year <= request.EndDate.Year; year++)
            candidateYears.Add(year);

        // Fringe pass: only relevant if the request window touches a year boundary inside the fringe distance. The fringe scans
        // rules whose un-adjusted date sits within ±effectiveFringeDays of the request edges so the adjustment phase can roll
        // them in. The effective distance is the larger of the configured default and the rule set's worst-case reach so unusual
        // adjustment actions (large AddDays, ReplaceWithNamedDate, Custom) widen the fringe automatically.
        var effectiveFringeDays = _fringeDays > _analysis.GlobalFringeReach ? _fringeDays : _analysis.GlobalFringeReach;
        DateTime fringeStart = AddDaysClamped(request.StartDate, -effectiveFringeDays);
        DateTime fringeEnd = AddDaysClamped(request.EndDate, effectiveFringeDays);

        HashSet<int> fringeYearSet = [];
        for (var year = fringeStart.Year; year <= fringeEnd.Year; year++)
        {
            if (year < request.StartDate.Year || year > request.EndDate.Year)
                fringeYearSet.Add(year);
        }

        var fringeYears = fringeYearSet.OrderBy(y => y).ToList();

        // Anchor years per algorithmic root. Tight envelope: only the years the request range spans, plus any fringe years (since
        // a fringe-pass offset rule may need an adjacent-year algorithmic anchor). Keeps Easter / Lunar New Year / Vesak
        // computation count to the minimum that covers the request without conservative ±1-year padding.
        Dictionary<string, IReadOnlyList<int>> anchorYearsByName = new(StringComparer.OrdinalIgnoreCase);

        HashSet<int> anchorYearSet = new(candidateYears);
        foreach (var year in fringeYears)
            anchorYearSet.Add(year);
        var anchorYears = anchorYearSet.OrderBy(y => y).ToList();

        foreach (RuleStaticProfile profile in eligible)
        {
            if (!profile.DependsOnAlgorithmicAnchor) continue;
            if (string.IsNullOrWhiteSpace(profile.RootAnchorRuleName)) continue;

            var anchorName = profile.RootAnchorRuleName!;
            if (!anchorYearsByName.ContainsKey(anchorName))
                anchorYearsByName[anchorName] = anchorYears;
        }

        return new NotableDateRangePlan(
            request,
            eligible,
            candidateYears,
            fringeYears,
            fringeStart,
            fringeEnd,
            anchorYearsByName);
    }

    /// <summary>
    /// Determines whether the rule may contribute to the request based on territory, calendar, and filter eligibility.
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

        return RuleMayApplyToTerritory(rule, request.TerritoryCode);
    }

    /// <summary>
    /// Returns <see langword="true" /> when the rule's authored territory list intersects the requested territory using
    /// parent / child containment semantics.
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
    /// Suppresses same-name rules whose territory is strictly broader than the request when a narrower-or-equal
    /// same-name rule is present in <paramref name="eligible" />. See the planner's call-site comment for the
    /// motivating data-pack pattern (canonical national rule + per-subdivision overrides).
    /// </summary>
    /// <param name="eligible">The pre-shadowed eligible-rule list (mutated in place not allowed; returns a new list).</param>
    /// <param name="request">The originating request.</param>
    /// <returns>The eligible list with broader-than-request same-name rules removed where a narrower one exists.</returns>
    /// <remarks>
    /// <para>
    /// Shadowing only fires when the request carries a parseable territory. Any-territory queries (a <see langword="null" />
    /// or empty request territory) intentionally keep every eligible rule so that callers asking for "everything" still
    /// see the canonical and per-subdivision variants side by side.
    /// </para>
    /// <para>
    /// Specificity is determined per rule by inspecting its authored territory list:
    /// </para>
    /// <list type="bullet">
    /// <item><description>A rule is <i>at-or-narrower-than</i> the request when at least one of its territories is contained in (or equal to) the request territory.</description></item>
    /// <item><description>A rule is <i>strictly broader</i> when every parseable territory it declares strictly contains the request (i.e. is a proper ancestor).</description></item>
    /// </list>
    /// <para>
    /// Only rules in the second category are suppressed, and only when at least one rule in the first category exists
    /// under the same <see cref="NotableDateRule.Name" /> (case-insensitive). Rules with no <see cref="NotableDateRule.Name" />
    /// are not grouped — they are passed through unchanged.
    /// </para>
    /// </remarks>
    private static List<RuleStaticProfile> ApplySameNameTerritoryShadowing(
        List<RuleStaticProfile> eligible,
        NotableDateRangeRequest request)
    {
        if (string.IsNullOrEmpty(request.TerritoryCode))
            return eligible;

        if (!TerritoryCode.TryParse(request.TerritoryCode, out TerritoryCode requested))
            return eligible;

        // Group by rule name (case-insensitive). Rules with no name are not eligible for shadowing.
        Dictionary<string, List<RuleStaticProfile>> groups = new(StringComparer.OrdinalIgnoreCase);
        List<RuleStaticProfile> unnamed = [];
        foreach (RuleStaticProfile profile in eligible)
        {
            string? name = profile.Rule.Name;
            if (string.IsNullOrEmpty(name))
            {
                unnamed.Add(profile);
                continue;
            }

            if (!groups.TryGetValue(name, out List<RuleStaticProfile>? bucket))
            {
                bucket = [];
                groups[name] = bucket;
            }

            bucket.Add(profile);
        }

        List<RuleStaticProfile> result = [.. unnamed];

        foreach (List<RuleStaticProfile> bucket in groups.Values)
        {
            if (bucket.Count == 1)
            {
                result.Add(bucket[0]);
                continue;
            }

            bool hasNarrowerOrEqual = false;
            foreach (RuleStaticProfile profile in bucket)
            {
                if (HasTerritoryNarrowerOrEqualToRequest(profile.Rule, requested))
                {
                    hasNarrowerOrEqual = true;
                    break;
                }
            }

            if (!hasNarrowerOrEqual)
            {
                result.AddRange(bucket);
                continue;
            }

            foreach (RuleStaticProfile profile in bucket)
            {
                if (IsRuleStrictlyBroaderThanRequest(profile.Rule, requested))
                    continue;

                result.Add(profile);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns <see langword="true" /> when at least one of the rule's declared territories is contained in (or equal
    /// to) the request territory — i.e. the rule is "specific enough" for the request.
    /// </summary>
    /// <param name="rule">The candidate rule.</param>
    /// <param name="requested">The parsed request territory.</param>
    /// <returns><see langword="true" /> when the rule narrows or equals the request.</returns>
    private static bool HasTerritoryNarrowerOrEqualToRequest(NotableDateRule rule, TerritoryCode requested)
    {
        // A rule with no authored territory is treated as global — broader than any specific request.
        if (string.IsNullOrEmpty(rule.TerritoryCode)) return false;

        foreach (TerritoryCode owned in TerritoryCode.ParseList(rule.TerritoryCode))
        {
            if (requested.Contains(owned))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true" /> when every authored territory on the rule strictly contains the request — i.e.
    /// the rule is broader than the request in every variant it declares, with no exact or narrower match.
    /// </summary>
    /// <param name="rule">The candidate rule.</param>
    /// <param name="requested">The parsed request territory.</param>
    /// <returns><see langword="true" /> when the rule is strictly broader than the request.</returns>
    private static bool IsRuleStrictlyBroaderThanRequest(NotableDateRule rule, TerritoryCode requested)
    {
        // A rule with no authored territory is global; treat that as strictly broader than any specific request.
        if (string.IsNullOrEmpty(rule.TerritoryCode)) return true;

        var anyParseable = false;

        foreach (TerritoryCode owned in TerritoryCode.ParseList(rule.TerritoryCode))
        {
            anyParseable = true;

            // The rule has at least one territory that equals or is narrower than the request — not strictly broader.
            if (requested.Contains(owned))
                return false;

            // The rule has at least one territory that doesn't relate to the request — not strictly broader either.
            if (!owned.Contains(requested))
                return false;
        }

        return anyParseable;
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
        return days > 0 && value >= DateTime.MaxValue.AddDays(-days) ? DateTime.MaxValue.Date : value.AddDays(days);
    }
}
