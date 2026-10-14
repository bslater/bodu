// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Performs the pre-resolution semantic validation that the XSD cannot express: duplicate identities, unresolved
/// adjustment and strategy references, and inverted year bounds; and, for a resource no reader produced, the identifier
/// patterns and numeric ranges the XSD does express.
/// </summary>
internal static class NotableDateRuleValidator
{
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
    /// Reports each identifier, reference and territory code of an assembled resource that breaks the pattern the
    /// notable-date schema requires of it, with the <c>BODU-CAL-SCHEMA</c> error the JSON reader reports for the same
    /// value.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives validation diagnostics.</param>
    /// <remarks>
    /// The loader checks these values as it reads a document, so <see cref="Validate" /> does not repeat the check; a
    /// resource assembled in code, which no reader produced, is held to them before it is written as a binary rule
    /// pack. A strategy's references need no check here, since the strategy's constructor refuses one that breaks the
    /// pattern. Each diagnostic names the JSON property the value is written under, as the JSON reader names it.
    /// </remarks>
    public static void ValidateIdentifiers(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        CheckPattern(resource.ResourceId, NotableDateSchemaPattern.ResourceIdentifier, "resourceId", diagnostics);

        foreach (AdjustmentPolicy policy in resource.AdjustmentPolicies)
        {
            CheckPattern(policy.Id, NotableDateSchemaPattern.Identifier, "id", diagnostics);
            CheckPatterns(policy.Scope.Territories, NotableDateSchemaPattern.TerritoryCode, "territories", diagnostics);
            CheckPatterns(policy.Scope.NotableDateRefs, NotableDateSchemaPattern.Identifier, "notableDateRefs", diagnostics);

            foreach (string scoped in policy.Scope.RuleRefs)
            {
                (string? qualifier, string name) = AdjustmentScope.SplitRuleRef(scoped);
                if (qualifier is not null)
                    CheckPattern(qualifier, NotableDateSchemaPattern.Identifier, "ruleRefs", diagnostics);

                CheckPattern(name, NotableDateSchemaPattern.Identifier, "ruleRefs", diagnostics);
            }

            if (policy.ActionNotableDateRef is not null)
                CheckPattern(policy.ActionNotableDateRef, NotableDateSchemaPattern.Identifier, "notableDateRef", diagnostics);

            if (policy.ActionRuleRef is not null)
                CheckPattern(policy.ActionRuleRef, NotableDateSchemaPattern.Identifier, "ruleRef", diagnostics);
        }

        foreach (NotableDateDefinition definition in resource.NotableDates)
        {
            CheckPattern(definition.Id, NotableDateSchemaPattern.Identifier, "id", diagnostics);

            foreach (NotableDateRule rule in definition.Rules)
            {
                CheckPattern(rule.Id, NotableDateSchemaPattern.Identifier, "id", diagnostics);
                CheckPatterns(rule.Applicability.Territories, NotableDateSchemaPattern.TerritoryCode, "territories", diagnostics);
                CheckPatterns(rule.AdjustmentPolicyRefs, NotableDateSchemaPattern.Identifier, "adjustments", diagnostics);
            }
        }
    }

    /// <summary>
    /// Reports each number of an assembled resource that lies outside the range the notable-date schema holds its field
    /// to, with the <c>BODU-CAL-SCHEMA</c> error the JSON reader reports for the same value, and an adjustment
    /// trigger's month outside 1 to 12 with the reader's <c>BODU-CAL-VALUE</c> error.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="diagnostics">The collection that receives validation diagnostics.</param>
    /// <remarks>
    /// The loader checks these values as it reads a document, so <see cref="Validate" /> does not repeat the check; a
    /// resource assembled in code, which no reader produced, is held to them before it is written as a binary rule
    /// pack. The bounds are those of <see cref="NotableDateSchemaRanges" />, which the readers apply. A strategy's or
    /// recurrence's numbers need no check here, since its constructor refuses one outside the range. Each diagnostic
    /// names the JSON property the value is written under, as the JSON reader names it.
    /// </remarks>
    public static void ValidateRanges(NotableDateResource resource, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        foreach (AdjustmentPolicy policy in resource.AdjustmentPolicies)
        {
            CheckRange(policy.Scope.FromYear, NotableDateSchemaRange.UnsignedShort, "fromYear", diagnostics);
            CheckRange(policy.Scope.ToYear, NotableDateSchemaRange.UnsignedShort, "toYear", diagnostics);
            CheckRanges(policy.Scope.OnlyYears, NotableDateSchemaRange.UnsignedShort, "onlyYears", diagnostics);
            CheckRanges(policy.Scope.ExceptYears, NotableDateSchemaRange.UnsignedShort, "exceptYears", diagnostics);
            CheckRange(policy.MaxSearchDays, NotableDateSchemaRange.PositiveInteger, "maxSearchDays", diagnostics);
            CheckRange(policy.TriggerDay, NotableDateSchemaRange.DayValue, "day", diagnostics);

            if (policy.TriggerMonth is int month && month is < 1 or > 12)
                NotableDateDocumentParseHelpers.AddValueError(diagnostics, CalendarResourceStrings.Validation_InvalidTriggerMonth, "trigger month", month.ToString(CultureInfo.InvariantCulture));
        }

        foreach (NotableDateDefinition definition in resource.NotableDates)
        {
            CheckRange(definition.DefaultDurationDays, NotableDateSchemaRange.PositiveInteger, "defaultDurationDays", diagnostics);

            foreach (NotableDateRule rule in definition.Rules)
            {
                RuleApplicability applicability = rule.Applicability;
                CheckRange(applicability.FromYear, NotableDateSchemaRange.UnsignedShort, "fromYear", diagnostics);
                CheckRange(applicability.ToYear, NotableDateSchemaRange.UnsignedShort, "toYear", diagnostics);
                CheckRanges(applicability.OnlyYears, NotableDateSchemaRange.UnsignedShort, "onlyYears", diagnostics);
                CheckRanges(applicability.ExceptYears, NotableDateSchemaRange.UnsignedShort, "exceptYears", diagnostics);
                CheckRange(applicability.EveryYears, NotableDateSchemaRange.PositiveInteger, "everyYears", diagnostics);
                CheckRange(applicability.AnchorYear, NotableDateSchemaRange.UnsignedShort, "anchorYear", diagnostics);

                if (rule.Duration is FixedDurationDefinition duration)
                    CheckRange(duration.Days, NotableDateSchemaRange.PositiveInteger, "durationDays", diagnostics);
            }
        }
    }

    /// <summary>
    /// Records the <c>BODU-CAL-SCHEMA</c> error for a number outside the range the schema holds its field to.
    /// </summary>
    /// <param name="value">The value, or <see langword="null" /> when the field is absent.</param>
    /// <param name="range">The range the schema holds the value to.</param>
    /// <param name="field">The name of the JSON property the value is written under, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic.</param>
    private static void CheckRange(int? value, NotableDateSchemaRange range, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is int number && NotableDateSchemaRanges.Check(number, range, field) is NotableDateValidationDiagnostic diagnostic)
            diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Records the <c>BODU-CAL-SCHEMA</c> error for each number of a list outside the range the schema holds its items
    /// to.
    /// </summary>
    /// <param name="values">The values.</param>
    /// <param name="range">The range the schema holds each value to.</param>
    /// <param name="field">The name of the JSON property the list is written under, used in the diagnostics.</param>
    /// <param name="diagnostics">The collection that receives the diagnostics.</param>
    private static void CheckRanges(IEnumerable<int> values, NotableDateSchemaRange range, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        foreach (int value in values)
            CheckRange(value, range, field, diagnostics);
    }

    /// <summary>
    /// Records the <c>BODU-CAL-SCHEMA</c> error for a value that breaks the pattern the schema requires of it.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="pattern">The pattern the schema requires of the value.</param>
    /// <param name="field">The name of the JSON property the value is written under, used in the diagnostic.</param>
    /// <param name="diagnostics">The collection that receives the diagnostic.</param>
    private static void CheckPattern(string value, NotableDateSchemaPattern pattern, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (NotableDateSchemaPatterns.Check(value, pattern, field) is NotableDateValidationDiagnostic diagnostic)
            diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Records the <c>BODU-CAL-SCHEMA</c> error for each value of a list that breaks the pattern the schema requires of
    /// it.
    /// </summary>
    /// <param name="values">The values.</param>
    /// <param name="pattern">The pattern the schema requires of each value.</param>
    /// <param name="field">The name of the JSON property the list is written under, used in the diagnostics.</param>
    /// <param name="diagnostics">The collection that receives the diagnostics.</param>
    private static void CheckPatterns(IEnumerable<string> values, NotableDateSchemaPattern pattern, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        foreach (string value in values)
            CheckPattern(value, pattern, field, diagnostics);
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
    /// Reports duplicate rule identifiers, unresolved adjustment and strategy references, and inverted year bounds
    /// within a single concept.
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
        if (rule.Strategy is IDateCalculationStrategy strategy)
            ValidateStrategy(resource, definition, rule, strategy, isEndStrategy: false, diagnostics, algorithms);

        if (rule.Duration is CalculatedEndDateDurationDefinition calculated)
            ValidateStrategy(resource, definition, rule, calculated.EndStrategy, isEndStrategy: true, diagnostics, algorithms);
    }

    /// <summary>
    /// Reports unresolved, ambiguous, or recurring references and unrecognized algorithm keys for a single strategy,
    /// selecting primary or duration-end-strategy diagnostic codes.
    /// </summary>
    /// <param name="resource">The resource being validated.</param>
    /// <param name="definition">The owning concept.</param>
    /// <param name="rule">The rule to validate.</param>
    /// <param name="strategy">The strategy to validate.</param>
    /// <param name="isEndStrategy">Whether the strategy is a calculated duration's end strategy.</param>
    /// <param name="diagnostics">The collection that receives diagnostics.</param>
    /// <param name="algorithms">
    /// The custom algorithm registry whose keys are accepted, or <see langword="null" />.
    /// </param>
    private static void ValidateStrategy(
        NotableDateResource resource,
        NotableDateDefinition definition,
        NotableDateRule rule,
        IDateCalculationStrategy strategy,
        bool isEndStrategy,
        ICollection<NotableDateValidationDiagnostic> diagnostics,
        INotableDateAlgorithmRegistry? algorithms)
    {
        if (strategy is AlgorithmDateStrategy algorithm && !AlgorithmDateStrategy.IsKnownKey(algorithm.Key) && !(algorithms?.Contains(algorithm.Key) ?? false))
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                isEndStrategy ? "BODU-CAL-DURATION-ALGORITHM" : "BODU-CAL-ALGORITHM",
                string.Format(CultureInfo.CurrentCulture, isEndStrategy ? CalendarResourceStrings.Validation_DurationUnknownAlgorithm : CalendarResourceStrings.Validation_UnknownAlgorithm, definition.Id, rule.Id, algorithm.Key)));
        }

        if (ReferenceOf(strategy) is not { } referencePair)
            return;

        (string notableDateRef, string? ruleRef) = referencePair;
        string reference = string.IsNullOrEmpty(ruleRef) ? notableDateRef : $"{notableDateRef}/{ruleRef}";
        int matches = CountReferenceMatches(resource, notableDateRef, ruleRef);

        if (matches == 0)
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                isEndStrategy ? "BODU-CAL-DURATION-OFFSET-MISSING" : "BODU-CAL-OFFSET-MISSING",
                string.Format(CultureInfo.CurrentCulture, isEndStrategy ? CalendarResourceStrings.Validation_DurationOffsetReferenceNotFound : CalendarResourceStrings.Validation_OffsetReferenceNotFound, definition.Id, rule.Id, reference)));
        }
        else if (matches > 1)
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                isEndStrategy ? "BODU-CAL-DURATION-OFFSET-AMBIGUOUS" : "BODU-CAL-OFFSET-AMBIGUOUS",
                string.Format(CultureInfo.CurrentCulture, isEndStrategy ? CalendarResourceStrings.Validation_DurationOffsetReferenceAmbiguous : CalendarResourceStrings.Validation_OffsetReferenceAmbiguous, definition.Id, rule.Id, reference)));
        }
        else if (ReferencedRuleIsRecurring(resource, notableDateRef, ruleRef))
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-REF-RECURRING",
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_ReferenceIsRecurring, definition.Id, rule.Id, reference)));
        }
    }

    /// <summary>
    /// Extracts the notable-date and rule references of a referential strategy.
    /// </summary>
    /// <param name="strategy">The strategy to inspect.</param>
    /// <returns>The reference pair, or <see langword="null" /> when the strategy is not referential.</returns>
    private static (string NotableDateRef, string? RuleRef)? ReferenceOf(IDateCalculationStrategy strategy) =>
        strategy switch
        {
            OffsetFromRuleStrategy s => (s.NotableDateRef, s.RuleRef),
            WeekdayNearRuleStrategy s => (s.NotableDateRef, s.RuleRef),
            NthWeekdayFromRuleStrategy s => (s.NotableDateRef, s.RuleRef),
            WorkingDayOffsetFromRuleStrategy s => (s.NotableDateRef, s.RuleRef),
            _ => null,
        };

    /// <summary>
    /// Determines whether a reference resolves to a rule whose occurrence source is a recurrence.
    /// </summary>
    /// <param name="resource">The resource being validated.</param>
    /// <param name="notableDateRef">The referenced concept id.</param>
    /// <param name="ruleRef">The referenced rule id, or <see langword="null" /> for the sole rule.</param>
    /// <returns>
    /// <see langword="true" /> when the reference resolves to a recurrence rule; otherwise <see langword="false" />.
    /// </returns>
    private static bool ReferencedRuleIsRecurring(NotableDateResource resource, string notableDateRef, string? ruleRef)
    {
        NotableDateDefinition? target = resource.FindDefinition(notableDateRef);
        if (target is null)
            return false;

        if (string.IsNullOrEmpty(ruleRef))
            return target.Rules.Count == 1 && target.Rules[0].Recurrence is not null;

        NotableDateRule? referenced = target.Rules.FirstOrDefault(r => string.Equals(r.Id, ruleRef, StringComparison.Ordinal));
        return referenced?.Recurrence is not null;
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
}
