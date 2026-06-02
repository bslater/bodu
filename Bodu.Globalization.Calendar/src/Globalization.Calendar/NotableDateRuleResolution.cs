// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResolution.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Classifies the outcome of resolving a <see cref="NotableDateRuleReference" /> against a rule set.
/// </summary>
internal enum RuleReferenceMatch
{
    /// <summary>No rule matched the reference.</summary>
    None,

    /// <summary>Exactly one rule was selected, either directly or after context disambiguation.</summary>
    Unique,

    /// <summary>More than one rule matched and no single candidate could be selected.</summary>
    Ambiguous,
}

/// <summary>
/// Represents the result of resolving a <see cref="NotableDateRuleReference" />: the match classification, the selected
/// rule when unique, and the number of candidates that matched the reference filters.
/// </summary>
/// <param name="Match">The match classification.</param>
/// <param name="Rule">The selected rule when <see cref="Match" /> is <see cref="RuleReferenceMatch.Unique" />; otherwise <see langword="null" />.</param>
/// <param name="CandidateCount">The number of rules that matched the reference's own filters, before context disambiguation.</param>
internal readonly record struct RuleReferenceResult(RuleReferenceMatch Match, NotableDateRule? Rule, int CandidateCount);

/// <summary>
/// Builds an identity-keyed index over a resolved rule set and resolves <see cref="NotableDateRuleReference" /> values
/// — used by offset anchors, replacement targets, removals, and nested-override targeting — applying a deterministic
/// context-based disambiguation policy when a name matches more than one variant.
/// </summary>
/// <remarks>
/// <para>
/// The index is built once per service from the flattened, override-merged rule set. Because the flatten pipeline keys
/// rules by full <see cref="NotableDateRuleIdentity" />, two rules with the same identity reaching the index indicate an
/// authoring error and are rejected at construction time.
/// </para>
/// <para>
/// A name-only reference still resolves uniquely when a single candidate carries that name (the dominant case for
/// authored cookbooks). When several variants share a name, the reference's own optional filters are applied first, then
/// the requesting context's territory and calendar are used to prefer a single candidate. If no single candidate
/// survives, resolution is reported as <see cref="RuleReferenceMatch.Ambiguous" /> so callers can raise a precise
/// diagnostic instead of binding to an arbitrary variant.
/// </para>
/// </remarks>
internal sealed class NotableDateRuleIndex
{
    /// <summary>
    /// Maps each rule's composite identity to the rule, guaranteeing uniqueness within the service.
    /// </summary>
    private readonly Dictionary<NotableDateRuleIdentity, NotableDateRule> _byIdentity = new();

    /// <summary>
    /// Groups rules by canonical name (case-insensitive) so a name reference can enumerate its candidate variants.
    /// </summary>
    private readonly Dictionary<string, List<NotableDateRule>> _byName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRuleIndex" /> class from a resolved rule set.
    /// </summary>
    /// <param name="rules">The flattened, override-merged rules to index.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="rules" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Thrown if two rules share the same <see cref="NotableDateRuleIdentity" />.</exception>
    public NotableDateRuleIndex(IReadOnlyList<NotableDateRule> rules)
    {
        ThrowHelper.ThrowIfNull(rules);

        foreach (NotableDateRule rule in rules)
        {
            if (rule is null || string.IsNullOrWhiteSpace(rule.Name))
                continue;

            NotableDateRuleIdentity identity = NotableDateRuleIdentity.From(rule);
            if (!_byIdentity.TryAdd(identity, rule))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    CalendarResourceStrings.Op_Invalid_DuplicateRuleIdentity,
                    identity.Name,
                    identity.RuleName ?? string.Empty,
                    identity.TerritoryCode ?? string.Empty,
                    identity.CalendarType?.FullName ?? string.Empty));
            }

            if (!_byName.TryGetValue(rule.Name, out List<NotableDateRule>? bucket))
            {
                bucket = [];
                _byName[rule.Name] = bucket;
            }

            bucket.Add(rule);
        }
    }

    /// <summary>
    /// Attempts to retrieve the rule with the exact supplied identity.
    /// </summary>
    /// <param name="identity">The composite identity to look up.</param>
    /// <param name="rule">When this method returns <see langword="true" />, the matching rule; otherwise <see langword="null" />.</param>
    /// <returns><see langword="true" /> if a rule with the exact identity exists; otherwise <see langword="false" />.</returns>
    public bool TryGetByIdentity(in NotableDateRuleIdentity identity, out NotableDateRule? rule) =>
        _byIdentity.TryGetValue(identity, out rule);

    /// <summary>
    /// Resolves <paramref name="reference" /> against the indexed rules, applying the reference's own filters and then
    /// disambiguating remaining candidates by the requesting context's territory and calendar.
    /// </summary>
    /// <param name="reference">The reference to resolve.</param>
    /// <param name="contextTerritory">The territory of the requesting rule, used to prefer a same-territory candidate.</param>
    /// <param name="contextCalendar">The calendar of the requesting rule, used to prefer a same-calendar candidate.</param>
    /// <returns>A <see cref="RuleReferenceResult" /> describing the match classification and selected rule.</returns>
    public RuleReferenceResult Resolve(in NotableDateRuleReference reference, string? contextTerritory, Type? contextCalendar)
    {
        if (string.IsNullOrWhiteSpace(reference.Name)
            || !_byName.TryGetValue(reference.Name, out List<NotableDateRule>? candidates))
        {
            return new RuleReferenceResult(RuleReferenceMatch.None, null, 0);
        }

        NotableDateRuleReference selector = reference;
        List<NotableDateRule> filtered = candidates.FindAll(c => MatchesFilters(c, selector));
        if (filtered.Count == 0)
            return new RuleReferenceResult(RuleReferenceMatch.None, null, 0);

        if (filtered.Count == 1)
            return new RuleReferenceResult(RuleReferenceMatch.Unique, filtered[0], 1);

        NotableDateRule? winner = Disambiguate(filtered, contextTerritory, contextCalendar);
        return winner is null
            ? new RuleReferenceResult(RuleReferenceMatch.Ambiguous, null, filtered.Count)
            : new RuleReferenceResult(RuleReferenceMatch.Unique, winner, filtered.Count);
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="rule" /> satisfies every filter the reference supplies
    /// (rule name, territory, and calendar type). Absent filters are treated as wildcards.
    /// </summary>
    /// <param name="rule">The candidate rule.</param>
    /// <param name="reference">The reference whose filters are applied.</param>
    /// <returns><see langword="true" /> if the rule matches all supplied filters; otherwise <see langword="false" />.</returns>
    private static bool MatchesFilters(NotableDateRule rule, in NotableDateRuleReference reference)
    {
        if (!string.IsNullOrEmpty(reference.RuleName)
            && !string.Equals(rule.RuleName ?? string.Empty, reference.RuleName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(reference.TerritoryCode)
            && !TerritoriesCompatible(rule.TerritoryCode, reference.TerritoryCode))
        {
            return false;
        }

        return reference.CalendarType is null || rule.CalendarType == reference.CalendarType;
    }

    /// <summary>
    /// Selects a single candidate by preferring, in order, an exact territory-and-calendar context match, then a
    /// territory match, then a calendar match. Returns <see langword="null" /> when no tier yields exactly one survivor.
    /// </summary>
    /// <param name="candidates">The candidates remaining after the reference filters.</param>
    /// <param name="contextTerritory">The requesting context's territory.</param>
    /// <param name="contextCalendar">The requesting context's calendar.</param>
    /// <returns>The disambiguated rule, or <see langword="null" /> when the reference remains ambiguous.</returns>
    private static NotableDateRule? Disambiguate(List<NotableDateRule> candidates, string? contextTerritory, Type? contextCalendar)
    {
        var territoryKey = NotableDateRuleIdentity.NormalizeTerritory(contextTerritory);

        List<NotableDateRule> territoryAndCalendar = candidates.FindAll(c =>
            string.Equals(NotableDateRuleIdentity.NormalizeTerritory(c.TerritoryCode), territoryKey, StringComparison.OrdinalIgnoreCase)
            && c.CalendarType == contextCalendar);
        if (territoryAndCalendar.Count == 1)
            return territoryAndCalendar[0];

        List<NotableDateRule> territoryOnly = candidates.FindAll(c =>
            string.Equals(NotableDateRuleIdentity.NormalizeTerritory(c.TerritoryCode), territoryKey, StringComparison.OrdinalIgnoreCase));
        if (territoryOnly.Count == 1)
            return territoryOnly[0];

        List<NotableDateRule> calendarOnly = candidates.FindAll(c => c.CalendarType == contextCalendar);
        return calendarOnly.Count == 1 ? calendarOnly[0] : null;
    }

    /// <summary>
    /// Determines whether two territory strings are compatible by bidirectional containment, treating an absent value
    /// on either side as a global (compatible) scope.
    /// </summary>
    /// <param name="candidate">The candidate rule's territory string.</param>
    /// <param name="reference">The reference's territory filter.</param>
    /// <returns><see langword="true" /> if the territories are compatible; otherwise <see langword="false" />.</returns>
    private static bool TerritoriesCompatible(string? candidate, string? reference)
    {
        if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(reference))
            return true;

        foreach (TerritoryCode left in TerritoryCode.ParseList(candidate))
        {
            foreach (TerritoryCode right in TerritoryCode.ParseList(reference))
            {
                if (left.Contains(right) || right.Contains(left))
                    return true;
            }
        }

        return false;
    }
}
