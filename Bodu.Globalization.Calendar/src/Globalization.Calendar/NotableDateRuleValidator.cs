// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleValidator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Performs the strict, post-flatten validation pass over a resolved notable-date rule set, surfacing authoring errors
/// (duplicate identities, missing or ambiguous offset anchors and replacement targets) and warnings (unregistered
/// algorithm keys) before the rules are used to resolve dates.
/// </summary>
internal static class NotableDateRuleValidator
{
    /// <summary>
    /// Validates the supplied rule set and returns the diagnostics it produced.
    /// </summary>
    /// <param name="rules">The flattened, override-merged rules to validate.</param>
    /// <param name="algorithms">The algorithm registry consulted for algorithm-key warnings, or <see langword="null" />.</param>
    /// <returns>The validation diagnostics; empty when the rule set is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="rules" /> is <see langword="null" />.</exception>
    public static IReadOnlyList<NotableDateValidationDiagnostic> Validate(
        IReadOnlyList<NotableDateRule> rules,
        INotableDateAlgorithmRegistry? algorithms)
    {
        ThrowHelper.ThrowIfNull(rules);

        var diagnostics = new List<NotableDateValidationDiagnostic>();
        var index = new NotableDateRuleIndex(rules);

        ReportDuplicateIdentities(rules, diagnostics);

        foreach (NotableDateRule rule in rules)
        {
            if (rule is null)
                continue;

            switch (rule.Strategy)
            {
                case DateResolutionStrategy.OffsetFromAnchor:
                    ValidateAnchor(rule, index, diagnostics);
                    break;

                case DateResolutionStrategy.Algorithm:
                    ValidateAlgorithm(rule, algorithms, diagnostics);
                    break;
            }

            ValidateReplacementTargets(rule, index, diagnostics);
        }

        return diagnostics;
    }

    /// <summary>
    /// Reports an error for each pair of rules that share the same full <see cref="NotableDateRuleIdentity" />.
    /// </summary>
    /// <param name="rules">The rules being validated.</param>
    /// <param name="diagnostics">The diagnostics list to append to.</param>
    private static void ReportDuplicateIdentities(IReadOnlyList<NotableDateRule> rules, List<NotableDateValidationDiagnostic> diagnostics)
    {
        var seen = new HashSet<NotableDateRuleIdentity>();
        foreach (NotableDateRule rule in rules)
        {
            if (rule is null || string.IsNullOrWhiteSpace(rule.Name))
                continue;

            NotableDateRuleIdentity identity = NotableDateRuleIdentity.From(rule);
            if (!seen.Add(identity))
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "DuplicateIdentity",
                    string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_DuplicateRuleIdentity, identity.Name, identity.RuleName ?? string.Empty, identity.TerritoryCode ?? string.Empty, identity.CalendarType?.FullName ?? string.Empty)));
            }
        }
    }

    /// <summary>
    /// Validates that an offset rule's anchor resolves to exactly one rule in the requesting context.
    /// </summary>
    /// <param name="rule">The offset rule.</param>
    /// <param name="index">The rule index used to resolve the anchor reference.</param>
    /// <param name="diagnostics">The diagnostics list to append to.</param>
    private static void ValidateAnchor(NotableDateRule rule, NotableDateRuleIndex index, List<NotableDateValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(rule.AnchorRuleName))
            return;

        RuleReferenceResult result = index.Resolve(NotableDateRuleReference.ForName(rule.AnchorRuleName!), rule.TerritoryCode, rule.CalendarType);
        switch (result.Match)
        {
            case RuleReferenceMatch.None:
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "MissingAnchor",
                    string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_AnchorRuleNotFound, rule.AnchorRuleName, rule.Name)));
                break;

            case RuleReferenceMatch.Ambiguous:
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "AmbiguousAnchor",
                    string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_AmbiguousAnchorReference, rule.AnchorRuleName, rule.Name, result.CandidateCount)));
                break;
        }
    }

    /// <summary>
    /// Validates that an algorithm rule's key is registered, warning when it is not and no explicit type is supplied.
    /// </summary>
    /// <param name="rule">The algorithm rule.</param>
    /// <param name="algorithms">The algorithm registry, or <see langword="null" />.</param>
    /// <param name="diagnostics">The diagnostics list to append to.</param>
    private static void ValidateAlgorithm(NotableDateRule rule, INotableDateAlgorithmRegistry? algorithms, List<NotableDateValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(rule.AlgorithmKey) || rule.AlgorithmType is not null)
            return;

        if (algorithms is null || !algorithms.TryGet(rule.AlgorithmKey!, out _))
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Warning,
                "UnregisteredAlgorithm",
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_AlgorithmKeyNotRegistered, rule.AlgorithmKey, rule.Name)));
        }
    }

    /// <summary>
    /// Validates that every <see cref="AdjustmentAction.ReplaceWithNamedDate" /> adjustment on the rule resolves to
    /// exactly one target.
    /// </summary>
    /// <param name="rule">The rule whose adjustments are inspected.</param>
    /// <param name="index">The rule index used to resolve replacement targets.</param>
    /// <param name="diagnostics">The diagnostics list to append to.</param>
    private static void ValidateReplacementTargets(NotableDateRule rule, NotableDateRuleIndex index, List<NotableDateValidationDiagnostic> diagnostics)
    {
        if (rule.Adjustments.IsDefaultOrEmpty)
            return;

        foreach (ObservanceAdjustment adjustment in rule.Adjustments)
        {
            if (adjustment.Action != AdjustmentAction.ReplaceWithNamedDate || string.IsNullOrWhiteSpace(adjustment.TargetRuleName))
                continue;

            NotableDateRuleReference reference = new(adjustment.TargetRuleName!, adjustment.TargetRuleVariant);
            RuleReferenceResult result = index.Resolve(reference, rule.TerritoryCode, rule.CalendarType);
            if (result.Match == RuleReferenceMatch.Ambiguous)
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "AmbiguousReplacementTarget",
                    string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_AmbiguousReplacementTarget, adjustment.TargetRuleName, adjustment.Key, rule.Name, result.CandidateCount)));
            }
            else if (result.Match == RuleReferenceMatch.None)
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "MissingReplacementTarget",
                    string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_AnchorRuleNotFound, adjustment.TargetRuleName, rule.Name)));
            }
        }
    }
}
