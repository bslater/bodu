// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Performs the pre-resolution semantic validation that the XSD cannot express: duplicate identities, unresolved
/// adjustment references, inverted year bounds, and impossible fixed dates.
/// </summary>
internal static class NotableDateRuleValidator
{
    /// <summary>
    /// The maximum day-of-month for each month, indexed so that January is at index zero and February allows 29.
    /// </summary>
    private static readonly int[] s_maxDaysPerMonth = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    /// <summary>
    /// Validates the assembled resource, recording an error diagnostic for each violation.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives validation diagnostics.</param>
    /// <param name="algorithms">
    /// The custom algorithm registry whose keys are accepted, or <see langword="null" />.
    /// </param>
    public static void Validate(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics, INotableDateAlgorithmRegistry? algorithms = null)
    {
        ValidateAdjustmentPolicyIds(resource, diagnostics);
        ValidateAdjustmentPolicyScopes(resource, diagnostics);
        ValidateAdjustmentActions(resource, diagnostics);
        ValidateAdjustmentTriggers(resource, diagnostics);
        ValidateNotableDates(resource, diagnostics, algorithms);
    }

    /// <summary>
    /// Reports adjustment policies whose scope declares a lower year bound after its upper year bound.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    private static void ValidateAdjustmentPolicyScopes(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        foreach (AdjustmentPolicy policy in resource.AdjustmentPolicies)
        {
            if (policy.Scope.FromYear is int from && policy.Scope.ToYear is int to && from > to)
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL-YEARS",
                    string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_AdjustmentScopeFromYearAfterToYear, policy.Id, from, to)));
            }
        }
    }

    /// <summary>
    /// Reports duplicate adjustment-policy identifiers.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    private static void ValidateAdjustmentPolicyIds(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (AdjustmentPolicy policy in resource.AdjustmentPolicies)
        {
            if (!seen.Add(policy.Id))
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL-DUP-POLICY",
                    string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_DuplicateAdjustmentPolicyId, policy.Id)));
            }
        }
    }

    /// <summary>
    /// Reports adjustment policies whose reference or custom actions are missing required targets or resolve
    /// ambiguously.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    private static void ValidateAdjustmentActions(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        foreach (AdjustmentPolicy policy in resource.AdjustmentPolicies)
        {
            switch (policy.Action)
            {
                case AdjustmentAction.ReplaceWithRule:
                    ValidateReplaceWithRule(resource, policy, diagnostics);
                    break;

                case AdjustmentAction.Custom when string.IsNullOrEmpty(policy.ActionHandlerKey):
                    diagnostics.Add(new NotableDateValidationDiagnostic(
                        NotableDateValidationSeverity.Error,
                        "BODU-CAL-HANDLER-MISSING",
                        string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_CustomHandlerKeyMissing, policy.Id)));
                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Reports adjustment policies whose <see cref="AdjustmentTrigger.Custom" /> trigger is missing its required
    /// handler key.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    private static void ValidateAdjustmentTriggers(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        foreach (AdjustmentPolicy policy in resource.AdjustmentPolicies)
        {
            if (policy.Trigger == AdjustmentTrigger.Custom && string.IsNullOrEmpty(policy.TriggerHandlerKey))
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL-TRIGGER-HANDLER-MISSING",
                    string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_CustomTriggerHandlerKeyMissing, policy.Id)));
            }
        }
    }

    /// <summary>
    /// Reports a <see cref="AdjustmentAction.ReplaceWithRule" /> action whose reference is missing, unresolved, or
    /// ambiguous.
    /// </summary>
    /// <param name="resource">The resource being validated.</param>
    /// <param name="policy">The policy to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    private static void ValidateReplaceWithRule(NotableDateResource resource, AdjustmentPolicy policy, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        string? notableDateRef = policy.ActionNotableDateRef;
        if (string.IsNullOrEmpty(notableDateRef))
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-REPLACE-MISSING",
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_ReplaceReferenceMissing, policy.Id)));
            return;
        }

        string reference = string.IsNullOrEmpty(policy.ActionRuleRef)
            ? notableDateRef
            : $"{notableDateRef}/{policy.ActionRuleRef}";

        int matches = CountReferenceMatches(resource, notableDateRef, policy.ActionRuleRef);
        if (matches == 0)
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-REPLACE-MISSING",
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_ReplaceReferenceNotFound, policy.Id, reference)));
        }
        else if (matches > 1)
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-REPLACE-AMBIGUOUS",
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_ReplaceReferenceAmbiguous, policy.Id, reference)));
        }
    }

    /// <summary>
    /// Reports duplicate concept identifiers and validates each concept's rules.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    /// <param name="algorithms">
    /// The custom algorithm registry whose keys are accepted, or <see langword="null" />.
    /// </param>
    private static void ValidateNotableDates(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics, INotableDateAlgorithmRegistry? algorithms)
    {
        HashSet<string> knownPolicies = new(resource.AdjustmentPolicies.Select(p => p.Id), StringComparer.Ordinal);
        HashSet<string> seenConcepts = new(StringComparer.Ordinal);

        foreach (NotableDateDefinition definition in resource.NotableDates)
        {
            if (!seenConcepts.Add(definition.Id))
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL-DUP-ND",
                    string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_DuplicateNotableDateId, definition.Id)));
            }

            ValidateRules(resource, definition, knownPolicies, diagnostics, algorithms);
        }
    }

    /// <summary>
    /// Reports duplicate rule identifiers, unresolved adjustment references, inverted year bounds, and impossible fixed
    /// dates within a single concept.
    /// </summary>
    /// <param name="resource">The resource being validated, used to resolve offset references.</param>
    /// <param name="definition">The concept to validate.</param>
    /// <param name="knownPolicies">The set of declared adjustment-policy identifiers.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    /// <param name="algorithms">
    /// The custom algorithm registry whose keys are accepted, or <see langword="null" />.
    /// </param>
    private static void ValidateRules(
        NotableDateResource resource,
        NotableDateDefinition definition,
        HashSet<string> knownPolicies,
        ICollection<NotableDateValidationDiagnostic> diagnostics,
        INotableDateAlgorithmRegistry? algorithms)
    {
        HashSet<string> seenRules = new(StringComparer.Ordinal);

        foreach (NotableDateRule rule in definition.Rules)
        {
            if (!seenRules.Add(rule.Id))
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL-DUP-RULE",
                    string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_DuplicateRuleId, rule.Id, definition.Id)));
            }

            foreach (string policyRef in rule.AdjustmentPolicyRefs)
            {
                if (!knownPolicies.Contains(policyRef))
                {
                    diagnostics.Add(new NotableDateValidationDiagnostic(
                        NotableDateValidationSeverity.Error,
                        "BODU-CAL-ADJREF",
                        string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_UnresolvedAdjustmentPolicy, rule.Id, policyRef)));
                }
            }

            ValidateYearBounds(definition, rule, diagnostics);
            ValidateFixedDate(definition, rule, diagnostics);
            ValidateStrategyReferences(resource, definition, rule, diagnostics, algorithms);
        }
    }

    /// <summary>
    /// Reports unresolved or ambiguous offset references and unrecognized algorithm keys.
    /// </summary>
    /// <param name="resource">The resource being validated.</param>
    /// <param name="definition">The owning concept.</param>
    /// <param name="rule">The rule to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    /// <param name="algorithms">
    /// The custom algorithm registry whose keys are accepted, or <see langword="null" />.
    /// </param>
    private static void ValidateStrategyReferences(
        NotableDateResource resource,
        NotableDateDefinition definition,
        NotableDateRule rule,
        ICollection<NotableDateValidationDiagnostic> diagnostics,
        INotableDateAlgorithmRegistry? algorithms)
    {
        switch (rule.Strategy)
        {
            case OffsetFromRuleStrategy offset:
                {
                string reference = string.IsNullOrEmpty(offset.RuleRef)
                        ? offset.NotableDateRef
                        : $"{offset.NotableDateRef}/{offset.RuleRef}";

                int matches = CountReferenceMatches(resource, offset.NotableDateRef, offset.RuleRef);
                    if (matches == 0)
                    {
                        diagnostics.Add(new NotableDateValidationDiagnostic(
                            NotableDateValidationSeverity.Error,
                            "BODU-CAL-OFFSET-MISSING",
                            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_OffsetReferenceNotFound, definition.Id, rule.Id, reference)));
                    }
                    else if (matches > 1)
                    {
                        diagnostics.Add(new NotableDateValidationDiagnostic(
                            NotableDateValidationSeverity.Error,
                            "BODU-CAL-OFFSET-AMBIGUOUS",
                            string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_OffsetReferenceAmbiguous, definition.Id, rule.Id, reference)));
                    }

                    break;
                }

            case AlgorithmDateStrategy algorithm when !AlgorithmDateStrategy.IsKnownKey(algorithm.Key) && !(algorithms?.Contains(algorithm.Key) ?? false):
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL-ALGORITHM",
                    string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_UnknownAlgorithm, definition.Id, rule.Id, algorithm.Key)));
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Counts the rules an offset reference resolves to within the resource.
    /// </summary>
    /// <param name="resource">The resource being validated.</param>
    /// <param name="notableDateRef">The referenced concept identifier.</param>
    /// <param name="ruleRef">The referenced rule identifier, or <see langword="null" /> for the sole rule.</param>
    /// <returns>The number of matching rules: 0 when missing, 1 when unambiguous, more than 1 when ambiguous.</returns>
    private static int CountReferenceMatches(NotableDateResource resource, string notableDateRef, string? ruleRef)
    {
        NotableDateDefinition? target = null;
        foreach (NotableDateDefinition candidate in resource.NotableDates)
        {
            if (string.Equals(candidate.Id, notableDateRef, StringComparison.Ordinal))
            {
                target = candidate;
                break;
            }
        }

        if (target is null)
            return 0;

        if (string.IsNullOrEmpty(ruleRef))
            return target.Rules.Count;

        return target.Rules.Count(r => string.Equals(r.Id, ruleRef, StringComparison.Ordinal));
    }

    /// <summary>
    /// Reports an inverted year range on a rule's applicability.
    /// </summary>
    /// <param name="definition">The owning concept.</param>
    /// <param name="rule">The rule to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    private static void ValidateYearBounds(
        NotableDateDefinition definition,
        NotableDateRule rule,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (rule.Applicability.FromYear is int from && rule.Applicability.ToYear is int to && from > to)
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-YEARS",
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_FromYearAfterToYear, definition.Id, rule.Id, from, to)));
        }
    }

    /// <summary>
    /// Reports a fixed-date rule whose day can never exist in its month.
    /// </summary>
    /// <param name="definition">The owning concept.</param>
    /// <param name="rule">The rule to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    private static void ValidateFixedDate(
        NotableDateDefinition definition,
        NotableDateRule rule,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (rule.Strategy is not FixedDateStrategy strategy)
            return;

        // Non-Gregorian month/day ranges vary by calendar (and a Hebrew alias defers the month), so the
        // proleptic-Gregorian day-in-month check does not apply.
        if (strategy.Calendar != CalendarSystem.Gregorian)
            return;

        if (strategy.Month is < 1 or > 12)
            return;

        if (strategy.Day > s_maxDaysPerMonth[strategy.Month - 1])
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-DAY",
                string.Format(
                    CultureInfo.CurrentCulture,
                    CalendarResourceStrings.Validation_InvalidDayValue,
                    definition.Id,
                    rule.Id,
                    strategy.Day,
                    strategy.Month)));
        }
    }
}
