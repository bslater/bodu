// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilder.JsonRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Implements the node-reading helpers that reconstruct a <see cref="NotableDateDocumentBuilder" /> from a parsed JSON
/// document in the subset form.
/// </summary>
public sealed partial class NotableDateDocumentBuilder
{
    /// <summary>Maps each JSON strategy kind, matched ignoring case as the loader matches it, to the local name of its XML element and the names of the attributes the loader reads from its body, which are the element's schema attribute names.</summary>
    private static readonly Dictionary<string, (string Element, string[] Attributes)> s_jsonStrategyKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fixed"] = ("Fixed", ["month", "day", "skipLeapMonth", "sweepCalendarYears"]),
        ["dayOfWeekInMonth"] = ("DayOfWeekInMonth", ["month", "dayOfWeek", "weekOrdinal"]),
        ["weekdayNearDate"] = ("WeekdayNearDate", ["month", "day", "dayOfWeek", "direction"]),
        ["relativeWeekdayInMonth"] = ("RelativeWeekdayInMonth", ["month", "dayOfWeek", "weekOrdinal", "relativeDayOfWeek", "direction"]),
        ["offsetFromRule"] = ("OffsetFromRule", ["notableDateRef", "ruleRef", "offsetDays"]),
        ["algorithm"] = ("Algorithm", ["key"]),
        ["weekdayNearRule"] = ("WeekdayNearRule", ["notableDateRef", "ruleRef", "dayOfWeek", "direction", "referenceYearOffset"]),
        ["nthWeekdayFromRule"] = ("NthWeekdayFromRule", ["notableDateRef", "ruleRef", "dayOfWeek", "ordinal", "referenceYearOffset"]),
        ["workingDayOffsetFromRule"] = ("WorkingDayOffsetFromRule", ["notableDateRef", "ruleRef", "offsetWorkingDays", "referenceYearOffset"]),
        ["workingDayInMonth"] = ("WorkingDayInMonth", ["month", "ordinal"]),
        ["ordinalDayOfMonth"] = ("OrdinalDayOfMonth", ["month", "ordinal"]),
        ["isoWeekDate"] = ("IsoWeekDate", ["week", "dayOfWeek"]),
        ["dayOfYear"] = ("DayOfYear", ["ordinal"]),
    };

    /// <summary>Maps each JSON recurrence kind, matched ignoring case as the loader matches it, to the local name of its XML element and the names of the attributes the loader reads from its body, which are the element's schema attribute names; a weekly recurrence's <c>days</c> list, which becomes child elements, is read apart from them.</summary>
    private static readonly Dictionary<string, (string Element, string[] Attributes)> s_jsonRecurrenceKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dailyInterval"] = ("DailyInterval", ["anchorDate", "intervalDays"]),
        ["weekly"] = ("Weekly", ["intervalWeeks", "anchorDate"]),
        ["monthlyDay"] = ("MonthlyDay", ["dayOfMonth", "intervalMonths", "anchorDate", "invalidDayBehavior"]),
        ["monthlyWeekday"] = ("MonthlyWeekday", ["dayOfWeek", "weekOrdinal", "intervalMonths", "anchorDate"]),
    };

    /// <summary>
    /// Reads the JSON <c>metadata</c> object into the builder.
    /// </summary>
    /// <param name="metadata">The metadata object, or <see langword="null" /> when absent.</param>
    private void ReadMetadataJson(JsonObject? metadata)
    {
        if (metadata is null)
            return;

        _metadataName = UnreadText(metadata, "name", "a metadata name that is an object or an array");
        _metadataDescription = UnreadText(metadata, "description", "a metadata description that is an object or an array");
        _sources.Clear();
        _sources.AddRange(ReadUnreadTextArray(Property(metadata, "sources") as JsonArray, "a metadata source that is an object or an array"));
    }

    /// <summary>
    /// Reads the JSON <c>resolutionPolicy</c> object into the builder.
    /// </summary>
    /// <param name="policyJson">The resolution-policy object, or <see langword="null" /> when absent.</param>
    private void ReadResolutionPolicyJson(JsonObject? policyJson)
    {
        if (policyJson is null)
            return;

        DayOfWeekSet? workingWeek = ParseWorkingWeek(ScalarText(policyJson, "workingDays"));

        IReadOnlyList<NotableDateCategory>? categoryPrecedence = ArrayProperty(policyJson, "categoryPrecedence") is not JsonArray array
            ? null
            : [.. ReadNameArray(array).Select(item => ParseEnum<NotableDateCategory>(item, "categoryPrecedence"))];

        ResolutionPolicyBuilder policy = new();
        policy.SetParsedValues(
            ParseNullableEnum<DuplicatePolicy>(ScalarText(policyJson, "duplicatePolicy"), "duplicatePolicy"),
            ParseNullableEnum<CollisionPolicy>(ScalarText(policyJson, "sameDayCollisionPolicy"), "sameDayCollisionPolicy"),
            ParseNullableEnum<CollisionPolicy>(ScalarText(policyJson, "spanCollisionPolicy"), "spanCollisionPolicy"),
            ParseNullableEnum<PriorityDirection>(ScalarText(policyJson, "priorityDirection"), "priorityDirection"),
            ParseNullableEnum<ObservedDateRangePolicy>(ScalarText(policyJson, "observedDateRangePolicy"), "observedDateRangePolicy"),
            workingWeek,
            categoryPrecedence);

        _resolutionPolicy = policy;
    }

    /// <summary>
    /// Reads the JSON <c>adjustmentPolicies</c> array into the builder.
    /// </summary>
    /// <param name="array">The adjustment-policies array, or <see langword="null" /> when absent.</param>
    private void ReadAdjustmentPoliciesJson(JsonArray? array)
    {
        if (array is null)
            return;

        foreach (JsonNode? node in array)
        {
            if (node is JsonObject policyJson)
                _adjustmentPolicies.Add(ReadAdjustmentPolicyJson(policyJson));
        }
    }

    /// <summary>
    /// Reads a single JSON adjustment-policy object into a policy builder.
    /// </summary>
    /// <param name="policyJson">The policy object.</param>
    /// <returns>The reconstructed <see cref="AdjustmentPolicyBuilder" />.</returns>
    private static AdjustmentPolicyBuilder ReadAdjustmentPolicyJson(JsonObject policyJson)
    {
        AdjustmentPolicyBuilder policy = new(ScalarText(policyJson, "id") ?? string.Empty);
        policy.SetParsedHeader(
            JsonInt(policyJson, "priority", "adjustment policy priority"),
            UnreadText(policyJson, "description", "an adjustment policy description that is an object or an array"),
            ReadScopeJson(ObjectProperty(policyJson, "scope")));

        if (ObjectProperty(policyJson, "trigger") is JsonObject trigger)
        {
            policy.SetParsedTrigger(
                ParseNullableEnum<AdjustmentTrigger>(ScalarText(trigger, "type"), "trigger type"),
                ReadNameArray(ArrayProperty(trigger, "weekdays")).Select(day => ParseEnum<DayOfWeek>(day, "trigger weekdays")),
                ParseTriggerMonth(ScalarText(trigger, "month"), "trigger month"),
                JsonInt(trigger, "day", "trigger day"),
                ParseNullableEnum<WeekOrdinal>(ScalarText(trigger, "weekOrdinal"), "trigger weekOrdinal"),
                ScalarText(trigger, "handlerKey"));
        }

        if (ObjectProperty(policyJson, "action") is JsonObject action)
        {
            policy.SetParsedAction(
                ParseNullableEnum<AdjustmentAction>(ScalarText(action, "type"), "action type"),
                JsonInt(action, "days", "action days"),
                ParseNullableEnum<DayOfWeek>(ScalarText(action, "dayOfWeek"), "action dayOfWeek"),
                JsonInt(action, "maxSearchDays", "action maxSearchDays"),
                JsonBool(action, "skipWeekends", "action skipWeekends"),
                JsonBool(action, "skipNonWorkingDates", "action skipNonWorkingDates"),
                ScalarText(action, "notableDateRef"),
                ScalarText(action, "ruleRef"),
                ScalarText(action, "handlerKey"));
        }

        if (ObjectProperty(policyJson, "emission") is JsonObject emission)
        {
            policy.SetParsedEmission(
                ParseNullableEnum<EmissionMode>(ScalarText(emission, "mode"), "emission mode"),
                ScalarText(emission, "reason"),
                JsonBool(emission, "nonWorking", "emission nonWorking"));
        }

        if (ObjectProperty(policyJson, "parameters") is JsonObject parameters)
            policy.SetParsedParameters(ReadParametersJson(parameters));

        return policy;
    }

    /// <summary>
    /// Reads a JSON handler-parameter map as the loader reads it: a string verbatim, a number by its JSON text, a
    /// Boolean as <see cref="bool.TrueString" /> or <see cref="bool.FalseString" />, and a <c>null</c> as empty text.
    /// </summary>
    /// <param name="parameters">The parameter map.</param>
    /// <returns>The key/value pairs, in document order.</returns>
    /// <exception cref="NotSupportedException">A parameter value is an object or an array.</exception>
    private static List<KeyValuePair<string, string>> ReadParametersJson(JsonObject parameters)
    {
        List<KeyValuePair<string, string>> pairs = new();
        foreach (KeyValuePair<string, JsonNode?> property in parameters)
        {
            // The loader keeps an object or an array as its original JSON text, white space included, which a parsed
            // node no longer carries, so such a value is rejected rather than read differently.
            string value = property.Value switch
            {
                null => string.Empty,
                JsonValue text when text.GetValueKind() == JsonValueKind.String => text.GetValue<string>(),
                JsonValue flag when flag.GetValueKind() == JsonValueKind.True => bool.TrueString,
                JsonValue flag when flag.GetValueKind() == JsonValueKind.False => bool.FalseString,
                JsonValue number => number.ToJsonString(),
                _ => throw UnrepresentableJsonField("a handler parameter whose value is an object or an array"),
            };

            pairs.Add(new KeyValuePair<string, string>(property.Key, value));
        }

        return pairs;
    }

    /// <summary>
    /// Reads a JSON adjustment-scope object into a scope builder.
    /// </summary>
    /// <param name="scopeJson">The scope object, or <see langword="null" /> when absent.</param>
    /// <returns>The reconstructed scope builder, or <see langword="null" /> when the object is absent.</returns>
    private static AdjustmentScopeBuilder? ReadScopeJson(JsonObject? scopeJson)
    {
        if (scopeJson is null)
            return null;

        AdjustmentScopeBuilder scope = new();
        scope.SetParsedValues(
            ReadTextArray(ArrayProperty(scopeJson, "territories"), "scope territories"),
            ReadNameArray(ArrayProperty(scopeJson, "calendars")).Select(calendar => ParseEnum<CalendarSystem>(calendar, "scope calendars")),
            ReadNameArray(ArrayProperty(scopeJson, "categories")).Select(category => ParseEnum<NotableDateCategory>(category, "scope categories")),
            ReadTextArray(ArrayProperty(scopeJson, "notableDateRefs"), "scope notableDateRefs"),
            ReadScopeRuleRefsJson(ArrayProperty(scopeJson, "ruleRefs")),
            ReadIntArray(ArrayProperty(scopeJson, "onlyYears"), "scope onlyYears"),
            ReadIntArray(ArrayProperty(scopeJson, "exceptYears"), "scope exceptYears"),
            JsonInt(scopeJson, "fromYear", "scope fromYear"),
            JsonInt(scopeJson, "toYear", "scope toYear"));

        return scope;
    }

    /// <summary>
    /// Reads a JSON adjustment scope's rule references, each of which must name its concept, since the document's XML
    /// form has no unqualified rule reference.
    /// </summary>
    /// <param name="array">The <c>ruleRefs</c> array, or <see langword="null" /> when absent.</param>
    /// <returns>The qualified rule references; empty when the array is absent.</returns>
    /// <exception cref="NotSupportedException">A reference does not name its concept.</exception>
    /// <exception cref="FormatException">An item is neither a rule identifier nor a reference object.</exception>
    private static List<(string NotableDateRef, string RuleRef)> ReadScopeRuleRefsJson(JsonArray? array)
    {
        List<(string NotableDateRef, string RuleRef)> references = new();
        if (array is null)
            return references;

        foreach (JsonNode? node in array)
        {
            if (node is JsonObject reference && ScalarText(reference, "notableDateRef") is string notableDateRef)
                references.Add((notableDateRef, ScalarText(reference, "ruleRef") ?? string.Empty));
            else if (node is JsonObject || (node is JsonValue name && name.GetValueKind() == JsonValueKind.String))
                throw UnrepresentableJsonField("a scope rule reference that does not name its notableDateRef");
            else
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidReferenceItem, "scope ruleRefs", node?.ToJsonString() ?? "null"));
        }

        return references;
    }

    /// <summary>
    /// Reads the JSON <c>notableDates</c> array into the builder.
    /// </summary>
    /// <param name="array">The notable-dates array, or <see langword="null" /> when absent.</param>
    private void ReadNotableDatesJson(JsonArray? array)
    {
        if (array is null)
            return;

        foreach (JsonNode? node in array)
        {
            if (node is JsonObject definitionJson)
                _definitions.Add(ReadNotableDateJson(definitionJson));
        }
    }

    /// <summary>
    /// Reads a single JSON notable-date object into a concept builder.
    /// </summary>
    /// <param name="definitionJson">The concept object.</param>
    /// <returns>The reconstructed concept builder.</returns>
    private static NotableDateDefinitionBuilder ReadNotableDateJson(JsonObject definitionJson)
    {
        NotableDateDefinitionBuilder definition = new(
            ScalarText(definitionJson, "id") ?? string.Empty,
            ScalarText(definitionJson, "displayName") ?? string.Empty,
            ParseEnum<NotableDateCategory>(ScalarText(definitionJson, "category"), "category"));

        definition.SetParsedValues(
            JsonInt(definitionJson, "defaultDurationDays", "defaultDurationDays"),
            JsonBool(definitionJson, "defaultNonWorkingDay", "defaultNonWorkingDay"),
            ReadTextArray(ArrayProperty(definitionJson, "tags"), "tags"));

        if (ArrayProperty(definitionJson, "rules") is JsonArray rules)
        {
            foreach (JsonNode? node in rules)
            {
                if (node is JsonObject ruleJson)
                    definition.AddRule(ReadRuleJson(ruleJson, definition.Id));
            }
        }

        return definition;
    }

    /// <summary>
    /// Reads a single JSON rule object into a rule builder.
    /// </summary>
    /// <param name="ruleJson">The rule object.</param>
    /// <param name="notableDateId">
    /// The identifier of the concept the rule belongs to, used in exception messages.
    /// </param>
    /// <returns>The reconstructed rule builder.</returns>
    /// <exception cref="FormatException">
    /// The rule's <c>strategy</c> and <c>recurrence</c> name no recognized kind or more than one between them, or its
    /// <c>untilDate</c> declares no end strategy or one that names no recognized kind or more than one.
    /// </exception>
    private static NotableDateRuleBuilder ReadRuleJson(JsonObject ruleJson, string notableDateId)
    {
        NotableDateRuleBuilder rule = new(ScalarText(ruleJson, "id") ?? string.Empty);
        JsonObject? strategyJson = ObjectProperty(ruleJson, "strategy");
        JsonObject? recurrenceJson = ObjectProperty(ruleJson, "recurrence");
        ThrowIfOccurrenceSourceInvalid(strategyJson, recurrenceJson, notableDateId, rule.Id, patch: false);

        JsonObject? applicability = ObjectProperty(ruleJson, "applicability");

        rule.SetParsedScalars(
            JsonInt(ruleJson, "priority", "rule priority"),
            ParseNullableEnum<NotableDateCategory>(ScalarText(ruleJson, "category"), "category"),
            JsonBool(ruleJson, "nonWorking", "rule nonWorking"),
            JsonInt(ruleJson, "durationDays", "durationDays"),
            UnreadText(ruleJson, "comment", "a rule comment that is an object or an array"),
            applicability is null ? null : ParseNullableEnum<CalendarSystem>(ScalarText(applicability, "calendar"), "calendar"),
            applicability is null ? null : JsonInt(applicability, "fromYear", "applicability fromYear"),
            applicability is null ? null : JsonInt(applicability, "toYear", "applicability toYear"),
            applicability is null ? null : JsonInt(applicability, "everyYears", "applicability everyYears"),
            applicability is null ? null : JsonInt(applicability, "anchorYear", "applicability anchorYear"));

        rule.SetParsedCollections(
            applicability is null ? Array.Empty<string>() : ReadTextArray(ArrayProperty(applicability, "territories"), "applicability territories"),
            applicability is null ? Array.Empty<int>() : ReadIntArray(ArrayProperty(applicability, "onlyYears"), "applicability onlyYears"),
            applicability is null ? Array.Empty<int>() : ReadIntArray(ArrayProperty(applicability, "exceptYears"), "applicability exceptYears"),
            ReadTextArray(ArrayProperty(ruleJson, "tags"), "rule tags"),
            ReadTextArray(ArrayProperty(ruleJson, "adjustments"), "rule adjustments"));

        rule.SetParsedStrategy(BuildStrategyElement(strategyJson));

        if (BuildRecurrenceElement(recurrenceJson) is XElement recurrence)
            rule.SetParsedRecurrence(recurrence);

        ReadUntilDateJson(ruleJson, rule, notableDateId);

        return rule;
    }

    /// <summary>
    /// Reads the calculated end-date duration of a JSON rule or <c>PatchRule</c> override, when it declares one, into a
    /// rule builder.
    /// </summary>
    /// <param name="owner">The rule or override object.</param>
    /// <param name="rule">The rule builder that receives the duration.</param>
    /// <param name="notableDateId">
    /// The identifier of the concept the rule belongs to, used in exception messages.
    /// </param>
    /// <exception cref="FormatException">
    /// The <c>untilDate</c> declares no end strategy, or one that names no recognized kind or more than one.
    /// </exception>
    private static void ReadUntilDateJson(JsonObject owner, NotableDateRuleBuilder rule, string notableDateId)
    {
        if (ObjectProperty(owner, "duration") is not JsonObject duration || ObjectProperty(duration, "untilDate") is not JsonObject untilDate)
            return;

        rule.SetParsedDuration(
            BuildEndStrategyElement(untilDate, notableDateId, rule.Id),
            ParseNullableEnum<DateBoundary>(ScalarText(untilDate, "startBoundary"), "untilDate startBoundary") ?? DateBoundary.Inclusive,
            ParseNullableEnum<DateBoundary>(ScalarText(untilDate, "endBoundary"), "untilDate endBoundary") ?? DateBoundary.Inclusive,
            ParseNullableEnum<EndDateSelection>(ScalarText(untilDate, "selection"), "untilDate selection") ?? EndDateSelection.FirstOnOrAfterStart);
    }

    /// <summary>
    /// Builds the end strategy element of a JSON <c>untilDate</c>, which must name exactly one recognized kind, as the
    /// loader requires.
    /// </summary>
    /// <param name="untilDate">The <c>untilDate</c> object.</param>
    /// <param name="notableDateId">
    /// The identifier of the concept the rule belongs to, used in exception messages.
    /// </param>
    /// <param name="ruleId">The identifier of the rule, used in exception messages.</param>
    /// <returns>The end strategy element.</returns>
    /// <exception cref="FormatException">
    /// The <c>untilDate</c> declares no end strategy, which the loader reports with <c>BODU-CAL-DURATION-NOEND</c>, or
    /// one that names no recognized kind, reported the same way, or more than one, which the loader reports with
    /// <c>BODU-CAL-DURATION-CONFLICT</c>.
    /// </exception>
    private static XElement BuildEndStrategyElement(JsonObject untilDate, string notableDateId, string ruleId)
    {
        JsonObject? strategyJson = ObjectProperty(untilDate, "strategy");
        int kinds = CountKinds(strategyJson, s_jsonStrategyKinds.Keys);
        if (kinds == 1 && BuildStrategyElement(strategyJson) is XElement element)
            return element;

        string format = strategyJson is null
            ? CalendarResourceStrings.Validation_DurationEndStrategyMissing
            : kinds == 0
                ? CalendarResourceStrings.Validation_DurationEndStrategyKindMissing
                : CalendarResourceStrings.Validation_DurationEndStrategyConflict;

        throw new FormatException(string.Format(CultureInfo.CurrentCulture, format, notableDateId, ruleId));
    }

    /// <summary>
    /// Reconstructs a <c>Recurrence</c> element from a JSON recurrence object, from its first recognized kind.
    /// </summary>
    /// <param name="recurrenceJson">The recurrence object, or <see langword="null" /> when absent.</param>
    /// <returns>
    /// The reconstructed recurrence element, or <see langword="null" /> when the object is absent or names no
    /// recognized kind.
    /// </returns>
    /// <remarks>
    /// Each attribute's name is matched as the loader matches it, ignoring case, and the attribute is written under its
    /// schema name; a property the loader does not read is ignored, as the loader ignores it.
    /// </remarks>
    private static XElement? BuildRecurrenceElement(JsonObject? recurrenceJson)
    {
        if (recurrenceJson is null)
            return null;

        foreach ((string kind, (string elementName, string[] attributes)) in s_jsonRecurrenceKinds)
        {
            if (ObjectProperty(recurrenceJson, kind) is not JsonObject body)
                continue;

            XElement element = BuildKindElement(elementName, attributes, body);
            if (string.Equals(kind, "weekly", StringComparison.Ordinal) && ArrayProperty(body, "days") is JsonArray days)
            {
                foreach (JsonNode? day in days)
                    element.Add(new XElement(BuilderXml.Namespace + "Day", new XAttribute("dayOfWeek", ParseEnum<DayOfWeek>(ItemText(day), "weekly days").ToString())));
            }

            return new XElement(BuilderXml.Namespace + "Recurrence", element);
        }

        return null;
    }

    /// <summary>
    /// Reads the JSON <c>overrides</c> array into the builder.
    /// </summary>
    /// <param name="array">The overrides array, or <see langword="null" /> when absent.</param>
    private void ReadOverridesJson(JsonArray? array)
    {
        if (array is null)
            return;

        OverrideBuilder overrides = new();
        foreach (JsonNode? node in array)
        {
            if (node is JsonObject overrideJson)
                overrides.AddEntry(ReadOverrideJson(overrideJson));
        }

        _overrides = overrides;
    }

    /// <summary>
    /// Reads a single JSON override object into an override entry.
    /// </summary>
    /// <param name="overrideJson">The override object.</param>
    /// <returns>The reconstructed override entry.</returns>
    /// <exception cref="FormatException">
    /// The override's <c>operation</c> is absent or names no operation, which the loader reports with
    /// <c>BODU-CAL-ENUM</c>, or an <c>AddRule</c> override has no <c>rule</c>, which the loader reports with
    /// <c>BODU-CAL-SCHEMA</c>.
    /// </exception>
    private static OverrideEntry ReadOverrideJson(JsonObject overrideJson)
    {
        string notableDateRef = ScalarText(overrideJson, "notableDateRef") ?? string.Empty;

        // The loader reads an absent operation as the empty name, which names no operation.
        List<NotableDateValidationDiagnostic> diagnostics = new();
        string? operation = NotableDateDocumentParseHelpers.ParseOverrideOperation(ScalarText(overrideJson, "operation") ?? string.Empty, diagnostics);
        ThrowIfMalformed(diagnostics);

        switch (operation)
        {
            case "AddRule":
                if (ObjectProperty(overrideJson, "rule") is not JsonObject ruleJson)
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_SchemaRequiredPropertyMissing, "AddRule", "rule"));

                return new OverrideEntry(OverrideOperation.AddRule, notableDateRef, null, ReadRuleJson(ruleJson, notableDateRef));

            case "RemoveRule":
                return new OverrideEntry(OverrideOperation.RemoveRule, notableDateRef, ScalarText(overrideJson, "ruleRef") ?? string.Empty, null);

            default:
                return ReadPatchOverrideJson(overrideJson, notableDateRef);
        }
    }

    /// <summary>
    /// Reads the flat patch scalars of a JSON patch override into an override entry.
    /// </summary>
    /// <param name="overrideJson">The override object.</param>
    /// <param name="notableDateRef">The identifier of the targeted concept.</param>
    /// <returns>The reconstructed patch override entry.</returns>
    /// <exception cref="FormatException">
    /// The override carries a <c>strategy</c> or a <c>recurrence</c>, and the two name no recognized kind or more than
    /// one, or its <c>untilDate</c> declares no end strategy or one that names no recognized kind or more than one.
    /// </exception>
    private static OverrideEntry ReadPatchOverrideJson(JsonObject overrideJson, string notableDateRef)
    {
        string ruleRef = ScalarText(overrideJson, "ruleRef") ?? string.Empty;
        JsonObject? strategyJson = ObjectProperty(overrideJson, "strategy");
        JsonObject? recurrenceJson = ObjectProperty(overrideJson, "recurrence");
        ThrowIfOccurrenceSourceInvalid(strategyJson, recurrenceJson, notableDateRef, ruleRef, patch: true);

        NotableDateRuleBuilder rule = new(ruleRef);
        JsonObject? applicability = ObjectProperty(overrideJson, "applicability");

        // A patch that carries an applicability replaces the rule's whole applicability, an empty one included, so a
        // present applicability always declares its calendar: the default, Gregorian, when the patch names none.
        rule.SetParsedScalars(
            JsonInt(overrideJson, "priority", "patch priority"),
            ParseNullableEnum<NotableDateCategory>(ScalarText(overrideJson, "category"), "category"),
            JsonBool(overrideJson, "nonWorking", "patch nonWorking"),
            JsonInt(overrideJson, "durationDays", "durationDays"),
            null,
            applicability is null ? null : ParseNullableEnum<CalendarSystem>(ScalarText(applicability, "calendar"), "calendar") ?? CalendarSystem.Gregorian,
            applicability is null ? null : JsonInt(applicability, "fromYear", "applicability fromYear"),
            applicability is null ? null : JsonInt(applicability, "toYear", "applicability toYear"),
            applicability is null ? null : JsonInt(applicability, "everyYears", "applicability everyYears"),
            applicability is null ? null : JsonInt(applicability, "anchorYear", "applicability anchorYear"));

        rule.SetParsedCollections(
            applicability is null ? Array.Empty<string>() : ReadTextArray(ArrayProperty(applicability, "territories"), "applicability territories"),
            applicability is null ? Array.Empty<int>() : ReadIntArray(ArrayProperty(applicability, "onlyYears"), "applicability onlyYears"),
            applicability is null ? Array.Empty<int>() : ReadIntArray(ArrayProperty(applicability, "exceptYears"), "applicability exceptYears"),
            ReadPatchListJson(ArrayProperty(overrideJson, "tags"), "patch tags", "an empty tags list on a PatchRule override"),
            ReadPatchListJson(ArrayProperty(overrideJson, "adjustments"), "patch adjustments", "an empty adjustments list on a PatchRule override"));

        rule.SetParsedStrategy(BuildStrategyElement(strategyJson));

        if (BuildRecurrenceElement(recurrenceJson) is XElement recurrence)
            rule.SetParsedRecurrence(recurrence);

        ReadUntilDateJson(overrideJson, rule, notableDateRef);

        return new OverrideEntry(OverrideOperation.PatchRule, notableDateRef, ruleRef, rule);
    }

    /// <summary>
    /// Rejects a rule that does not name exactly one occurrence source, or a <c>PatchRule</c> override that replaces
    /// the rule's occurrence source without naming exactly one, as the loader rejects them: a rule's <c>strategy</c>
    /// and <c>recurrence</c>, and a patch's when either is present, must name one recognized kind between them.
    /// </summary>
    /// <param name="strategyJson">The strategy object, or <see langword="null" /> when absent.</param>
    /// <param name="recurrenceJson">The recurrence object, or <see langword="null" /> when absent.</param>
    /// <param name="notableDateId">The identifier of the concept, used in the exception message.</param>
    /// <param name="ruleId">The identifier of the rule, used in the exception message.</param>
    /// <param name="patch">
    /// <see langword="true" /> when the objects belong to a <c>PatchRule</c> override rather than a rule, which leaves
    /// the occurrence source unchanged when it carries neither and selects the override's wording.
    /// </param>
    /// <exception cref="FormatException">
    /// The rule's strategy and recurrence, or the patch's when either is present, name no recognized kind or more than
    /// one between them.
    /// </exception>
    private static void ThrowIfOccurrenceSourceInvalid(JsonObject? strategyJson, JsonObject? recurrenceJson, string notableDateId, string ruleId, bool patch)
    {
        if (patch && strategyJson is null && recurrenceJson is null)
            return;

        int sources = CountKinds(strategyJson, s_jsonStrategyKinds.Keys) + CountKinds(recurrenceJson, s_jsonRecurrenceKinds.Keys);
        if (sources == 1)
            return;

        string format = (sources == 0, patch) switch
        {
            (true, false) => CalendarResourceStrings.Validation_OccurrenceSourceMissing,
            (false, false) => CalendarResourceStrings.Validation_OccurrenceSourceConflict,
            (true, true) => CalendarResourceStrings.Validation_PatchOccurrenceSourceMissing,
            (false, true) => CalendarResourceStrings.Validation_PatchOccurrenceSourceConflict,
        };

        throw new FormatException(string.Format(CultureInfo.CurrentCulture, format, notableDateId, ruleId));
    }

    /// <summary>
    /// Counts the recognized kinds a JSON strategy or recurrence object names, as the loader counts them.
    /// </summary>
    /// <param name="source">The strategy or recurrence object, or <see langword="null" /> when absent.</param>
    /// <param name="kinds">The recognized kind names.</param>
    /// <returns>The number of recognized kinds present; zero when <paramref name="source" /> is absent.</returns>
    private static int CountKinds(JsonObject? source, IEnumerable<string> kinds) =>
        source is null ? 0 : kinds.Count(kind => ObjectProperty(source, kind) is not null);

    /// <summary>
    /// Reads a list that replaces a patched rule's or an imported concept's list, which the builder can hold only when
    /// it is absent or has items: the document's XML form has no empty replacement list.
    /// </summary>
    /// <param name="array">The list, or <see langword="null" /> when absent.</param>
    /// <param name="field">
    /// The name of the field the list belongs to, used in the exception message for an item.
    /// </param>
    /// <param name="feature">A description of the list, used in the exception message when it is empty.</param>
    /// <returns>The list's values; empty when the list is absent.</returns>
    /// <exception cref="NotSupportedException">The list is present and empty.</exception>
    /// <exception cref="FormatException">An item is not a string.</exception>
    private static List<string> ReadPatchListJson(JsonArray? array, string field, string feature) =>
        array is { Count: 0 } ? throw UnrepresentableJsonField(feature) : ReadTextArray(array, field);

    /// <summary>
    /// Creates a <see cref="NotSupportedException" /> describing a field of a JSON document that the builder's document
    /// model cannot represent.
    /// </summary>
    /// <param name="feature">A description of the field.</param>
    /// <returns>The exception to throw.</returns>
    private static NotSupportedException UnrepresentableJsonField(string feature) =>
        new(string.Format(CultureInfo.CurrentCulture, BuilderResourceStrings.Op_NotSupported_JsonFieldNotRepresentable, feature));

    /// <summary>
    /// Reads the JSON <c>imports</c> array into the builder.
    /// </summary>
    /// <param name="array">The imports array, or <see langword="null" /> when absent.</param>
    /// <exception cref="NotSupportedException">A use declares an empty <c>adjustments</c> list.</exception>
    private void ReadImportsJson(JsonArray? array)
    {
        if (array is null)
            return;

        foreach (JsonNode? node in array)
        {
            if (node is not JsonObject importJson)
                continue;

            ImportBuilder import = new(ScalarText(importJson, "resource") ?? string.Empty);
            if (ArrayProperty(importJson, "uses") is JsonArray uses)
            {
                foreach (JsonNode? useNode in uses)
                {
                    if (useNode is JsonObject useJson)
                        import.AddUse(ReadImportUseJson(useJson));
                }
            }

            _imports.Add(import);
        }
    }

    /// <summary>
    /// Reads a single JSON import use into an import-use builder.
    /// </summary>
    /// <param name="useJson">The use object.</param>
    /// <returns>The reconstructed import-use builder.</returns>
    /// <exception cref="NotSupportedException">The use declares an empty <c>adjustments</c> list.</exception>
    private static ImportUseBuilder ReadImportUseJson(JsonObject useJson)
    {
        ImportUseBuilder use = new(ScalarText(useJson, "notableDateRef") ?? string.Empty);
        use.SetParsedValues(
            ScalarText(useJson, "as"),
            ScalarText(useJson, "territory"),
            ParseNullableEnum<NotableDateCategory>(ScalarText(useJson, "category"), "import use category"),
            JsonBool(useJson, "nonWorking", "import use nonWorking"),
            ReadPatchListJson(ArrayProperty(useJson, "adjustments"), "import use adjustments", "an empty adjustments list on an import use"));

        return use;
    }

    /// <summary>
    /// Builds an XML strategy element from a JSON strategy object, from its first recognized kind.
    /// </summary>
    /// <param name="strategyJson">The strategy object, or <see langword="null" /> when absent.</param>
    /// <returns>
    /// The reconstructed strategy element, or <see langword="null" /> when the object is absent or names no recognized
    /// kind.
    /// </returns>
    /// <remarks>
    /// Each attribute's name is matched as the loader matches it, ignoring case, and the attribute is written under its
    /// schema name; a property the loader does not read is ignored, as the loader ignores it.
    /// </remarks>
    private static XElement? BuildStrategyElement(JsonObject? strategyJson)
    {
        if (strategyJson is null)
            return null;

        foreach ((string kind, (string elementName, string[] attributes)) in s_jsonStrategyKinds)
        {
            if (ObjectProperty(strategyJson, kind) is JsonObject body)
                return BuildKindElement(elementName, attributes, body);
        }

        return null;
    }

    /// <summary>
    /// Builds the XML element of a strategy or recurrence kind from its JSON body, writing each attribute the loader
    /// reads under its schema name, in the order the body holds them.
    /// </summary>
    /// <param name="elementName">The local name of the kind's XML element.</param>
    /// <param name="attributes">The schema names of the attributes the loader reads from the kind's body.</param>
    /// <param name="body">The kind's JSON body.</param>
    /// <returns>The kind's XML element.</returns>
    /// <remarks>
    /// An attribute takes its value from the first property that bears its name, ignoring case, and is not the JSON
    /// literal <c>null</c>, as the loader reads it; any other property is ignored, as the loader ignores it.
    /// </remarks>
    private static XElement BuildKindElement(string elementName, string[] attributes, JsonObject body)
    {
        XElement element = new(BuilderXml.Namespace + elementName);
        foreach (KeyValuePair<string, JsonNode?> property in body)
        {
            string? attribute = Array.Find(attributes, name => string.Equals(name, property.Key, StringComparison.OrdinalIgnoreCase));
            if (property.Value is not null && attribute is not null && element.Attribute(attribute) is null)
                element.SetAttributeValue(attribute, ConvertStrategyAttribute(attribute, property.Value));
        }

        return element;
    }

    /// <summary>
    /// Converts a JSON strategy attribute value into the string form expected by the XML schema, normalizing months and
    /// Boolean literals.
    /// </summary>
    /// <param name="name">The attribute's schema name.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The string form of the attribute value.</returns>
    private static string ConvertStrategyAttribute(string name, JsonNode value)
    {
        if (string.Equals(name, "month", StringComparison.Ordinal) && value.GetValueKind() == JsonValueKind.Number)
            return BuilderXml.GetMonthName((int)value);

        return value.GetValueKind() switch
        {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => ((int)value).ToString(CultureInfo.InvariantCulture),
            _ => ReadScalar(value, name) ?? string.Empty,
        };
    }

    /// <summary>
    /// Reads a JSON list of identifiers, territory codes or tags with the loader's rules: each item must be a string.
    /// </summary>
    /// <param name="array">The array, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the array belongs to, used in the exception message.</param>
    /// <returns>The string values; empty when the array is absent.</returns>
    /// <exception cref="FormatException">An item is not a string.</exception>
    private static List<string> ReadTextArray(JsonArray? array, string field)
    {
        List<string> values = new();
        if (array is null)
            return values;

        foreach (JsonNode? node in array)
        {
            if (node is JsonValue value && value.GetValueKind() == JsonValueKind.String)
                values.Add(value.GetValue<string>());
            else
                throw new FormatException(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidTextItem, field, node?.ToJsonString() ?? "null"));
        }

        return values;
    }

    /// <summary>
    /// Reads a JSON list of enumeration names as the loader reads it: a string item by its value, and any other item by
    /// its JSON text, so the name parse that follows rejects it rather than the list losing it.
    /// </summary>
    /// <param name="array">The array, or <see langword="null" /> when absent.</param>
    /// <returns>The item texts; empty when the array is absent.</returns>
    private static List<string> ReadNameArray(JsonArray? array)
    {
        List<string> values = new();
        if (array is null)
            return values;

        foreach (JsonNode? node in array)
            values.Add(ItemText(node));

        return values;
    }

    /// <summary>
    /// Reads a JSON list of text the loader does not read, accepting the scalars a text field accepts.
    /// </summary>
    /// <param name="array">The array, or <see langword="null" /> when absent.</param>
    /// <param name="feature">
    /// A description of an item that is an object or an array, used in the exception message.
    /// </param>
    /// <returns>The item texts, without the <c>null</c> items; empty when the array is absent.</returns>
    /// <exception cref="NotSupportedException">An item is an object or an array.</exception>
    private static List<string> ReadUnreadTextArray(JsonArray? array, string feature)
    {
        List<string> values = new();
        if (array is null)
            return values;

        foreach (JsonNode? node in array)
        {
            if (UnreadValueText(node, feature) is string text)
                values.Add(text);
        }

        return values;
    }

    /// <summary>
    /// Returns the text of a JSON list item as the loader reads a name: a string by its value, and any other item, a
    /// <c>null</c> among them, by its JSON text.
    /// </summary>
    /// <param name="node">The item, or <see langword="null" /> for a JSON <c>null</c>.</param>
    /// <returns>The item's text.</returns>
    private static string ItemText(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.String
            ? value.GetValue<string>()
            : node?.ToJsonString() ?? "null";

    /// <summary>
    /// Reads a JSON array of integers with the loader's rules: each item is read as an integer property is, so a number
    /// in quotes is accepted and any other item that is not a 32-bit integer is rejected.
    /// </summary>
    /// <param name="array">The array, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the array belongs to, used in the exception message.</param>
    /// <returns>The integer values; empty when the array is absent.</returns>
    /// <exception cref="FormatException">An item is not a 32-bit integer.</exception>
    private static List<int> ReadIntArray(JsonArray? array, string field)
    {
        List<int> values = new();
        if (array is null)
            return values;

        foreach (JsonNode? node in array)
        {
            // The loader reads an item that is not a string by its JSON text, a null among them.
            if (ParseNullableInt(ItemText(node), field) is int parsed)
                values.Add(parsed);
        }

        return values;
    }

    /// <summary>
    /// Reads an optional JSON integer property with the loader's rules: a number, or a number in quotes, that is a
    /// 32-bit integer.
    /// </summary>
    /// <param name="owner">The object holding the property.</param>
    /// <param name="name">The property's name, matched ignoring case.</param>
    /// <param name="field">The name of the field the value belongs to, used in the exception message.</param>
    /// <returns>The integer, or <see langword="null" /> when the property is absent.</returns>
    /// <exception cref="FormatException">The value is not a 32-bit integer, or is an object or an array.</exception>
    private static int? JsonInt(JsonObject owner, string name, string field) =>
        ParseNullableInt(ScalarText(owner, name), field);

    /// <summary>
    /// Reads an optional JSON Boolean property with the loader's rules: a Boolean, or an XML-schema Boolean literal as
    /// text or a number (<c>true</c>, <c>false</c>, <c>1</c> or <c>0</c>).
    /// </summary>
    /// <param name="owner">The object holding the property.</param>
    /// <param name="name">The property's name, matched ignoring case.</param>
    /// <param name="field">The name of the field the value belongs to, used in the exception message.</param>
    /// <returns>The Boolean, or <see langword="null" /> when the property is absent.</returns>
    /// <exception cref="FormatException">The value is not a Boolean, or is an object or an array.</exception>
    private static bool? JsonBool(JsonObject owner, string name, string field) =>
        ParseNullableBool(ScalarText(owner, name), field);

    /// <summary>
    /// Reads a JSON scalar property as the loader does: a string verbatim, a number by its invariant digits, and a
    /// Boolean as <c>true</c> or <c>false</c>.
    /// </summary>
    /// <param name="owner">The object holding the property.</param>
    /// <param name="name">The property's name, matched ignoring case.</param>
    /// <returns>The scalar's text, or <see langword="null" /> when the property is absent.</returns>
    /// <exception cref="FormatException">
    /// The value is an object or an array, which the loader reports with <c>BODU-CAL-VALUE</c>.
    /// </exception>
    private static string? ScalarText(JsonObject owner, string name) =>
        ReadScalar(Property(owner, name), name);

    /// <summary>
    /// Reads the value of a JSON scalar property as the loader does: a string verbatim, a number by its invariant
    /// digits, and a Boolean as <c>true</c> or <c>false</c>.
    /// </summary>
    /// <param name="value">The property's value, or <see langword="null" /> when the property is absent.</param>
    /// <param name="name">The property's name, used in the exception message.</param>
    /// <returns>The scalar's text, or <see langword="null" /> when the value is absent.</returns>
    /// <exception cref="FormatException">
    /// The value is an object or an array, which the loader reports with <c>BODU-CAL-VALUE</c>.
    /// </exception>
    private static string? ReadScalar(JsonNode? value, string name) =>
        value switch
        {
            null => null,
            JsonValue scalar => ScalarValueText(scalar),
            _ => throw new FormatException(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidScalarValue, name, value.ToJsonString())),
        };

    /// <summary>
    /// Returns the text of a JSON scalar as the loader reads it: a string verbatim, a number by its invariant digits,
    /// and a Boolean as <c>true</c> or <c>false</c>.
    /// </summary>
    /// <param name="value">The scalar.</param>
    /// <returns>The scalar's text.</returns>
    private static string? ScalarValueText(JsonValue value) =>
        value.GetValueKind() switch
        {
            JsonValueKind.String => value.GetValue<string>(),
            JsonValueKind.Number => value.TryGetValue(out int number) ? number.ToString(CultureInfo.InvariantCulture) : value.ToJsonString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };

    /// <summary>
    /// Reads a text property the loader does not read, accepting the scalars a text field accepts: a string verbatim, a
    /// number by its invariant digits, and a Boolean as <c>true</c> or <c>false</c>.
    /// </summary>
    /// <param name="owner">The object holding the property.</param>
    /// <param name="name">The property's name, matched ignoring case.</param>
    /// <param name="feature">
    /// A description of the property holding an object or an array, used in the exception message.
    /// </param>
    /// <returns>The scalar's text, or <see langword="null" /> when the property is absent.</returns>
    /// <exception cref="NotSupportedException">
    /// The value is an object or an array, which the builder's document model, holding the field as text, cannot hold.
    /// </exception>
    private static string? UnreadText(JsonObject owner, string name, string feature) =>
        UnreadValueText(Property(owner, name), feature);

    /// <summary>
    /// Reads a text value the loader does not read, accepting the scalars a text field accepts.
    /// </summary>
    /// <param name="value">The value, or <see langword="null" /> when absent.</param>
    /// <param name="feature">
    /// A description of the field holding an object or an array, used in the exception message.
    /// </param>
    /// <returns>The scalar's text, or <see langword="null" /> when the value is absent.</returns>
    /// <exception cref="NotSupportedException">
    /// The value is an object or an array, which the builder's document model, holding the field as text, cannot hold.
    /// </exception>
    private static string? UnreadValueText(JsonNode? value, string feature) =>
        value switch
        {
            null => null,
            JsonValue scalar => ScalarValueText(scalar),
            _ => throw UnrepresentableJsonField(feature),
        };

    /// <summary>
    /// Finds a property of a JSON object as the loader finds it: the first property whose name matches ignoring case
    /// and whose value is not the JSON literal <c>null</c>.
    /// </summary>
    /// <param name="owner">The object holding the property.</param>
    /// <param name="name">The property's name.</param>
    /// <returns>The property's value, or <see langword="null" /> when the property is absent.</returns>
    private static JsonNode? Property(JsonObject owner, string name)
    {
        foreach (KeyValuePair<string, JsonNode?> property in owner)
        {
            if (property.Value is not null && string.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        return null;
    }

    /// <summary>
    /// Finds a property of a JSON object that must hold an object, as the loader finds it.
    /// </summary>
    /// <param name="owner">The object holding the property.</param>
    /// <param name="name">The property's name, matched ignoring case.</param>
    /// <returns>The property's object, or <see langword="null" /> when the property is absent.</returns>
    /// <exception cref="FormatException">
    /// The property holds a value that is not an object, which the loader reports with <c>BODU-CAL-VALUE</c>.
    /// </exception>
    private static JsonObject? ObjectProperty(JsonObject owner, string name) =>
        Property(owner, name) switch
        {
            null => null,
            JsonObject value => value,
            JsonNode value => throw WrongKind(CalendarResourceStrings.Validation_InvalidObjectValue, name, value),
        };

    /// <summary>
    /// Finds a property of a JSON object that must hold an array, as the loader finds it.
    /// </summary>
    /// <param name="owner">The object holding the property.</param>
    /// <param name="name">The property's name, matched ignoring case.</param>
    /// <returns>The property's array, or <see langword="null" /> when the property is absent.</returns>
    /// <exception cref="FormatException">
    /// The property holds a value that is not an array, which the loader reports with <c>BODU-CAL-VALUE</c>.
    /// </exception>
    private static JsonArray? ArrayProperty(JsonObject owner, string name) =>
        Property(owner, name) switch
        {
            null => null,
            JsonArray value => value,
            JsonNode value => throw WrongKind(CalendarResourceStrings.Validation_InvalidListValue, name, value),
        };

    /// <summary>
    /// Creates the <see cref="FormatException" /> for a property holding a value of the wrong kind, carrying the
    /// message of the <c>BODU-CAL-VALUE</c> diagnostic the loader reports for it.
    /// </summary>
    /// <param name="format">The resource format string, taking the property's name and the kind of its value.</param>
    /// <param name="name">The property's name.</param>
    /// <param name="value">The property's value.</param>
    /// <returns>The exception to throw.</returns>
    private static FormatException WrongKind(string format, string name, JsonNode value) =>
        new(string.Format(CultureInfo.CurrentCulture, format, name, JsonDocumentNode.KindName(value.GetValueKind())));
}
