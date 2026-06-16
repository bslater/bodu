// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleOverrideApplier.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Applies ID-targeted override operations to a set of notable-date concepts, recording a diagnostic when an operation
/// targets a concept or rule that does not exist.
/// </summary>
internal static class NotableDateRuleOverrideApplier
{
    /// <summary>
    /// Applies the supplied override operations and returns the effective notable-date concepts.
    /// </summary>
    /// <param name="definitions">The notable-date concepts before override application.</param>
    /// <param name="overrides">The override operations to apply, in document order.</param>
    /// <param name="diagnostics">The collection that receives override diagnostics.</param>
    /// <returns>The effective notable-date concepts after applying every override.</returns>
    public static List<NotableDateDefinition> Apply(
        IReadOnlyList<NotableDateDefinition> definitions,
        IReadOnlyList<NotableDateRuleOverride> overrides,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        var working = definitions.ToList();

        foreach (NotableDateRuleOverride operation in overrides)
        {
            int index = working.FindIndex(d => string.Equals(d.Id, operation.NotableDateRef, StringComparison.Ordinal));
            if (index < 0)
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL-OVERRIDE-ND",
                    string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_OverrideNotableDateNotFound, operation.NotableDateRef)));
                continue;
            }

            working[index] = ApplyToDefinition(working[index], operation, diagnostics);
        }

        return working;
    }

    /// <summary>
    /// Applies a single override operation to the concept it targets.
    /// </summary>
    /// <param name="definition">The targeted concept.</param>
    /// <param name="operation">The override operation.</param>
    /// <param name="diagnostics">The collection that receives override diagnostics.</param>
    /// <returns>The updated concept, or the original concept when the operation could not be applied.</returns>
    private static NotableDateDefinition ApplyToDefinition(
        NotableDateDefinition definition,
        NotableDateRuleOverride operation,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        switch (operation)
        {
            case RemoveRuleOverride remove:
                {
                    var rules = definition.Rules.ToList();
                int removed = rules.RemoveAll(r => string.Equals(r.Id, remove.RuleRef, StringComparison.Ordinal));
                    if (removed == 0)
                    {
                        AddRuleNotFound(diagnostics, remove.RuleRef, definition.Id);
                        return definition;
                    }

                    return definition.WithRules(rules);
                }

            case PatchRuleOverride patch:
                {
                    var rules = definition.Rules.ToList();
                int ruleIndex = rules.FindIndex(r => string.Equals(r.Id, patch.RuleRef, StringComparison.Ordinal));
                    if (ruleIndex < 0)
                    {
                        AddRuleNotFound(diagnostics, patch.RuleRef, definition.Id);
                        return definition;
                    }

                    rules[ruleIndex] = ApplyPatch(rules[ruleIndex], patch);
                    return definition.WithRules(rules);
                }

            case AddRuleOverride add:
                {
                    var rules = definition.Rules.ToList();
                    rules.Add(add.Rule);
                    return definition.WithRules(rules);
                }

            default:
                return definition;
        }
    }

    /// <summary>
    /// Produces a copy of a rule with the patch's non-null fields applied.
    /// </summary>
    /// <param name="rule">The rule to patch.</param>
    /// <param name="patch">The patch operation.</param>
    /// <returns>A new <see cref="NotableDateRule" /> with the patched fields.</returns>
    private static NotableDateRule ApplyPatch(NotableDateRule rule, PatchRuleOverride patch) =>
        new(
            rule.Id,
            patch.Priority ?? rule.Priority,
            patch.Category ?? rule.Category,
            patch.NonWorking ?? rule.NonWorking,
            patch.DurationDays ?? rule.DurationDays,
            patch.Applicability ?? rule.Applicability,
            patch.Strategy ?? rule.Strategy,
            patch.AdjustmentPolicyRefs ?? rule.AdjustmentPolicyRefs,
            patch.Tags ?? rule.Tags);

    /// <summary>
    /// Records a diagnostic for an override that targets a non-existent rule.
    /// </summary>
    /// <param name="diagnostics">The collection that receives the diagnostic.</param>
    /// <param name="ruleRef">The targeted rule identifier.</param>
    /// <param name="notableDateId">The owning concept identifier.</param>
    private static void AddRuleNotFound(ICollection<NotableDateValidationDiagnostic> diagnostics, string ruleRef, string notableDateId) =>
        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-OVERRIDE-RULE",
            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_OverrideRuleNotFound, ruleRef, notableDateId)));
}
