// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CookbookValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Performs the pre-resolution semantic validation that the XSD cannot express: duplicate identities, unresolved
/// adjustment references, inverted year bounds, and impossible fixed dates.
/// </summary>
internal static class CookbookValidator
{
    /// <summary>
    /// The maximum day-of-month for each month, indexed so that January is at index zero and February allows 29.
    /// </summary>
    private static readonly int[] s_maxDaysPerMonth = { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    /// <summary>
    /// Validates the assembled resource, recording an error diagnostic for each violation.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives validation diagnostics.</param>
    public static void Validate(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        ValidateAdjustmentPolicyIds(resource, diagnostics);
        ValidateNotableDates(resource, diagnostics);
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
                    "BODU-CAL2-DUP-POLICY",
                    string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Validation_DuplicateAdjustmentPolicyId, policy.Id)));
            }
        }
    }

    /// <summary>
    /// Reports duplicate concept identifiers and validates each concept's rules.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    private static void ValidateNotableDates(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        HashSet<string> knownPolicies = new(resource.AdjustmentPolicies.Select(p => p.Id), StringComparer.Ordinal);
        HashSet<string> seenConcepts = new(StringComparer.Ordinal);

        foreach (NotableDateDefinition definition in resource.NotableDates)
        {
            if (!seenConcepts.Add(definition.Id))
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL2-DUP-ND",
                    string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Validation_DuplicateNotableDateId, definition.Id)));
            }

            ValidateRules(definition, knownPolicies, diagnostics);
        }
    }

    /// <summary>
    /// Reports duplicate rule identifiers, unresolved adjustment references, inverted year bounds, and impossible fixed
    /// dates within a single concept.
    /// </summary>
    /// <param name="definition">The concept to validate.</param>
    /// <param name="knownPolicies">The set of declared adjustment-policy identifiers.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    private static void ValidateRules(
        NotableDateDefinition definition,
        HashSet<string> knownPolicies,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        HashSet<string> seenRules = new(StringComparer.Ordinal);

        foreach (NotableDateRule rule in definition.Rules)
        {
            if (!seenRules.Add(rule.Id))
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL2-DUP-RULE",
                    string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Validation_DuplicateRuleId, rule.Id, definition.Id)));
            }

            foreach (string policyRef in rule.AdjustmentPolicyRefs)
            {
                if (!knownPolicies.Contains(policyRef))
                {
                    diagnostics.Add(new NotableDateValidationDiagnostic(
                        NotableDateValidationSeverity.Error,
                        "BODU-CAL2-ADJREF",
                        string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Validation_UnresolvedAdjustmentPolicy, rule.Id, policyRef)));
                }
            }

            ValidateYearBounds(definition, rule, diagnostics);
            ValidateFixedDate(definition, rule, diagnostics);
        }
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
                "BODU-CAL2-YEARS",
                string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Validation_FromYearAfterToYear, definition.Id, rule.Id, from, to)));
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

        if (strategy.Month is < 1 or > 12)
            return;

        if (strategy.Day > s_maxDaysPerMonth[strategy.Month - 1])
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL2-DAY",
                string.Format(
                    CultureInfo.InvariantCulture,
                    Calendar2ResourceStrings.Validation_InvalidDayValue,
                    definition.Id,
                    rule.Id,
                    strategy.Day,
                    strategy.Month)));
        }
    }
}
