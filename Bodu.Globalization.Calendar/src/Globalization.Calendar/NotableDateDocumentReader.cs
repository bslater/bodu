// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using static Bodu.Globalization.Calendar.NotableDateDocumentParseHelpers;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Shapes a parsed notable-date document from an <see cref="IDocumentNode" /> tree, so the XML and JSON parsers share a
/// single schema-binding implementation. Format-specific concerns - XML schema validation and the raw parse - stay with
/// the two public parsers; everything structural lives here.
/// </summary>
internal static class NotableDateDocumentReader
{
    /// <summary>The node names of the calculation-strategy kinds a <c>Strategy</c> node may hold.</summary>
    private static readonly string[] s_strategyKinds =
    [
        "fixed", "dayOfWeekInMonth", "weekdayNearDate", "relativeWeekdayInMonth", "offsetFromRule", "algorithm",
        "weekdayNearRule", "nthWeekdayFromRule", "workingDayOffsetFromRule", "workingDayInMonth", "ordinalDayOfMonth",
        "isoWeekDate", "dayOfYear",
    ];

    /// <summary>The node names of the recurrence kinds a <c>Recurrence</c> node may hold.</summary>
    private static readonly string[] s_recurrenceKinds = ["dailyInterval", "weekly", "monthlyDay", "monthlyWeekday"];

    /// <summary>
    /// Reads a document from its root node.
    /// </summary>
    /// <param name="root">The root document node.</param>
    /// <param name="diagnostics">The collection that receives structural and semantic diagnostics.</param>
    /// <returns>The parsed <see cref="ParsedNotableDateDocument" />.</returns>
    public static ParsedNotableDateDocument Read(IDocumentNode root, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        return new ParsedNotableDateDocument(
            ReadPatterned(root, "resourceId", NotableDateSchemaPattern.ResourceIdentifier, diagnostics) ?? string.Empty,
            root.Scalar("schemaVersion") ?? string.Empty,
            ReadResolutionPolicy(root.Child("resolutionPolicy"), diagnostics),
            ReadAdjustmentPolicies(root, diagnostics),
            ReadNotableDates(root, diagnostics),
            ReadOverrides(root, diagnostics),
            ReadImports(root, diagnostics));
    }

    /// <summary>
    /// Reads the resource-level resolution policy, falling back to the recommended defaults when absent.
    /// </summary>
    /// <param name="node">The resolution-policy node, or <see langword="null" />.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="ResolutionPolicy" />.</returns>
    private static ResolutionPolicy ReadResolutionPolicy(IDocumentNode? node, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (node is null)
            return ResolutionPolicy.Default;

        IReadOnlyList<string>? precedence = node.OptionalScalarList("CategoryPrecedence", "Category", "value", "categoryPrecedence");

        return new ResolutionPolicy(
            ParseEnum(node.Scalar("duplicatePolicy"), DuplicatePolicy.Error, "duplicatePolicy", diagnostics),
            ParseEnum(node.Scalar("sameDayCollisionPolicy"), CollisionPolicy.KeepAll, "sameDayCollisionPolicy", diagnostics),
            ParseEnum(node.Scalar("spanCollisionPolicy"), CollisionPolicy.KeepAll, "spanCollisionPolicy", diagnostics),
            ParseEnum(node.Scalar("priorityDirection"), PriorityDirection.HigherWins, "priorityDirection", diagnostics),
            ParseEnum(node.Scalar("observedDateRangePolicy"), ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion, "observedDateRangePolicy", diagnostics),
            ParseWorkingWeek(node.Scalar("workingDays"), diagnostics),
            precedence is null ? null : ParseEnumList(precedence, NotableDateCategory.None, "categoryPrecedence", diagnostics));
    }

    /// <summary>
    /// Reads the reusable adjustment policies declared by the resource.
    /// </summary>
    /// <param name="root">The root document node.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed adjustment policies.</returns>
    private static List<AdjustmentPolicy> ReadAdjustmentPolicies(IDocumentNode root, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<AdjustmentPolicy> policies = new();

        foreach (IDocumentNode policy in root.List("AdjustmentPolicies", "AdjustmentPolicy", "adjustmentPolicies"))
        {
            IDocumentNode? trigger = policy.Child("trigger");
            IDocumentNode? action = policy.Child("action");
            IDocumentNode? emission = policy.Child("emission");

            IEnumerable<DayOfWeek> weekdays = trigger is null
                ? []
                : ParseEnumList(trigger.ScalarList(null, "Weekday", "value", "weekdays"), DayOfWeek.Monday, "trigger weekdays", diagnostics);

            policies.Add(new AdjustmentPolicy(
                ReadPatterned(policy, "id", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty,
                ParseInt(policy.Scalar("priority"), 0, "adjustment policy priority", diagnostics),
                ReadScope(policy.Child("scope"), diagnostics),
                ParseEnum(trigger?.Scalar("type"), AdjustmentTrigger.Always, "trigger type", diagnostics),
                weekdays,
                ParseEnum(action?.Scalar("type"), AdjustmentAction.None, "action type", diagnostics),
                ParseNullableEnum<DayOfWeek>(action?.Scalar("dayOfWeek"), "action dayOfWeek", diagnostics),
                ParseInt(action?.Scalar("days"), 0, "action days", diagnostics),
                ParseNullableInt(action?.Scalar("maxSearchDays"), "action maxSearchDays", diagnostics),
                ParseBool(action?.Scalar("skipWeekends"), true, "action skipWeekends", diagnostics),
                ParseBool(action?.Scalar("skipNonWorkingDates"), false, "action skipNonWorkingDates", diagnostics),
                ParseEnum(emission?.Scalar("mode"), EmissionMode.ActualOnly, "emission mode", diagnostics),
                emission?.Scalar("reason"),
                ParseNullableBool(emission?.Scalar("nonWorking"), "emission nonWorking", diagnostics),
                ParseTriggerMonth(trigger?.Scalar("month"), "trigger month", diagnostics),
                ParseNullableInt(trigger?.Scalar("day"), "trigger day", diagnostics),
                ParseNullableEnum<WeekOrdinal>(trigger?.Scalar("weekOrdinal"), "trigger weekOrdinal", diagnostics),
                ReadPatterned(action, "notableDateRef", NotableDateSchemaPattern.Identifier, diagnostics),
                ReadPatterned(action, "ruleRef", NotableDateSchemaPattern.Identifier, diagnostics),
                action?.Scalar("handlerKey"),
                trigger?.Scalar("handlerKey"),
                ReadHandlerParameters(policy)));
        }

        return policies;
    }

    /// <summary>
    /// Reads the custom-handler parameter map declared by a policy.
    /// </summary>
    /// <param name="policy">The adjustment-policy node.</param>
    /// <returns>The parameter map, or <see langword="null" /> when none are declared.</returns>
    private static Dictionary<string, string>? ReadHandlerParameters(IDocumentNode policy)
    {
        Dictionary<string, string> parameters = new(StringComparer.Ordinal);
        foreach ((string? key, string? value) in policy.KeyValueList("Parameters", "Param", "key", "value", "parameters"))
        {
            if (key.Length > 0)
                parameters[key] = value;
        }

        return parameters.Count > 0 ? parameters : null;
    }

    /// <summary>
    /// Reads an adjustment policy scope, falling back to a global scope when absent.
    /// </summary>
    /// <param name="node">The scope node, or <see langword="null" />.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="AdjustmentScope" />.</returns>
    private static AdjustmentScope ReadScope(IDocumentNode? node, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (node is null)
            return AdjustmentScope.Global;

        IReadOnlyList<(string? Qualifier, string Name)> ruleRefs = node.QualifiedReferenceList(null, "Rule", "notableDateRef", "ruleRef", "ruleRefs", "scope ruleRefs", diagnostics);
        foreach ((string? qualifier, string name) in ruleRefs)
        {
            node.CheckPattern(qualifier, NotableDateSchemaPattern.Identifier, "ruleRefs", diagnostics);
            node.CheckPattern(name, NotableDateSchemaPattern.Identifier, "ruleRefs", diagnostics);
        }

        return new AdjustmentScope(
            CheckPatterns(node, node.TextList(null, "Territory", "code", "territories", "scope territories", diagnostics), NotableDateSchemaPattern.TerritoryCode, "territories", diagnostics),
            ParseEnumList(node.ScalarList(null, "Calendar", "name", "calendars"), CalendarSystem.Gregorian, "scope calendars", diagnostics),
            ParseEnumList(node.ScalarList(null, "Category", "value", "categories"), NotableDateCategory.None, "scope categories", diagnostics),
            CheckPatterns(node, node.TextList(null, "NotableDate", "ref", "notableDateRefs", "scope notableDateRefs", diagnostics), NotableDateSchemaPattern.Identifier, "notableDateRefs", diagnostics),
            ruleRefs.Select(reference => AdjustmentScope.QualifyRuleRef(reference.Qualifier, reference.Name)),
            ParseNullableInt(node.Scalar("fromYear"), "scope fromYear", diagnostics),
            ParseNullableInt(node.Scalar("toYear"), "scope toYear", diagnostics),
            node.IntList(null, "OnlyYear", "value", "onlyYears", "scope onlyYears", diagnostics),
            node.IntList(null, "ExceptYear", "value", "exceptYears", "scope exceptYears", diagnostics));
    }

    /// <summary>
    /// Reads the notable-date concepts declared by the resource.
    /// </summary>
    /// <param name="root">The root document node.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed notable-date concepts.</returns>
    private static List<NotableDateDefinition> ReadNotableDates(IDocumentNode root, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateDefinition> definitions = new();

        foreach (IDocumentNode notableDate in root.List("NotableDates", "NotableDate", "notableDates"))
        {
            string id = ReadPatterned(notableDate, "id", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty;

            definitions.Add(new NotableDateDefinition(
                id,
                notableDate.Scalar("displayName") ?? string.Empty,
                ParseEnum(notableDate.Scalar("category"), NotableDateCategory.None, "category", diagnostics),
                ParseBool(notableDate.Scalar("defaultNonWorkingDay"), false, "defaultNonWorkingDay", diagnostics),
                ParseInt(notableDate.Scalar("defaultDurationDays"), 1, "defaultDurationDays", diagnostics),
                notableDate.TextList("Tags", "Tag", "value", "tags", "tags", diagnostics),
                ReadRules(notableDate, id, diagnostics)));
        }

        return definitions;
    }

    /// <summary>
    /// Reads the rules belonging to a notable-date concept.
    /// </summary>
    /// <param name="notableDate">The owning concept node.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed rules.</returns>
    private static List<NotableDateRule> ReadRules(IDocumentNode notableDate, string notableDateId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateRule> rules = new();

        foreach (IDocumentNode rule in notableDate.List("Rules", "Rule", "rules"))
            rules.Add(ReadRule(rule, notableDateId, diagnostics));

        return rules;
    }

    /// <summary>
    /// Reads a single rule node.
    /// </summary>
    /// <param name="node">The rule node.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="NotableDateRule" />.</returns>
    private static NotableDateRule ReadRule(IDocumentNode node, string notableDateId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        string id = ReadPatterned(node, "id", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty;
        IDocumentNode? strategyNode = node.Child("strategy");
        IDocumentNode? recurrenceNode = node.Child("recurrence");
        ValidateOccurrenceSource(strategyNode, recurrenceNode, notableDateId, id, diagnostics);

        RuleApplicability applicability = ReadApplicability(node.Child("applicability"), diagnostics);
        NotableDateDurationDefinition? duration = ReadDuration(node, applicability.Calendar, notableDateId, id, diagnostics);
        IDateRecurrenceStrategy? recurrence = ReadRecurrence(recurrenceNode, notableDateId, id, diagnostics);

        IReadOnlyList<string> adjustments = CheckPatterns(node, node.TextList("Adjustments", "Adjustment", "policyRef", "adjustments", "rule adjustments", diagnostics), NotableDateSchemaPattern.Identifier, "adjustments", diagnostics);
        IReadOnlyList<string> tags = node.TextList("Tags", "Tag", "value", "tags", "rule tags", diagnostics);

        return recurrence is not null
            ? new NotableDateRule(
                id,
                ParseInt(node.Scalar("priority"), 0, "rule priority", diagnostics),
                ParseNullableEnum<NotableDateCategory>(node.Scalar("category"), "category", diagnostics),
                ParseNullableBool(node.Scalar("nonWorking"), "rule nonWorking", diagnostics),
                duration,
                applicability,
                recurrence,
                adjustments,
                tags)
            : new NotableDateRule(
                id,
                ParseInt(node.Scalar("priority"), 0, "rule priority", diagnostics),
                ParseNullableEnum<NotableDateCategory>(node.Scalar("category"), "category", diagnostics),
                ParseNullableBool(node.Scalar("nonWorking"), "rule nonWorking", diagnostics),
                duration,
                applicability,
                ReadStrategy(strategyNode, applicability.Calendar, notableDateId, id, diagnostics),
                adjustments,
                tags);
    }

    /// <summary>
    /// Reports a rule that does not declare exactly one occurrence source: one recognized strategy kind or one
    /// recognized recurrence kind.
    /// </summary>
    /// <param name="strategy">The rule's strategy node, or <see langword="null" />.</param>
    /// <param name="recurrence">The rule's recurrence node, or <see langword="null" />.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <param name="patch">
    /// <see langword="true" /> when the nodes belong to a <c>PatchRule</c> override rather than a rule, selecting the
    /// override's wording.
    /// </param>
    /// <remarks>
    /// The XSD already enforces this for a rule in XML; the check exists for JSON, which is not schema-validated, and
    /// for a <c>PatchRule</c> override, whose schema permits both a strategy and a recurrence. It reports the same
    /// condition for both formats so a rule never falls back to a placeholder date or silently drops a source.
    /// </remarks>
    private static void ValidateOccurrenceSource(IDocumentNode? strategy, IDocumentNode? recurrence, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics, bool patch = false)
    {
        int sources = CountKinds(strategy, s_strategyKinds) + CountKinds(recurrence, s_recurrenceKinds);
        if (sources == 1)
            return;

        string format = (sources == 0, patch) switch
        {
            (true, false) => CalendarResourceStrings.Validation_OccurrenceSourceMissing,
            (false, false) => CalendarResourceStrings.Validation_OccurrenceSourceConflict,
            (true, true) => CalendarResourceStrings.Validation_PatchOccurrenceSourceMissing,
            (false, true) => CalendarResourceStrings.Validation_PatchOccurrenceSourceConflict,
        };

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL-RULE-SOURCE",
            string.Format(System.Globalization.CultureInfo.CurrentCulture, format, notableDateId, ruleId)));
    }

    /// <summary>
    /// Counts the recognized kinds a strategy or recurrence node holds.
    /// </summary>
    /// <param name="node">The strategy or recurrence node, or <see langword="null" />.</param>
    /// <param name="kinds">The recognized kind names.</param>
    /// <returns>The number of recognized kinds present; zero when <paramref name="node" /> is absent.</returns>
    private static int CountKinds(IDocumentNode? node, string[] kinds) =>
        node is null ? 0 : kinds.Count(kind => node.Child(kind) is not null);

    /// <summary>
    /// Reads a rule's applicability, falling back to a global Gregorian applicability when absent.
    /// </summary>
    /// <param name="node">The applicability node, or <see langword="null" />.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="RuleApplicability" />.</returns>
    private static RuleApplicability ReadApplicability(IDocumentNode? node, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (node is null)
            return new RuleApplicability(CalendarSystem.Gregorian, null, null, [], [], []);

        return new RuleApplicability(
            ParseEnum(node.Scalar("calendar"), CalendarSystem.Gregorian, "calendar", diagnostics),
            ParseNullableInt(node.Scalar("fromYear"), "applicability fromYear", diagnostics),
            ParseNullableInt(node.Scalar("toYear"), "applicability toYear", diagnostics),
            CheckPatterns(node, node.TextList(null, "Territory", "code", "territories", "applicability territories", diagnostics), NotableDateSchemaPattern.TerritoryCode, "territories", diagnostics),
            node.IntList(null, "OnlyYear", "value", "onlyYears", "applicability onlyYears", diagnostics),
            node.IntList(null, "ExceptYear", "value", "exceptYears", "applicability exceptYears", diagnostics),
            ParseNullableInt(node.Scalar("everyYears"), "applicability everyYears", diagnostics),
            ParseNullableInt(node.Scalar("anchorYear"), "applicability anchorYear", diagnostics));
    }

    /// <summary>
    /// Reads a rule's strategy, recording an error diagnostic instead of throwing when a value lies outside the
    /// strategy's own range.
    /// </summary>
    /// <param name="node">The strategy node, or <see langword="null" />.</param>
    /// <param name="calendar">The calendar system the rule's applicability declares.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>
    /// The parsed <see cref="IDateCalculationStrategy" />, or a 1 January placeholder when the strategy is absent,
    /// unrecognized, or invalid. The caller has then recorded an error diagnostic, so the placeholder never reaches a
    /// loaded resource.
    /// </returns>
    /// <remarks>
    /// The schema reports an out-of-range value in an XML document but parsing carries on, and a JSON document has no
    /// schema at all, so the strategy constructors' range guards are the last line of defence. Their
    /// <see cref="ArgumentException" /> becomes a <c>BODU-CAL-STRATEGY</c> diagnostic, as <see cref="ReadRecurrence" />
    /// does for recurrences, so a load fails only with its documented exceptions and a collect-mode load never throws
    /// for bad data.
    /// </remarks>
    private static IDateCalculationStrategy ReadStrategy(IDocumentNode? node, CalendarSystem calendar, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        try
        {
            return ReadStrategyCore(node, calendar, notableDateId, ruleId, diagnostics);
        }
        catch (ArgumentException ex)
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-STRATEGY",
                string.Format(System.Globalization.CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_StrategyInvalid, notableDateId, ruleId, ex.Message)));
            return new FixedDateStrategy(1, 1);
        }
    }

    /// <summary>
    /// Reads a rule's strategy by probing for the single populated strategy kind.
    /// </summary>
    /// <param name="node">The strategy node, or <see langword="null" />.</param>
    /// <param name="calendar">The calendar system the rule's applicability declares.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>
    /// The parsed <see cref="IDateCalculationStrategy" />, or a 1 January placeholder when no recognized strategy kind
    /// is present.
    /// </returns>
    /// <exception cref="ArgumentException">A parsed value lies outside the strategy's range.</exception>
    private static IDateCalculationStrategy ReadStrategyCore(IDocumentNode? node, CalendarSystem calendar, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (node is null)
            return new FixedDateStrategy(1, 1);

        if (node.Child("fixed") is IDocumentNode fixedStrategy)
        {
            (int month, string? monthAlias) = ParseFixedMonth(fixedStrategy.Scalar("month"), calendar, notableDateId, ruleId, diagnostics);
            return new FixedDateStrategy(
                month,
                ParseInt(fixedStrategy.Scalar("day"), 1, "fixed day", diagnostics),
                calendar,
                ParseBool(fixedStrategy.Scalar("sweepCalendarYears"), false, "fixed sweepCalendarYears", diagnostics),
                ParseBool(fixedStrategy.Scalar("skipLeapMonth"), false, "fixed skipLeapMonth", diagnostics),
                monthAlias);
        }

        if (node.Child("dayOfWeekInMonth") is IDocumentNode dayOfWeekInMonth)
        {
            return new DayOfWeekInMonthStrategy(
                ParseMonth(dayOfWeekInMonth.Scalar("month"), notableDateId, ruleId, diagnostics),
                ParseEnum(dayOfWeekInMonth.Scalar("dayOfWeek"), DayOfWeek.Monday, "dayOfWeekInMonth dayOfWeek", diagnostics),
                ParseEnum(dayOfWeekInMonth.Scalar("weekOrdinal"), WeekOrdinal.First, "dayOfWeekInMonth weekOrdinal", diagnostics));
        }

        if (node.Child("weekdayNearDate") is IDocumentNode weekdayNearDate)
        {
            return new WeekdayNearDateStrategy(
                ParseMonth(weekdayNearDate.Scalar("month"), notableDateId, ruleId, diagnostics),
                ParseInt(weekdayNearDate.Scalar("day"), 1, "weekdayNearDate day", diagnostics),
                ParseEnum(weekdayNearDate.Scalar("dayOfWeek"), DayOfWeek.Monday, "weekdayNearDate dayOfWeek", diagnostics),
                ParseEnum(weekdayNearDate.Scalar("direction"), WeekdayProximity.OnOrAfter, "weekdayNearDate direction", diagnostics));
        }

        if (node.Child("relativeWeekdayInMonth") is IDocumentNode relativeWeekdayInMonth)
        {
            return new RelativeWeekdayInMonthStrategy(
                ParseMonth(relativeWeekdayInMonth.Scalar("month"), notableDateId, ruleId, diagnostics),
                ParseEnum(relativeWeekdayInMonth.Scalar("dayOfWeek"), DayOfWeek.Monday, "relativeWeekdayInMonth dayOfWeek", diagnostics),
                ParseEnum(relativeWeekdayInMonth.Scalar("weekOrdinal"), WeekOrdinal.First, "relativeWeekdayInMonth weekOrdinal", diagnostics),
                ParseEnum(relativeWeekdayInMonth.Scalar("relativeDayOfWeek"), DayOfWeek.Monday, "relativeWeekdayInMonth relativeDayOfWeek", diagnostics),
                ParseEnum(relativeWeekdayInMonth.Scalar("direction"), WeekdayProximity.After, "relativeWeekdayInMonth direction", diagnostics));
        }

        if (node.Child("offsetFromRule") is IDocumentNode offsetFromRule)
        {
            string? ruleRef = ReadPatterned(offsetFromRule, "ruleRef", NotableDateSchemaPattern.Identifier, diagnostics);
            return new OffsetFromRuleStrategy(
                ReadPatterned(offsetFromRule, "notableDateRef", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty,
                string.IsNullOrEmpty(ruleRef) ? null : ruleRef,
                ParseInt(offsetFromRule.Scalar("offsetDays"), 0, "offsetFromRule offsetDays", diagnostics));
        }

        if (node.Child("algorithm") is IDocumentNode algorithm)
            return new AlgorithmDateStrategy(algorithm.Scalar("key") ?? string.Empty);

        if (node.Child("weekdayNearRule") is IDocumentNode weekdayNearRule)
        {
            string? ruleRef = ReadPatterned(weekdayNearRule, "ruleRef", NotableDateSchemaPattern.Identifier, diagnostics);
            return new WeekdayNearRuleStrategy(
                ReadPatterned(weekdayNearRule, "notableDateRef", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty,
                string.IsNullOrEmpty(ruleRef) ? null : ruleRef,
                ParseEnum(weekdayNearRule.Scalar("dayOfWeek"), DayOfWeek.Monday, "weekdayNearRule dayOfWeek", diagnostics),
                ParseEnum(weekdayNearRule.Scalar("direction"), WeekdayProximity.OnOrAfter, "weekdayNearRule direction", diagnostics),
                ParseInt(weekdayNearRule.Scalar("referenceYearOffset"), 0, "weekdayNearRule referenceYearOffset", diagnostics));
        }

        if (node.Child("nthWeekdayFromRule") is IDocumentNode nthWeekdayFromRule)
        {
            string? ruleRef = ReadPatterned(nthWeekdayFromRule, "ruleRef", NotableDateSchemaPattern.Identifier, diagnostics);
            return new NthWeekdayFromRuleStrategy(
                ReadPatterned(nthWeekdayFromRule, "notableDateRef", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty,
                string.IsNullOrEmpty(ruleRef) ? null : ruleRef,
                ParseEnum(nthWeekdayFromRule.Scalar("dayOfWeek"), DayOfWeek.Monday, "nthWeekdayFromRule dayOfWeek", diagnostics),
                ParseInt(nthWeekdayFromRule.Scalar("ordinal"), 1, "nthWeekdayFromRule ordinal", diagnostics),
                ParseInt(nthWeekdayFromRule.Scalar("referenceYearOffset"), 0, "nthWeekdayFromRule referenceYearOffset", diagnostics));
        }

        if (node.Child("workingDayOffsetFromRule") is IDocumentNode workingDayOffsetFromRule)
        {
            string? ruleRef = ReadPatterned(workingDayOffsetFromRule, "ruleRef", NotableDateSchemaPattern.Identifier, diagnostics);
            return new WorkingDayOffsetFromRuleStrategy(
                ReadPatterned(workingDayOffsetFromRule, "notableDateRef", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty,
                string.IsNullOrEmpty(ruleRef) ? null : ruleRef,
                ParseInt(workingDayOffsetFromRule.Scalar("offsetWorkingDays"), 0, "workingDayOffsetFromRule offsetWorkingDays", diagnostics),
                ParseInt(workingDayOffsetFromRule.Scalar("referenceYearOffset"), 0, "workingDayOffsetFromRule referenceYearOffset", diagnostics));
        }

        if (node.Child("workingDayInMonth") is IDocumentNode workingDayInMonth)
        {
            return new WorkingDayInMonthStrategy(
                ParseMonth(workingDayInMonth.Scalar("month"), notableDateId, ruleId, diagnostics),
                ParseInt(workingDayInMonth.Scalar("ordinal"), 1, "workingDayInMonth ordinal", diagnostics));
        }

        if (node.Child("ordinalDayOfMonth") is IDocumentNode ordinalDayOfMonth)
        {
            return new OrdinalDayOfMonthStrategy(
                ParseMonth(ordinalDayOfMonth.Scalar("month"), notableDateId, ruleId, diagnostics),
                ParseInt(ordinalDayOfMonth.Scalar("ordinal"), 1, "ordinalDayOfMonth ordinal", diagnostics));
        }

        if (node.Child("isoWeekDate") is IDocumentNode isoWeekDate)
        {
            return new IsoWeekDateStrategy(
                ParseInt(isoWeekDate.Scalar("week"), 1, "isoWeekDate week", diagnostics),
                ParseEnum(isoWeekDate.Scalar("dayOfWeek"), DayOfWeek.Monday, "isoWeekDate dayOfWeek", diagnostics));
        }

        if (node.Child("dayOfYear") is IDocumentNode dayOfYear)
            return new DayOfYearStrategy(ParseInt(dayOfYear.Scalar("ordinal"), 1, "dayOfYear ordinal", diagnostics));

        return new FixedDateStrategy(1, 1);
    }

    /// <summary>
    /// Reads a rule's duration, resolving either an inherited/fixed day count or a calculated end-date span.
    /// </summary>
    /// <param name="ruleNode">The rule node.</param>
    /// <param name="calendar">The calendar system the rule's applicability declares.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>
    /// The parsed duration definition, or <see langword="null" /> when the rule inherits the concept's default
    /// duration.
    /// </returns>
    private static NotableDateDurationDefinition? ReadDuration(IDocumentNode ruleNode, CalendarSystem calendar, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        int? durationDays = ParseNullableInt(ruleNode.Scalar("durationDays"), "durationDays", diagnostics);
        IDocumentNode? untilDate = ruleNode.Child("duration")?.Child("untilDate");

        if (durationDays is int days && untilDate is not null)
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-DURATION-CONFLICT",
                string.Format(System.Globalization.CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_DurationConflict, notableDateId, ruleId)));
            return new FixedDurationDefinition(days);
        }

        if (untilDate is not null)
        {
            if (untilDate.Child("strategy") is not IDocumentNode endStrategyNode)
            {
                diagnostics.Add(new NotableDateValidationDiagnostic(
                    NotableDateValidationSeverity.Error,
                    "BODU-CAL-DURATION-NOEND",
                    string.Format(System.Globalization.CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_DurationEndStrategyMissing, notableDateId, ruleId)));
                return null;
            }

            // The schema holds an XML end strategy to one kind; a JSON one is checked here, as a rule's start strategy
            // is, so the end never falls back to a placeholder 1 January or silently drops a kind.
            int endKinds = CountKinds(endStrategyNode, s_strategyKinds);
            if (endKinds != 1)
            {
                diagnostics.Add(endKinds == 0
                    ? new NotableDateValidationDiagnostic(
                        NotableDateValidationSeverity.Error,
                        "BODU-CAL-DURATION-NOEND",
                        string.Format(System.Globalization.CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_DurationEndStrategyKindMissing, notableDateId, ruleId))
                    : new NotableDateValidationDiagnostic(
                        NotableDateValidationSeverity.Error,
                        "BODU-CAL-DURATION-CONFLICT",
                        string.Format(System.Globalization.CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_DurationEndStrategyConflict, notableDateId, ruleId)));
                return null;
            }

            return new CalculatedEndDateDurationDefinition(
                ReadStrategy(endStrategyNode, calendar, notableDateId, ruleId, diagnostics),
                ParseEnum(untilDate.Scalar("startBoundary"), DateBoundary.Inclusive, "untilDate startBoundary", diagnostics),
                ParseEnum(untilDate.Scalar("endBoundary"), DateBoundary.Inclusive, "untilDate endBoundary", diagnostics),
                ParseEnum(untilDate.Scalar("selection"), EndDateSelection.FirstOnOrAfterStart, "untilDate selection", diagnostics));
        }

        return durationDays is int fixedDays ? new FixedDurationDefinition(fixedDays) : null;
    }

    /// <summary>
    /// Reads a rule's recurrence source, if present, by probing for the single populated recurrence kind.
    /// </summary>
    /// <param name="node">The recurrence node, or <see langword="null" />.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>
    /// The parsed <see cref="IDateRecurrenceStrategy" />, or <see langword="null" /> when the rule has no recurrence or
    /// its recurrence definition is invalid (a diagnostic is recorded in the latter case).
    /// </returns>
    private static IDateRecurrenceStrategy? ReadRecurrence(IDocumentNode? node, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (node is null)
            return null;

        try
        {
            if (node.Child("dailyInterval") is IDocumentNode dailyInterval)
            {
                return new DailyIntervalRecurrenceStrategy(
                    ParseNullableDate(dailyInterval.Scalar("anchorDate"), "dailyInterval anchorDate", diagnostics) ?? default,
                    ParseInt(dailyInterval.Scalar("intervalDays"), 1, "dailyInterval intervalDays", diagnostics));
            }

            if (node.Child("weekly") is IDocumentNode weekly)
            {
                List<DayOfWeek> days = ParseEnumList(weekly.ScalarList(null, "Day", "dayOfWeek", "days"), DayOfWeek.Monday, "weekly days", diagnostics);
                return new WeeklyRecurrenceStrategy(
                    days,
                    ParseInt(weekly.Scalar("intervalWeeks"), 1, "weekly intervalWeeks", diagnostics),
                    ParseNullableDate(weekly.Scalar("anchorDate"), "weekly anchorDate", diagnostics));
            }

            if (node.Child("monthlyDay") is IDocumentNode monthlyDay)
            {
                return new MonthlyDayRecurrenceStrategy(
                    ParseInt(monthlyDay.Scalar("dayOfMonth"), 1, "monthlyDay dayOfMonth", diagnostics),
                    ParseInt(monthlyDay.Scalar("intervalMonths"), 1, "monthlyDay intervalMonths", diagnostics),
                    ParseNullableDate(monthlyDay.Scalar("anchorDate"), "monthlyDay anchorDate", diagnostics),
                    ParseEnum(monthlyDay.Scalar("invalidDayBehavior"), InvalidDayOfMonthBehavior.Skip, "monthlyDay invalidDayBehavior", diagnostics));
            }

            if (node.Child("monthlyWeekday") is IDocumentNode monthlyWeekday)
            {
                return new MonthlyWeekdayRecurrenceStrategy(
                    ParseEnum(monthlyWeekday.Scalar("dayOfWeek"), DayOfWeek.Monday, "monthlyWeekday dayOfWeek", diagnostics),
                    ParseEnum(monthlyWeekday.Scalar("weekOrdinal"), WeekOrdinal.First, "monthlyWeekday weekOrdinal", diagnostics),
                    ParseInt(monthlyWeekday.Scalar("intervalMonths"), 1, "monthlyWeekday intervalMonths", diagnostics),
                    ParseNullableDate(monthlyWeekday.Scalar("anchorDate"), "monthlyWeekday anchorDate", diagnostics));
            }

            return null;
        }
        catch (ArgumentException ex)
        {
            diagnostics.Add(new NotableDateValidationDiagnostic(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-RECURRENCE",
                string.Format(System.Globalization.CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_RecurrenceInvalid, notableDateId, ruleId, ex.Message)));
            return null;
        }
    }

    /// <summary>
    /// Reads the override operations declared by the resource.
    /// </summary>
    /// <param name="root">The root document node.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed override operations.</returns>
    private static List<NotableDateRuleOverride> ReadOverrides(IDocumentNode root, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateRuleOverride> overrides = new();

        foreach (IDocumentNode operation in root.List("Overrides", null, "overrides"))
        {
            string notableDateRef = ReadPatterned(operation, "notableDateRef", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty;

            switch (operation.Discriminator("operation"))
            {
                case "RemoveRule":
                    overrides.Add(new RemoveRuleOverride(notableDateRef, ReadPatterned(operation, "ruleRef", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty));
                    break;

                case "PatchRule":
                    overrides.Add(ReadPatchRule(operation, notableDateRef, diagnostics));
                    break;

                case "AddRule" when operation.Child("rule") is IDocumentNode rule:
                    overrides.Add(new AddRuleOverride(notableDateRef, ReadRule(rule, notableDateRef, diagnostics)));
                    break;

                default:
                    break;
            }
        }

        return overrides;
    }

    /// <summary>
    /// Reads a <c>PatchRule</c> override operation.
    /// </summary>
    /// <param name="operation">The patch-rule node.</param>
    /// <param name="notableDateRef">The identifier of the targeted concept.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="PatchRuleOverride" />.</returns>
    private static PatchRuleOverride ReadPatchRule(IDocumentNode operation, string notableDateRef, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        string ruleRef = ReadPatterned(operation, "ruleRef", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty;

        IDocumentNode? applicabilityNode = operation.Child("applicability");
        IDocumentNode? strategyNode = operation.Child("strategy");
        IDocumentNode? recurrenceNode = operation.Child("recurrence");

        // A patch that carries neither a strategy nor a recurrence leaves the rule's occurrence source unchanged; one
        // that carries either replaces it, so it must name exactly one source, as a rule must.
        if (strategyNode is not null || recurrenceNode is not null)
            ValidateOccurrenceSource(strategyNode, recurrenceNode, notableDateRef, ruleRef, diagnostics, patch: true);

        RuleApplicability? applicability = applicabilityNode is null ? null : ReadApplicability(applicabilityNode, diagnostics);
        CalendarSystem calendar = applicability?.Calendar ?? CalendarSystem.Gregorian;

        IReadOnlyList<string>? adjustments = CheckPatterns(operation, operation.OptionalTextList("Adjustments", "Adjustment", "policyRef", "adjustments", "patch adjustments", diagnostics), NotableDateSchemaPattern.Identifier, "adjustments", diagnostics);
        IReadOnlyList<string>? tags = operation.OptionalTextList("Tags", "Tag", "value", "tags", "patch tags", diagnostics);

        // A patch that carries neither a durationDays attribute nor a Duration element leaves the duration unchanged.
        NotableDateDurationDefinition? duration =
            operation.Scalar("durationDays") is null && operation.Child("duration") is null
                ? null
                : ReadDuration(operation, calendar, notableDateRef, ruleRef, diagnostics);

        return new PatchRuleOverride(
            notableDateRef,
            ruleRef,
            ParseNullableInt(operation.Scalar("priority"), "patch priority", diagnostics),
            ParseNullableEnum<NotableDateCategory>(operation.Scalar("category"), "category", diagnostics),
            ParseNullableBool(operation.Scalar("nonWorking"), "patch nonWorking", diagnostics),
            duration,
            applicability,
            strategyNode is null ? null : ReadStrategy(strategyNode, calendar, notableDateRef, ruleRef, diagnostics),
            ReadRecurrence(recurrenceNode, notableDateRef, ruleRef, diagnostics),
            adjustments is null ? null : [.. adjustments],
            tags is null ? null : [.. tags]);
    }

    /// <summary>
    /// Reads the import directives declared by the resource.
    /// </summary>
    /// <param name="root">The root document node.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed import directives.</returns>
    private static List<NotableDateImport> ReadImports(IDocumentNode root, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateImport> imports = new();

        foreach (IDocumentNode import in root.List("Imports", "Import", "imports"))
        {
            List<NotableDateImportUse> uses = new();
            foreach (IDocumentNode use in import.List(null, "Use", "uses"))
            {
                uses.Add(new NotableDateImportUse(
                    ReadPatterned(use, "notableDateRef", NotableDateSchemaPattern.Identifier, diagnostics) ?? string.Empty,
                    ReadPatterned(use, "as", NotableDateSchemaPattern.Identifier, diagnostics),
                    ReadPatterned(use, "territory", NotableDateSchemaPattern.TerritoryCode, diagnostics),
                    ParseNullableEnum<NotableDateCategory>(use.Scalar("category"), "import use category", diagnostics),
                    ParseNullableBool(use.Scalar("nonWorking"), "import use nonWorking", diagnostics),
                    CheckPatterns(use, use.OptionalTextList("Adjustments", "Adjustment", "policyRef", "adjustments", "import use adjustments", diagnostics), NotableDateSchemaPattern.Identifier, "adjustments", diagnostics)));
            }

            imports.Add(new NotableDateImport(import.Scalar("resource") ?? string.Empty, uses));
        }

        return imports;
    }

    /// <summary>
    /// Reads a scalar that the schema holds to a pattern, checking a JSON value against it.
    /// </summary>
    /// <param name="node">The node holding the scalar, or <see langword="null" /> when absent.</param>
    /// <param name="name">The attribute or property name.</param>
    /// <param name="pattern">The pattern the schema enforces on the value.</param>
    /// <param name="diagnostics">
    /// The collection that receives the <c>BODU-CAL-SCHEMA</c> error for a JSON value that does not match the pattern.
    /// </param>
    /// <returns>The scalar value, or <see langword="null" /> when the node or the value is absent.</returns>
    private static string? ReadPatterned(IDocumentNode? node, string name, NotableDateSchemaPattern pattern, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (node is null)
            return null;

        string? value = node.Scalar(name);
        node.CheckPattern(value, pattern, name, diagnostics);
        return value;
    }

    /// <summary>
    /// Checks each value of a list that the schema holds to a pattern, checking a JSON value against it.
    /// </summary>
    /// <param name="node">The node holding the list.</param>
    /// <param name="values">The list's values, or <see langword="null" /> when the list is absent.</param>
    /// <param name="pattern">The pattern the schema enforces on each value.</param>
    /// <param name="field">The name of the JSON property holding the list, used in the diagnostics.</param>
    /// <param name="diagnostics">
    /// The collection that receives the <c>BODU-CAL-SCHEMA</c> error for each JSON value that does not match the
    /// pattern.
    /// </param>
    /// <returns><paramref name="values" />, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(values))]
    private static IReadOnlyList<string>? CheckPatterns(IDocumentNode node, IReadOnlyList<string>? values, NotableDateSchemaPattern pattern, string field, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (values is not null)
        {
            foreach (string value in values)
                node.CheckPattern(value, pattern, field, diagnostics);
        }

        return values;
    }
}
