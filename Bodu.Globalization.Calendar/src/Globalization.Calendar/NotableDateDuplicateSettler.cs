// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDuplicateSettler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Applies a document's <see cref="DuplicatePolicy" /> to the concepts and rules the document declares, before its
/// imports are composed and its overrides applied.
/// </summary>
/// <remarks>
/// <para>
/// The policy is applied within the one document, in document order, and only to its own declarations: imported
/// documents keep their composition rules (the first import that supplies a concept wins, and a local concept replaces
/// an imported one), and an <c>AddRule</c> override whose rule identifier already exists stays an error.
/// <see cref="DuplicatePolicy.Error" /> leaves the declarations as they are, so the validator reports
/// <c>BODU-CAL-DUP-ND</c> and <c>BODU-CAL-DUP-RULE</c>.
/// </para>
/// </remarks>
internal static class NotableDateDuplicateSettler
{
    /// <summary>
    /// Returns the document with its own duplicate concepts and rules settled by its duplicate policy.
    /// </summary>
    /// <param name="document">The parsed document.</param>
    /// <returns>
    /// <paramref name="document" /> itself under <see cref="DuplicatePolicy.Error" />; otherwise a copy whose concepts
    /// are settled.
    /// </returns>
    public static ParsedNotableDateDocument Settle(ParsedNotableDateDocument document)
    {
        IReadOnlyList<NotableDateDefinition> concepts = document.ResolutionPolicy.DuplicatePolicy switch
        {
            DuplicatePolicy.KeepFirst => KeepOne(document.NotableDates, keepLast: false),
            DuplicatePolicy.KeepLast => KeepOne(document.NotableDates, keepLast: true),
            DuplicatePolicy.Merge => Merge(document.NotableDates),
            _ => document.NotableDates,
        };

        return ReferenceEquals(concepts, document.NotableDates)
            ? document
            : new ParsedNotableDateDocument(
                document.ResourceId,
                document.SchemaVersion,
                document.ResolutionPolicy,
                document.AdjustmentPolicies,
                concepts,
                document.Overrides,
                document.Imports);
    }

    /// <summary>
    /// Keeps one declaration of each concept identifier, the first or the last, at its own position, and within each
    /// kept concept one declaration of each rule identifier, chosen the same way.
    /// </summary>
    /// <param name="concepts">The declared concepts, in document order.</param>
    /// <param name="keepLast">
    /// <see langword="true" /> to keep the last declaration of an identifier; <see langword="false" /> to keep the
    /// first.
    /// </param>
    /// <returns>The kept concepts, in document order.</returns>
    private static List<NotableDateDefinition> KeepOne(IReadOnlyList<NotableDateDefinition> concepts, bool keepLast) =>
        [.. KeepOne(concepts, concept => concept.Id, keepLast).Select(concept => WithRules(concept, KeepOne(concept.Rules, rule => rule.Id, keepLast)))];

    /// <summary>
    /// Keeps one item of each identifier, the first or the last, at its own position.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items, in document order.</param>
    /// <param name="identify">Selects an item's identifier.</param>
    /// <param name="keepLast">
    /// <see langword="true" /> to keep the last item of an identifier; <see langword="false" /> to keep the first.
    /// </param>
    /// <returns>The kept items, in document order.</returns>
    private static List<T> KeepOne<T>(IReadOnlyList<T> items, Func<T, string> identify, bool keepLast)
    {
        Dictionary<string, int> kept = new(StringComparer.Ordinal);
        for (int i = 0; i < items.Count; i++)
        {
            string id = identify(items[i]);
            if (keepLast || !kept.ContainsKey(id))
                kept[id] = i;
        }

        List<T> result = new(kept.Count);
        for (int i = 0; i < items.Count; i++)
        {
            if (kept[identify(items[i])] == i)
                result.Add(items[i]);
        }

        return result;
    }

    /// <summary>
    /// Merges each later declaration of a concept identifier into the first: its rules are appended to the first
    /// declaration's, in document order, and the first declaration's own fields are kept.
    /// </summary>
    /// <param name="concepts">The declared concepts, in document order.</param>
    /// <returns>The merged concepts, each at the position of its first declaration.</returns>
    /// <remarks>
    /// Rules are not merged: two rules that share an identifier, declared in one concept or brought together here, stay
    /// for the validator to report as <c>BODU-CAL-DUP-RULE</c>.
    /// </remarks>
    private static List<NotableDateDefinition> Merge(IReadOnlyList<NotableDateDefinition> concepts)
    {
        Dictionary<string, int> positions = new(StringComparer.Ordinal);
        List<NotableDateDefinition> firsts = new();
        List<List<NotableDateRule>> rules = new();

        foreach (NotableDateDefinition concept in concepts)
        {
            if (positions.TryGetValue(concept.Id, out int position))
            {
                rules[position].AddRange(concept.Rules);
                continue;
            }

            positions[concept.Id] = firsts.Count;
            firsts.Add(concept);
            rules.Add([.. concept.Rules]);
        }

        List<NotableDateDefinition> merged = new(firsts.Count);
        for (int i = 0; i < firsts.Count; i++)
            merged.Add(WithRules(firsts[i], rules[i]));

        return merged;
    }

    /// <summary>
    /// Returns a concept with the supplied rules, or the concept itself when the rules are its own.
    /// </summary>
    /// <param name="concept">The concept.</param>
    /// <param name="rules">The rules it holds.</param>
    /// <returns>The concept with <paramref name="rules" />.</returns>
    private static NotableDateDefinition WithRules(NotableDateDefinition concept, List<NotableDateRule> rules) =>
        rules.Count == concept.Rules.Count
            ? concept
            : new NotableDateDefinition(concept.Id, concept.DisplayName, concept.Category, concept.DefaultNonWorkingDay, concept.DefaultDurationDays, concept.Tags, rules);
}
