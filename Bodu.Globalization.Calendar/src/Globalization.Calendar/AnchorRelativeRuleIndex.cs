// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchorRelativeRuleIndex.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Indexes rules that are calculated as offsets from another named anchor rule.
/// </summary>
/// <remarks>
/// <para>
/// The index supports efficient lookup of rules such as <c>Start of Lent</c>, <c>Palm Sunday</c>, <c>Good Friday</c>,
/// and <c>Easter Monday</c> after the shared calculation anchor has been resolved.
/// </para>
/// <para>
/// Once the anchor date is known, a requested date window can be converted into a minimum and maximum offset. Only
/// rules whose authored offsets fall within that range need to be materialized.
/// </para>
/// </remarks>
internal sealed class AnchorRelativeRuleIndex
{
    private readonly Dictionary<string, SortedDictionary<int, List<NotableDateRule>>> _rulesByAnchorAndOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnchorRelativeRuleIndex" /> class.
    /// </summary>
    /// <param name="rules">The rules to index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rules" /> is <see langword="null" />.</exception>
    public AnchorRelativeRuleIndex(IEnumerable<NotableDateRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        _rulesByAnchorAndOffset = new Dictionary<string, SortedDictionary<int, List<NotableDateRule>>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (NotableDateRule rule in rules)
        {
            if (rule.Strategy != DateResolutionStrategy.OffsetFromAnchor)
                continue;

            if (string.IsNullOrWhiteSpace(rule.AnchorRuleName))
                continue;

            if (rule.OffsetDays is not { } offsetDays)
                continue;

            if (!_rulesByAnchorAndOffset.TryGetValue(rule.AnchorRuleName, out SortedDictionary<int, List<NotableDateRule>>? byOffset))
            {
                byOffset = [];
                _rulesByAnchorAndOffset.Add(rule.AnchorRuleName, byOffset);
            }

            if (!byOffset.TryGetValue(offsetDays, out List<NotableDateRule>? offsetRules))
            {
                offsetRules = [];
                byOffset.Add(offsetDays, offsetRules);
            }

            offsetRules.Add(rule);
        }
    }

    /// <summary>
    /// Finds anchor-relative rules whose offset falls within the specified inclusive range.
    /// </summary>
    /// <param name="anchorRuleName">The anchor rule name.</param>
    /// <param name="minOffsetDays">The inclusive minimum offset, in days.</param>
    /// <param name="maxOffsetDays">The inclusive maximum offset, in days.</param>
    /// <returns>The matching rules, ordered by offset and then by rule priority and name.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="anchorRuleName" /> is <see langword="null" />, empty, or consists only of white-space
    /// characters.
    /// </exception>
    public IReadOnlyList<NotableDateRule> FindRules(
        string anchorRuleName,
        int minOffsetDays,
        int maxOffsetDays)
    {
        if (string.IsNullOrWhiteSpace(anchorRuleName))
            throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_AnchorRuleNameNullOrWhiteSpace, nameof(anchorRuleName));

        if (minOffsetDays > maxOffsetDays)
            return [];

        if (!_rulesByAnchorAndOffset.TryGetValue(anchorRuleName, out SortedDictionary<int, List<NotableDateRule>>? byOffset))
            return [];

        List<NotableDateRule> results = [];

        foreach (KeyValuePair<int, List<NotableDateRule>> pair in byOffset)
        {
            if (pair.Key < minOffsetDays)
                continue;

            if (pair.Key > maxOffsetDays)
                break;

            results.AddRange(pair.Value);
        }

        return [.. results
            .OrderBy(rule => rule.OffsetDays.GetValueOrDefault())
            .ThenBy(rule => rule.Priority)
            .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase)];
    }
}