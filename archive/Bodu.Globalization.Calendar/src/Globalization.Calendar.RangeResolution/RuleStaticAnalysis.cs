// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleStaticAnalysis.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// The look-up of static profiles keyed by the rule's full composite identity, so same-name variants resolve
    /// distinctly rather than collapsing under a name-only key.
    /// </summary>
    private readonly Dictionary<NotableDateRuleIdentity, RuleStaticProfile> _profilesByIdentity;

    /// <summary>
    /// The case-insensitive look-up of profiles by canonical name, used only by the name-based convenience overload of
    /// <see cref="TryGetProfile(string, out RuleStaticProfile)" /> and suppressed when a name is shared by more than one
    /// variant (see <see cref="_ambiguousNames" />).
    /// </summary>
    private readonly Dictionary<string, RuleStaticProfile> _profilesByName;

    /// <summary>
    /// The canonical names shared by more than one variant; the name-based <see cref="TryGetProfile(string, out RuleStaticProfile)" />
    /// reports no match for these so callers never bind to an arbitrary variant by name.
    /// </summary>
    private readonly HashSet<string> _ambiguousNames;

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
    /// <param name="profilesByIdentity">The lookup of profiles by full composite identity.</param>
    /// <param name="profilesByName">The case-insensitive lookup of profiles by canonical name.</param>
    /// <param name="ambiguousNames">The canonical names shared by more than one variant.</param>
    /// <param name="dependentsByAnchor">
    /// The case-insensitive lookup of profiles whose root anchor is the keyed rule name.
    /// </param>
    /// <param name="globalFringeReach">
    /// The maximum absolute reach across every profile, used by the planner to size fringe scans.
    /// </param>
    private RuleStaticAnalysis(
        List<RuleStaticProfile> profiles,
        Dictionary<NotableDateRuleIdentity, RuleStaticProfile> profilesByIdentity,
        Dictionary<string, RuleStaticProfile> profilesByName,
        HashSet<string> ambiguousNames,
        Dictionary<string, List<RuleStaticProfile>> dependentsByAnchor,
        int globalFringeReach)
    {
        _profiles = profiles;
        _profilesByIdentity = profilesByIdentity;
        _profilesByName = profilesByName;
        _ambiguousNames = ambiguousNames;
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
    /// Attempts to retrieve the profile for the rule with the supplied composite identity.
    /// </summary>
    /// <param name="identity">The full rule identity to look up.</param>
    /// <param name="profile">The matching profile when the method returns <see langword="true" />.</param>
    /// <returns>
    /// <see langword="true" /> when a profile exists for the supplied identity; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetProfile(in NotableDateRuleIdentity identity, out RuleStaticProfile profile)
    {
        if (_profilesByIdentity.TryGetValue(identity, out RuleStaticProfile? found))
        {
            profile = found;
            return true;
        }

        profile = null!;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve a profile for the rule with the supplied canonical name. Reports no match when the name is
    /// shared by more than one variant, so callers never bind to an arbitrary candidate by name; prefer
    /// <see cref="TryGetProfile(in NotableDateRuleIdentity, out RuleStaticProfile)" /> on execution paths.
    /// </summary>
    /// <param name="ruleName">The canonical rule name to look up.</param>
    /// <param name="profile">The matching profile when the method returns <see langword="true" />.</param>
    /// <returns>
    /// <see langword="true" /> when exactly one profile carries the supplied name; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetProfile(string ruleName, out RuleStaticProfile profile)
    {
        if (!string.IsNullOrEmpty(ruleName)
            && !_ambiguousNames.Contains(ruleName)
            && _profilesByName.TryGetValue(ruleName, out RuleStaticProfile? found))
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
        ThrowHelper.ThrowIfNull(rules);

        // An identity-keyed index resolves offset anchors with the same deterministic, variant-aware disambiguation
        // policy as the runtime resolver, so an offset chain binds to the contextually-correct variant (for example
        // western versus orthodox Easter) rather than the first same-name candidate.
        var index = new NotableDateRuleIndex(rules);

        List<RuleStaticProfile> profiles = [];
        Dictionary<NotableDateRuleIdentity, RuleStaticProfile> profilesByIdentity = new();
        Dictionary<string, RuleStaticProfile> profilesByName = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> ambiguousNames = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<RuleStaticProfile>> dependentsByAnchor = new(StringComparer.OrdinalIgnoreCase);

        foreach (NotableDateRule rule in rules)
        {
            RuleStaticProfile profile = BuildProfile(rule, index);
            profiles.Add(profile);

            if (!string.IsNullOrWhiteSpace(rule.Name))
            {
                profilesByIdentity[NotableDateRuleIdentity.From(rule)] = profile;

                if (!profilesByName.TryAdd(rule.Name, profile))
                    ambiguousNames.Add(rule.Name);
            }

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

        return new RuleStaticAnalysis(profiles, profilesByIdentity, profilesByName, ambiguousNames, dependentsByAnchor, globalFringeReach);
    }

    /// <summary>
    /// Computes the static profile for a single rule, walking offset chains until the root anchor is identified.
    /// </summary>
    /// <param name="rule">The rule to profile.</param>
    /// <param name="index">The identity-keyed rule index used to resolve offset anchors.</param>
    /// <returns>The constructed profile.</returns>
    private static RuleStaticProfile BuildProfile(NotableDateRule rule, NotableDateRuleIndex index)
    {
        (RuleTier baseTier, NotableDateRuleIdentity? rootAnchor, var offsetFromRoot) = ClassifyRule(rule, index);

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
    /// <param name="index">The identity-keyed rule index used to resolve offset anchors.</param>
    /// <returns>
    /// The classified tier, root anchor identity (when applicable), and aggregate offset from the root anchor.
    /// </returns>
    private static (RuleTier Tier, NotableDateRuleIdentity? RootAnchor, int OffsetFromRoot) ClassifyRule(
        NotableDateRule rule,
        NotableDateRuleIndex index)
    {
        switch (rule.Strategy)
        {
            case DateResolutionStrategy.Algorithm:
                return (RuleTier.Algorithmic, NotableDateRuleIdentity.From(rule), 0);

            case DateResolutionStrategy.Fixed:
            case DateResolutionStrategy.DayOfWeekInMonth:
            case DateResolutionStrategy.WeekdayNearDate:
            case DateResolutionStrategy.RelativeWeekdayInMonth:
                return (RuleTier.Fixed, null, 0);

            case DateResolutionStrategy.OffsetFromAnchor:
                return ClassifyOffsetChain(rule, index);

            default:
                return (RuleTier.Fixed, null, 0);
        }
    }

    /// <summary>
    /// Walks an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> chain until a non-offset rule is found, summing
    /// the offsets along the way.
    /// </summary>
    /// <param name="rule">The offset rule.</param>
    /// <param name="index">The identity-keyed rule index used to resolve each anchor in the chain.</param>
    /// <returns>The classified tier, root anchor identity, and aggregate offset.</returns>
    /// <remarks>
    /// Each anchor reference is resolved through <see cref="NotableDateRuleIndex" /> using any authored
    /// variant/territory/calendar filter and the requesting rule's context. When a reference cannot be resolved to a
    /// single rule — because it is missing or ambiguous among same-name variants — the chain degrades to
    /// <see cref="RuleTier.Fixed" /> with no root rather than binding to an arbitrary candidate; the strict validation
    /// pass reports the missing/ambiguous reference as an error.
    /// </remarks>
    private static (RuleTier Tier, NotableDateRuleIdentity? RootAnchor, int OffsetFromRoot) ClassifyOffsetChain(
        NotableDateRule rule,
        NotableDateRuleIndex index)
    {
        // Track visited rules by full identity so two same-name variants in a chain are not mistaken for a cycle.
        HashSet<NotableDateRuleIdentity> visited = [];
        NotableDateRule current = rule;
        var aggregateOffset = 0;

        while (current.Strategy == DateResolutionStrategy.OffsetFromAnchor)
        {
            if (string.IsNullOrWhiteSpace(current.AnchorRuleName))
                return (RuleTier.Fixed, null, 0);

            if (!visited.Add(NotableDateRuleIdentity.From(current)))
                return (RuleTier.Fixed, null, 0);

            aggregateOffset += current.OffsetDays ?? 0;

            NotableDateRuleReference reference = new(current.AnchorRuleName!, current.AnchorRuleVariant, current.AnchorTerritoryCode, current.AnchorCalendarType);
            RuleReferenceResult result = index.Resolve(reference, current.TerritoryCode, current.CalendarType);
            if (result.Match != RuleReferenceMatch.Unique || result.Rule is not { } next)
                return (RuleTier.Fixed, null, 0);

            current = next;
        }

        return current.Strategy switch
        {
            DateResolutionStrategy.Algorithm => (RuleTier.OffsetFromAlgorithmic, NotableDateRuleIdentity.From(current), aggregateOffset),
            DateResolutionStrategy.Fixed or DateResolutionStrategy.DayOfWeekInMonth or DateResolutionStrategy.WeekdayNearDate or DateResolutionStrategy.RelativeWeekdayInMonth =>
                (RuleTier.OffsetFromFixed, NotableDateRuleIdentity.From(current), aggregateOffset),
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
    /// required. <see cref="AdjustmentAction.MoveToNextWorkingDay" /> is bounded by the adjuster's 366-day cap; in
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
            AdjustmentAction.MoveToNextWorkingDay => (0, 7),
            AdjustmentAction.ReplaceWithNamedDate => (-31, 31),
            // Custom handlers are arbitrary by definition. Default to a conservative ±31-day envelope so handlers whose shift
            // fits inside one month are admitted out of the box. Authors whose handlers shift further must declare their reach
            // via <see cref="ObservanceAdjustment.MaxAdjustmentReachDays" />.
            AdjustmentAction.Custom => (-31, 31),
            _ => (0, 0),
        };
    }
}
