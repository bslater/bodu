// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Globalization.Calendar.NotableDateDocumentParseHelpers;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Shapes a parsed notable-date document from an <see cref="IDocumentNode" /> tree, so the XML and JSON parsers share a
/// single schema-binding implementation. Format-specific concerns — XML schema validation and the raw parse — stay with
/// the two public parsers; everything structural lives here.
/// </summary>
internal static class NotableDateDocumentReader
{
    /// <summary>
    /// Reads a document from its root node.
    /// </summary>
    /// <param name="root">The root document node.</param>
    /// <param name="diagnostics">The collection that receives structural and semantic diagnostics.</param>
    /// <returns>The parsed <see cref="ParsedNotableDateDocument" />.</returns>
    public static ParsedNotableDateDocument Read(IDocumentNode root, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        return new ParsedNotableDateDocument(
            root.Scalar("resourceId") ?? string.Empty,
            root.Scalar("schemaVersion") ?? string.Empty,
            ReadResolutionPolicy(root.Child("resolutionPolicy")),
            ReadAdjustmentPolicies(root),
            ReadNotableDates(root, diagnostics),
            ReadOverrides(root, diagnostics),
            ReadImports(root));
    }

    /// <summary>
    /// Reads the resource-level resolution policy, falling back to the recommended defaults when absent.
    /// </summary>
    /// <param name="node">The resolution-policy node, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="ResolutionPolicy" />.</returns>
    private static ResolutionPolicy ReadResolutionPolicy(IDocumentNode? node)
    {
        if (node is null)
            return ResolutionPolicy.Default;

        IReadOnlyList<string>? precedence = node.OptionalScalarList("CategoryPrecedence", "Category", "value", "categoryPrecedence");

        return new ResolutionPolicy(
            ParseEnum(node.Scalar("duplicatePolicy"), DuplicatePolicy.Error),
            ParseEnum(node.Scalar("sameDayCollisionPolicy"), CollisionPolicy.KeepAll),
            ParseEnum(node.Scalar("spanCollisionPolicy"), CollisionPolicy.KeepAll),
            ParseEnum(node.Scalar("priorityDirection"), PriorityDirection.HigherWins),
            ParseEnum(node.Scalar("observedDateRangePolicy"), ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion),
            ParseWorkingWeek(node.Scalar("workingDays")),
            precedence is null ? null : [.. precedence.Select(c => ParseEnum(c, NotableDateCategory.None))]);
    }

    /// <summary>
    /// Reads the reusable adjustment policies declared by the resource.
    /// </summary>
    /// <param name="root">The root document node.</param>
    /// <returns>The parsed adjustment policies.</returns>
    private static List<AdjustmentPolicy> ReadAdjustmentPolicies(IDocumentNode root)
    {
        List<AdjustmentPolicy> policies = new();

        foreach (IDocumentNode policy in root.List("AdjustmentPolicies", "AdjustmentPolicy", "adjustmentPolicies"))
        {
            IDocumentNode? trigger = policy.Child("trigger");
            IDocumentNode? action = policy.Child("action");
            IDocumentNode? emission = policy.Child("emission");

            IEnumerable<DayOfWeek> weekdays = trigger is null
                ? []
                : trigger.ScalarList(null, "Weekday", "value", "weekdays").Select(w => ParseEnum(w, DayOfWeek.Monday));

            policies.Add(new AdjustmentPolicy(
                policy.Scalar("id") ?? string.Empty,
                ParseInt(policy.Scalar("priority"), 0),
                ReadScope(policy.Child("scope")),
                ParseEnum(trigger?.Scalar("type"), AdjustmentTrigger.Always),
                weekdays,
                ParseEnum(action?.Scalar("type"), AdjustmentAction.None),
                ParseNullableEnum<DayOfWeek>(action?.Scalar("dayOfWeek")),
                ParseInt(action?.Scalar("days"), 0),
                ParseNullableInt(action?.Scalar("maxSearchDays")),
                ParseBool(action?.Scalar("skipWeekends"), true),
                ParseBool(action?.Scalar("skipNonWorkingDates"), false),
                ParseEnum(emission?.Scalar("mode"), EmissionMode.ActualOnly),
                emission?.Scalar("reason"),
                ParseNullableBool(emission?.Scalar("nonWorking")),
                ParseTriggerMonth(trigger?.Scalar("month")),
                ParseNullableInt(trigger?.Scalar("day")),
                ParseNullableEnum<WeekOrdinal>(trigger?.Scalar("weekOrdinal")),
                action?.Scalar("notableDateRef"),
                action?.Scalar("ruleRef"),
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
    private static IReadOnlyDictionary<string, string>? ReadHandlerParameters(IDocumentNode policy)
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
    /// <returns>The parsed <see cref="AdjustmentScope" />.</returns>
    private static AdjustmentScope ReadScope(IDocumentNode? node)
    {
        if (node is null)
            return AdjustmentScope.Global;

        return new AdjustmentScope(
            node.ScalarList(null, "Territory", "code", "territories"),
            node.ScalarList(null, "Calendar", "name", "calendars").Select(c => ParseEnum(c, CalendarSystem.Gregorian)),
            node.ScalarList(null, "Category", "value", "categories").Select(c => ParseEnum(c, NotableDateCategory.None)),
            node.ScalarList(null, "NotableDate", "ref", "notableDateRefs"),
            node.ScalarList(null, "Rule", "ruleRef", "ruleRefs"),
            ParseNullableInt(node.Scalar("fromYear")),
            ParseNullableInt(node.Scalar("toYear")),
            node.IntList(null, "OnlyYear", "value", "onlyYears"),
            node.IntList(null, "ExceptYear", "value", "exceptYears"));
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
            string id = notableDate.Scalar("id") ?? string.Empty;

            definitions.Add(new NotableDateDefinition(
                id,
                notableDate.Scalar("displayName") ?? string.Empty,
                ParseEnum(notableDate.Scalar("category"), NotableDateCategory.None),
                ParseBool(notableDate.Scalar("defaultNonWorkingDay"), false),
                ParseInt(notableDate.Scalar("defaultDurationDays"), 1),
                notableDate.ScalarList("Tags", "Tag", "value", "tags"),
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
        string id = node.Scalar("id") ?? string.Empty;
        RuleApplicability applicability = ReadApplicability(node.Child("applicability"));
        NotableDateDurationDefinition? duration = ReadDuration(node, applicability.Calendar, notableDateId, id, diagnostics);
        IDateRecurrenceStrategy? recurrence = ReadRecurrence(node.Child("recurrence"), notableDateId, id, diagnostics);

        IReadOnlyList<string> adjustments = node.ScalarList("Adjustments", "Adjustment", "policyRef", "adjustments");
        IReadOnlyList<string> tags = node.ScalarList("Tags", "Tag", "value", "tags");

        return recurrence is not null
            ? new NotableDateRule(
                id,
                ParseInt(node.Scalar("priority"), 0),
                ParseNullableEnum<NotableDateCategory>(node.Scalar("category")),
                ParseNullableBool(node.Scalar("nonWorking")),
                duration,
                applicability,
                recurrence,
                adjustments,
                tags)
            : new NotableDateRule(
                id,
                ParseInt(node.Scalar("priority"), 0),
                ParseNullableEnum<NotableDateCategory>(node.Scalar("category")),
                ParseNullableBool(node.Scalar("nonWorking")),
                duration,
                applicability,
                ReadStrategy(node.Child("strategy"), applicability.Calendar, notableDateId, id, diagnostics),
                adjustments,
                tags);
    }

    /// <summary>
    /// Reads a rule's applicability, falling back to a global Gregorian applicability when absent.
    /// </summary>
    /// <param name="node">The applicability node, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="RuleApplicability" />.</returns>
    private static RuleApplicability ReadApplicability(IDocumentNode? node)
    {
        if (node is null)
            return new RuleApplicability(CalendarSystem.Gregorian, null, null, [], [], []);

        return new RuleApplicability(
            ParseEnum(node.Scalar("calendar"), CalendarSystem.Gregorian),
            ParseNullableInt(node.Scalar("fromYear")),
            ParseNullableInt(node.Scalar("toYear")),
            node.ScalarList(null, "Territory", "code", "territories"),
            node.IntList(null, "OnlyYear", "value", "onlyYears"),
            node.IntList(null, "ExceptYear", "value", "exceptYears"),
            ParseNullableInt(node.Scalar("everyYears")),
            ParseNullableInt(node.Scalar("anchorYear")));
    }

    /// <summary>
    /// Reads a rule's strategy by probing for the single populated strategy kind.
    /// </summary>
    /// <param name="node">The strategy node, or <see langword="null" />.</param>
    /// <param name="calendar">The calendar system the rule's applicability declares.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="IDateCalculationStrategy" />.</returns>
    private static IDateCalculationStrategy ReadStrategy(IDocumentNode? node, CalendarSystem calendar, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (node is null)
            return new FixedDateStrategy(1, 1);

        if (node.Child("fixed") is IDocumentNode fixedStrategy)
        {
            (int month, string? monthAlias) = ParseFixedMonth(fixedStrategy.Scalar("month"), calendar, notableDateId, ruleId, diagnostics);
            return new FixedDateStrategy(
                month,
                Math.Clamp(ParseInt(fixedStrategy.Scalar("day"), 1), 1, 31),
                calendar,
                ParseBool(fixedStrategy.Scalar("sweepCalendarYears"), false),
                ParseBool(fixedStrategy.Scalar("skipLeapMonth"), false),
                monthAlias);
        }

        if (node.Child("dayOfWeekInMonth") is IDocumentNode dayOfWeekInMonth)
        {
            return new DayOfWeekInMonthStrategy(
                ParseMonth(dayOfWeekInMonth.Scalar("month"), notableDateId, ruleId, diagnostics),
                ParseEnum(dayOfWeekInMonth.Scalar("dayOfWeek"), DayOfWeek.Monday),
                ParseEnum(dayOfWeekInMonth.Scalar("weekOrdinal"), WeekOrdinal.First));
        }

        if (node.Child("weekdayNearDate") is IDocumentNode weekdayNearDate)
        {
            return new WeekdayNearDateStrategy(
                ParseMonth(weekdayNearDate.Scalar("month"), notableDateId, ruleId, diagnostics),
                Math.Clamp(ParseInt(weekdayNearDate.Scalar("day"), 1), 1, 31),
                ParseEnum(weekdayNearDate.Scalar("dayOfWeek"), DayOfWeek.Monday),
                ParseEnum(weekdayNearDate.Scalar("direction"), WeekdayProximity.OnOrAfter));
        }

        if (node.Child("relativeWeekdayInMonth") is IDocumentNode relativeWeekdayInMonth)
        {
            return new RelativeWeekdayInMonthStrategy(
                ParseMonth(relativeWeekdayInMonth.Scalar("month"), notableDateId, ruleId, diagnostics),
                ParseEnum(relativeWeekdayInMonth.Scalar("dayOfWeek"), DayOfWeek.Monday),
                ParseEnum(relativeWeekdayInMonth.Scalar("weekOrdinal"), WeekOrdinal.First),
                ParseEnum(relativeWeekdayInMonth.Scalar("relativeDayOfWeek"), DayOfWeek.Monday),
                ParseEnum(relativeWeekdayInMonth.Scalar("direction"), WeekdayProximity.After));
        }

        if (node.Child("offsetFromRule") is IDocumentNode offsetFromRule)
        {
            string? ruleRef = offsetFromRule.Scalar("ruleRef");
            return new OffsetFromRuleStrategy(
                offsetFromRule.Scalar("notableDateRef") ?? string.Empty,
                string.IsNullOrEmpty(ruleRef) ? null : ruleRef,
                ParseInt(offsetFromRule.Scalar("offsetDays"), 0));
        }

        if (node.Child("algorithm") is IDocumentNode algorithm)
            return new AlgorithmDateStrategy(algorithm.Scalar("key") ?? string.Empty);

        if (node.Child("weekdayNearRule") is IDocumentNode weekdayNearRule)
        {
            string? ruleRef = weekdayNearRule.Scalar("ruleRef");
            return new WeekdayNearRuleStrategy(
                weekdayNearRule.Scalar("notableDateRef") ?? string.Empty,
                string.IsNullOrEmpty(ruleRef) ? null : ruleRef,
                ParseEnum(weekdayNearRule.Scalar("dayOfWeek"), DayOfWeek.Monday),
                ParseEnum(weekdayNearRule.Scalar("direction"), WeekdayProximity.OnOrAfter),
                ParseInt(weekdayNearRule.Scalar("referenceYearOffset"), 0));
        }

        if (node.Child("nthWeekdayFromRule") is IDocumentNode nthWeekdayFromRule)
        {
            string? ruleRef = nthWeekdayFromRule.Scalar("ruleRef");
            return new NthWeekdayFromRuleStrategy(
                nthWeekdayFromRule.Scalar("notableDateRef") ?? string.Empty,
                string.IsNullOrEmpty(ruleRef) ? null : ruleRef,
                ParseEnum(nthWeekdayFromRule.Scalar("dayOfWeek"), DayOfWeek.Monday),
                ParseInt(nthWeekdayFromRule.Scalar("ordinal"), 1),
                ParseInt(nthWeekdayFromRule.Scalar("referenceYearOffset"), 0));
        }

        if (node.Child("workingDayOffsetFromRule") is IDocumentNode workingDayOffsetFromRule)
        {
            string? ruleRef = workingDayOffsetFromRule.Scalar("ruleRef");
            return new WorkingDayOffsetFromRuleStrategy(
                workingDayOffsetFromRule.Scalar("notableDateRef") ?? string.Empty,
                string.IsNullOrEmpty(ruleRef) ? null : ruleRef,
                ParseInt(workingDayOffsetFromRule.Scalar("offsetWorkingDays"), 0),
                ParseInt(workingDayOffsetFromRule.Scalar("referenceYearOffset"), 0));
        }

        if (node.Child("workingDayInMonth") is IDocumentNode workingDayInMonth)
        {
            return new WorkingDayInMonthStrategy(
                ParseMonth(workingDayInMonth.Scalar("month"), notableDateId, ruleId, diagnostics),
                ParseInt(workingDayInMonth.Scalar("ordinal"), 1));
        }

        if (node.Child("ordinalDayOfMonth") is IDocumentNode ordinalDayOfMonth)
        {
            return new OrdinalDayOfMonthStrategy(
                ParseMonth(ordinalDayOfMonth.Scalar("month"), notableDateId, ruleId, diagnostics),
                ParseInt(ordinalDayOfMonth.Scalar("ordinal"), 1));
        }

        if (node.Child("isoWeekDate") is IDocumentNode isoWeekDate)
        {
            return new IsoWeekDateStrategy(
                ParseInt(isoWeekDate.Scalar("week"), 1),
                ParseEnum(isoWeekDate.Scalar("dayOfWeek"), DayOfWeek.Monday));
        }

        if (node.Child("dayOfYear") is IDocumentNode dayOfYear)
            return new DayOfYearStrategy(ParseInt(dayOfYear.Scalar("ordinal"), 1));

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
        int? durationDays = ParseNullableInt(ruleNode.Scalar("durationDays"));
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

            return new CalculatedEndDateDurationDefinition(
                ReadStrategy(endStrategyNode, calendar, notableDateId, ruleId, diagnostics),
                ParseEnum(untilDate.Scalar("startBoundary"), DateBoundary.Inclusive),
                ParseEnum(untilDate.Scalar("endBoundary"), DateBoundary.Inclusive),
                ParseEnum(untilDate.Scalar("selection"), EndDateSelection.FirstOnOrAfterStart));
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
                    ParseDate(dailyInterval.Scalar("anchorDate")) ?? default,
                    ParseInt(dailyInterval.Scalar("intervalDays"), 1));
            }

            if (node.Child("weekly") is IDocumentNode weekly)
            {
                var days = weekly.ScalarList(null, "Day", "dayOfWeek", "days")
                    .Select(value => ParseEnum(value, DayOfWeek.Monday))
                    .ToList();
                return new WeeklyRecurrenceStrategy(
                    days,
                    ParseInt(weekly.Scalar("intervalWeeks"), 1),
                    ParseDate(weekly.Scalar("anchorDate")));
            }

            if (node.Child("monthlyDay") is IDocumentNode monthlyDay)
            {
                return new MonthlyDayRecurrenceStrategy(
                    ParseInt(monthlyDay.Scalar("dayOfMonth"), 1),
                    ParseInt(monthlyDay.Scalar("intervalMonths"), 1),
                    ParseDate(monthlyDay.Scalar("anchorDate")),
                    ParseEnum(monthlyDay.Scalar("invalidDayBehavior"), InvalidDayOfMonthBehavior.Skip));
            }

            if (node.Child("monthlyWeekday") is IDocumentNode monthlyWeekday)
            {
                return new MonthlyWeekdayRecurrenceStrategy(
                    ParseEnum(monthlyWeekday.Scalar("dayOfWeek"), DayOfWeek.Monday),
                    ParseEnum(monthlyWeekday.Scalar("weekOrdinal"), WeekOrdinal.First),
                    ParseInt(monthlyWeekday.Scalar("intervalMonths"), 1),
                    ParseDate(monthlyWeekday.Scalar("anchorDate")));
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
    /// Parses an ISO-8601 <c>yyyy-MM-dd</c> date token.
    /// </summary>
    /// <param name="value">The date token, or <see langword="null" />.</param>
    /// <returns>The parsed date, or <see langword="null" /> when the token is absent or malformed.</returns>
    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateOnly result)
            ? result
            : null;

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
            string notableDateRef = operation.Scalar("notableDateRef") ?? string.Empty;

            switch (operation.Discriminator("operation"))
            {
                case "RemoveRule":
                    overrides.Add(new RemoveRuleOverride(notableDateRef, operation.Scalar("ruleRef") ?? string.Empty));
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
        string ruleRef = operation.Scalar("ruleRef") ?? string.Empty;

        IDocumentNode? applicabilityNode = operation.Child("applicability");
        IDocumentNode? strategyNode = operation.Child("strategy");
        RuleApplicability? applicability = applicabilityNode is null ? null : ReadApplicability(applicabilityNode);
        CalendarSystem calendar = applicability?.Calendar ?? CalendarSystem.Gregorian;

        IReadOnlyList<string>? adjustments = operation.OptionalScalarList("Adjustments", "Adjustment", "policyRef", "adjustments");
        IReadOnlyList<string>? tags = operation.OptionalScalarList("Tags", "Tag", "value", "tags");

        // A patch that carries neither a durationDays attribute nor a Duration element leaves the duration unchanged.
        NotableDateDurationDefinition? duration =
            operation.Scalar("durationDays") is null && operation.Child("duration") is null
                ? null
                : ReadDuration(operation, calendar, notableDateRef, ruleRef, diagnostics);

        return new PatchRuleOverride(
            notableDateRef,
            ruleRef,
            ParseNullableInt(operation.Scalar("priority")),
            ParseNullableEnum<NotableDateCategory>(operation.Scalar("category")),
            ParseNullableBool(operation.Scalar("nonWorking")),
            duration,
            applicability,
            strategyNode is null ? null : ReadStrategy(strategyNode, calendar, notableDateRef, ruleRef, diagnostics),
            ReadRecurrence(operation.Child("recurrence"), notableDateRef, ruleRef, diagnostics),
            adjustments is null ? null : [.. adjustments],
            tags is null ? null : [.. tags]);
    }

    /// <summary>
    /// Reads the import directives declared by the resource.
    /// </summary>
    /// <param name="root">The root document node.</param>
    /// <returns>The parsed import directives.</returns>
    private static List<NotableDateImport> ReadImports(IDocumentNode root)
    {
        List<NotableDateImport> imports = new();

        foreach (IDocumentNode import in root.List("Imports", "Import", "imports"))
        {
            List<NotableDateImportUse> uses = new();
            foreach (IDocumentNode use in import.List(null, "Use", "uses"))
            {
                uses.Add(new NotableDateImportUse(
                    use.Scalar("notableDateRef") ?? string.Empty,
                    use.Scalar("as"),
                    use.Scalar("territory"),
                    ParseNullableEnum<NotableDateCategory>(use.Scalar("category")),
                    ParseNullableBool(use.Scalar("nonWorking")),
                    use.OptionalScalarList("Adjustments", "Adjustment", "policyRef", "adjustments")));
            }

            imports.Add(new NotableDateImport(import.Scalar("resource") ?? string.Empty, uses));
        }

        return imports;
    }
}
