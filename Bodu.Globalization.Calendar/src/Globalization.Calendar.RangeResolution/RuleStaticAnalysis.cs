// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleStaticAnalysis.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Aggregates the static, year-independent analysis of a notable-date rule set used by the chronological
/// range-resolution pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The analysis exposes per-rule <see cref="RuleStaticProfile" /> records and a look-up index for offset-relative
/// dependencies. It is built once from the effective rule list and re-used by every range-resolution request.
/// </para>
/// </remarks>
internal sealed class RuleStaticAnalysis
{
    /// <summary>
    /// The case-insensitive look-up of static profiles keyed by canonical rule name.
    /// </summary>
    private readonly Dictionary<string, RuleStaticProfile> _profilesByRuleName;

    /// <summary>
    /// The case-insensitive look-up of profiles whose root anchor matches the keyed rule name.
    /// </summary>
    private readonly Dictionary<string, List<RuleStaticProfile>> _dependentsByAnchor;

    /// <summary>
    /// The full set of static profiles in input order.
    /// </summary>
    private readonly List<RuleStaticProfile> _profiles;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleStaticAnalysis" /> class.
    /// </summary>
    /// <param name="profiles">The static profile per rule.</param>
    /// <param name="profilesByRuleName">The case-insensitive lookup of profiles by rule name.</param>
    /// <param name="dependentsByAnchor">
    /// The case-insensitive lookup of profiles whose root anchor is the keyed rule name.
    /// </param>
    /// <param name="globalFringeReach">
    /// The maximum absolute reach across every profile, used by the planner to size fringe scans.
    /// </param>
    private RuleStaticAnalysis(
        List<RuleStaticProfile> profiles,
        Dictionary<string, RuleStaticProfile> profilesByRuleName,
        Dictionary<string, List<RuleStaticProfile>> dependentsByAnchor,
        int globalFringeReach)
    {
        _profiles = profiles;
        _profilesByRuleName = profilesByRuleName;
        _dependentsByAnchor = dependentsByAnchor;
        GlobalFringeReach = globalFringeReach;
    }

    /// <summary>
    /// Gets the profile for every rule processed by the pipeline, in input order.
    /// </summary>
    /// <returns>A read-only list of <see cref="RuleStaticProfile" /> entries in stable input order.</returns>
    public IReadOnlyList<RuleStaticProfile> Profiles => _profiles;

    /// <summary>
    /// Gets the maximum absolute day-delta across every rule's observable reach (forward or backward). Used by the
    /// planner to size the fringe scan distance so that adjustment shifts and multi-day spans extending across year
    /// boundaries are admitted into the fringe pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is intentionally distinct from per-rule reach: the planner needs a single fringe distance to decide which
    /// adjacent civil years to scan, while the pipeline filters individual fringe-year materializations using each
    /// rule's own <see cref="RuleStaticProfile.MinObservedReach" /> / <see cref="RuleStaticProfile.MaxObservedReach" />
    /// .
    /// </para>
    /// </remarks>
    /// <returns>
    /// A non-negative day count expressing the largest possible adjustment shift across the rule set.
    /// </returns>
    public int GlobalFringeReach { get; }

    /// <summary>
    /// Attempts to retrieve a profile for the rule with the supplied name.
    /// </summary>
    /// <param name="ruleName">The rule name to look up.</param>
    /// <param name="profile">The matching profile when the method returns <see langword="true" />.</param>
    /// <returns>
    /// <see langword="true" /> when a profile exists for the supplied rule name; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetProfile(string ruleName, out RuleStaticProfile profile)
    {
        if (_profilesByRuleName.TryGetValue(ruleName, out RuleStaticProfile? found))
        {
            profile = found;
            return true;
        }

        profile = null!;
        return false;
    }

    /// <summary>
    /// Gets the profiles whose root anchor is the rule with the supplied name. Returns an empty list when the anchor
    /// has no dependents.
    /// </summary>
    /// <param name="anchorRuleName">The anchor rule name to look up.</param>
    /// <returns>The dependent profiles, in declaration order.</returns>
    public IReadOnlyList<RuleStaticProfile> GetDependents(string anchorRuleName) =>
        _dependentsByAnchor.TryGetValue(anchorRuleName, out List<RuleStaticProfile>? list)
            ? list
            : Array.Empty<RuleStaticProfile>();

    /// <summary>
    /// Builds a <see cref="RuleStaticAnalysis" /> from the supplied rule set.
    /// </summary>
    /// <param name="rules">The effective rules to analyze.</param>
    /// <returns>The constructed analysis.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rules" /> is <see langword="null" />.</exception>
    public static RuleStaticAnalysis Build(IReadOnlyList<NotableDateRule> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));

        Dictionary<string, NotableDateRule> rulesByName = new(StringComparer.OrdinalIgnoreCase);
        foreach (NotableDateRule rule in rules)
        {
            if (!string.IsNullOrWhiteSpace(rule.Name))
                rulesByName[rule.Name] = rule;
        }

        List<RuleStaticProfile> profiles = [];
        Dictionary<string, RuleStaticProfile> profilesByRuleName = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<RuleStaticProfile>> dependentsByAnchor = new(StringComparer.OrdinalIgnoreCase);

        foreach (NotableDateRule rule in rules)
        {
            RuleStaticProfile profile = BuildProfile(rule, rulesByName);
            profiles.Add(profile);

            if (!string.IsNullOrWhiteSpace(rule.Name))
                profilesByRuleName[rule.Name] = profile;

            if (!string.IsNullOrWhiteSpace(profile.RootAnchorRuleName))
            {
                if (!dependentsByAnchor.TryGetValue(profile.RootAnchorRuleName!, out List<RuleStaticProfile>? list))
                {
                    list = [];
                    dependentsByAnchor[profile.RootAnchorRuleName!] = list;
                }

                list.Add(profile);
            }
        }

        var globalFringeReach = 0;
        foreach (RuleStaticProfile profile in profiles)
        {
            var forward = profile.MaxObservedReach > 0 ? profile.MaxObservedReach : 0;
            var backward = profile.MinObservedReach < 0 ? -profile.MinObservedReach : 0;
            var magnitude = forward > backward ? forward : backward;
            if (magnitude > globalFringeReach) globalFringeReach = magnitude;
        }

        return new RuleStaticAnalysis(profiles, profilesByRuleName, dependentsByAnchor, globalFringeReach);
    }

    /// <summary>
    /// Computes the static profile for a single rule, walking offset chains until the root anchor is identified.
    /// </summary>
    /// <param name="rule">The rule to profile.</param>
    /// <param name="rulesByName">The case-insensitive rule lookup.</param>
    /// <returns>The constructed profile.</returns>
    private static RuleStaticProfile BuildProfile(NotableDateRule rule, IReadOnlyDictionary<string, NotableDateRule> rulesByName)
    {
        (RuleTier baseTier, var rootAnchor, var offsetFromRoot) = ClassifyRule(rule, rulesByName);

        ComputeAdjustmentReach(rule, out var adjustmentMin, out var adjustmentMax);

        var durationDays = Math.Max(1, rule.DurationDays);

        // MinObservedReach: the most-negative day delta the rule can produce relative to its own anchor.
        // MaxObservedReach: the most-positive day delta the rule can produce, including duration end-day.
        var minReach = Math.Min(0, adjustmentMin);
        var maxReach = Math.Max(durationDays - 1, adjustmentMax + (durationDays - 1));

        return new RuleStaticProfile(rule, baseTier, rootAnchor, offsetFromRoot, minReach, maxReach);
    }

    /// <summary>
    /// Walks the offset-anchor chain to identify the rule's processing tier, root anchor name, and total day offset
    /// from the root.
    /// </summary>
    /// <param name="rule">The rule to classify.</param>
    /// <param name="rulesByName">The case-insensitive rule lookup.</param>
    /// <returns>
    /// The classified tier, root anchor rule name (when applicable), and aggregate offset from the root anchor.
    /// </returns>
    private static (RuleTier Tier, string? RootAnchorRuleName, int OffsetFromRoot) ClassifyRule(
        NotableDateRule rule,
        IReadOnlyDictionary<string, NotableDateRule> rulesByName)
    {
        switch (rule.Strategy)
        {
            case DateResolutionStrategy.Algorithm:
                return (RuleTier.Algorithmic, rule.Name, 0);

            case DateResolutionStrategy.Fixed:
            case DateResolutionStrategy.DayOfWeekInMonth:
                return (RuleTier.Fixed, null, 0);

            case DateResolutionStrategy.OffsetFromAnchor:
                return ClassifyOffsetChain(rule, rulesByName);

            default:
                return (RuleTier.Fixed, null, 0);
        }
    }

    /// <summary>
    /// Walks an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> chain until a non-offset rule is found, summing
    /// the offsets along the way.
    /// </summary>
    /// <param name="rule">The offset rule.</param>
    /// <param name="rulesByName">The case-insensitive rule lookup.</param>
    /// <returns>The classified tier, root anchor rule name, and aggregate offset.</returns>
    private static (RuleTier Tier, string? RootAnchorRuleName, int OffsetFromRoot) ClassifyOffsetChain(
        NotableDateRule rule,
        IReadOnlyDictionary<string, NotableDateRule> rulesByName)
    {
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
        NotableDateRule current = rule;
        var aggregateOffset = 0;

        while (current.Strategy == DateResolutionStrategy.OffsetFromAnchor)
        {
            if (string.IsNullOrWhiteSpace(current.AnchorRuleName))
                return (RuleTier.Fixed, null, 0);

            if (!visited.Add(current.Name ?? string.Empty))
                return (RuleTier.Fixed, null, 0);

            aggregateOffset += current.OffsetDays ?? 0;

            if (!rulesByName.TryGetValue(current.AnchorRuleName!, out NotableDateRule? next))
                return (RuleTier.Fixed, null, 0);

            current = next;
        }

        return current.Strategy switch
        {
            DateResolutionStrategy.Algorithm => (RuleTier.OffsetFromAlgorithmic, current.Name, aggregateOffset),
            DateResolutionStrategy.Fixed or DateResolutionStrategy.DayOfWeekInMonth =>
                (RuleTier.OffsetFromFixed, current.Name, aggregateOffset),
            _ => (RuleTier.Fixed, null, 0),
        };
    }

    /// <summary>
    /// Calculates the worst-case minimum and maximum day deltas produced by the rule's observance adjustments.
    /// </summary>
    /// <param name="rule">The rule whose adjustments are inspected.</param>
    /// <param name="minDelta">The most-negative day delta any adjustment can produce; never positive.</param>
    /// <param name="maxDelta">The most-positive day delta any adjustment can produce; never negative.</param>
    private static void ComputeAdjustmentReach(NotableDateRule rule, out int minDelta, out int maxDelta)
    {
        minDelta = 0;
        maxDelta = 0;

        foreach (ObservanceAdjustment adjustment in rule.Adjustments)
        {
            (var low, var high) = EstimateAdjustmentReach(adjustment);
            if (low < minDelta) minDelta = low;
            if (high > maxDelta) maxDelta = high;
        }
    }

    /// <summary>
    /// Estimates the worst-case day-delta envelope for a single adjustment.
    /// </summary>
    /// <param name="adjustment">The adjustment under analysis.</param>
    /// <returns>The minimum and maximum day deltas that the adjustment may produce.</returns>
    /// <remarks>
    /// <para>
    /// The estimate is conservative: it overstates rather than understates so that on-demand expansion is rarely
    /// required. <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> is bounded by the adjuster's 366-day cap; in
    /// practice the chains are short and this prototype caps the static estimate at one week.
    /// </para>
    /// </remarks>
    private static (int Min, int Max) EstimateAdjustmentReach(ObservanceAdjustment adjustment)
    {
        // Author-declared reach takes precedence over heuristic estimates. The envelope is treated as symmetric (±value) so a
        // single property covers both forward and backward handlers without forcing every author to think about direction.
        if (adjustment.MaxAdjustmentReachDays is { } declared && declared >= 0)
            return (-declared, declared);

        return adjustment.Action switch
        {
            AdjustmentAction.None => (0, 0),
            AdjustmentAction.AddDays => adjustment.OffsetDays >= 0
                ? (0, adjustment.OffsetDays)
                : (adjustment.OffsetDays, 0),
            AdjustmentAction.MoveToNextWeekday => (0, 3),
            AdjustmentAction.MoveToPreviousWeekday => (-3, 0),
            AdjustmentAction.MoveToNextNonWorkingDay => (0, 7),
            AdjustmentAction.ReplaceWithNamedDate => (-31, 31),
            // Custom handlers are arbitrary by definition. Default to a conservative ±31-day envelope so handlers whose shift
            // fits inside one month are admitted out of the box. Authors whose handlers shift further must declare their reach
            // via <see cref="ObservanceAdjustment.MaxAdjustmentReachDays" />.
            AdjustmentAction.Custom => (-31, 31),
            _ => (0, 0),
        };
    }
}
