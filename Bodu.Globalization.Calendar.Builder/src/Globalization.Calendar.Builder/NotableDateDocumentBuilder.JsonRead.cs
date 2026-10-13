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
    /// <summary>Maps each JSON strategy property name, matched ignoring case as the loader matches it, to the corresponding XML strategy element local name.</summary>
    private static readonly Dictionary<string, string> s_jsonStrategyElementNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fixed"] = "Fixed",
        ["dayOfWeekInMonth"] = "DayOfWeekInMonth",
        ["weekdayNearDate"] = "WeekdayNearDate",
        ["relativeWeekdayInMonth"] = "RelativeWeekdayInMonth",
        ["offsetFromRule"] = "OffsetFromRule",
        ["algorithm"] = "Algorithm",
        ["weekdayNearRule"] = "WeekdayNearRule",
        ["nthWeekdayFromRule"] = "NthWeekdayFromRule",
        ["workingDayOffsetFromRule"] = "WorkingDayOffsetFromRule",
        ["workingDayInMonth"] = "WorkingDayInMonth",
        ["ordinalDayOfMonth"] = "OrdinalDayOfMonth",
        ["isoWeekDate"] = "IsoWeekDate",
        ["dayOfYear"] = "DayOfYear",
    };

    /// <summary>Maps each JSON recurrence property name, matched ignoring case as the loader matches it, to the corresponding XML recurrence element local name.</summary>
    private static readonly Dictionary<string, string> s_jsonRecurrenceElementNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dailyInterval"] = "DailyInterval",
        ["weekly"] = "Weekly",
        ["monthlyDay"] = "MonthlyDay",
        ["monthlyWeekday"] = "MonthlyWeekday",
    };

    /// <summary>
    /// Reads the JSON <c>metadata</c> object into the builder.
    /// </summary>
    /// <param name="metadata">The metadata object, or <see langword="null" /> when absent.</param>
    private void ReadMetadataJson(JsonObject? metadata)
    {
        if (metadata is null)
            return;

        _metadataName = UnreadText(metadata["name"], "a metadata name that is an object or an array");
        _metadataDescription = UnreadText(metadata["description"], "a metadata description that is an object or an array");
        _sources.Clear();
        _sources.AddRange(ReadUnreadTextArray(metadata["sources"] as JsonArray, "a metadata source that is an object or an array"));
    }

    /// <summary>
    /// Reads the JSON <c>resolutionPolicy</c> object into the builder.
    /// </summary>
    /// <param name="policyJson">The resolution-policy object, or <see langword="null" /> when absent.</param>
    private void ReadResolutionPolicyJson(JsonObject? policyJson)
    {
        if (policyJson is null)
            return;

        DayOfWeekSet? workingWeek = ParseWorkingWeek(ScalarText(policyJson["workingDays"]));

        IReadOnlyList<NotableDateCategory>? categoryPrecedence = policyJson["categoryPrecedence"] is not JsonArray array
            ? null
            : [.. ReadNameArray(array).Select(item => ParseEnum<NotableDateCategory>(item, "categoryPrecedence"))];

        ResolutionPolicyBuilder policy = new();
        policy.SetParsedValues(
            ParseNullableEnum<DuplicatePolicy>(ScalarText(policyJson["duplicatePolicy"]), "duplicatePolicy"),
            ParseNullableEnum<CollisionPolicy>(ScalarText(policyJson["sameDayCollisionPolicy"]), "sameDayCollisionPolicy"),
            ParseNullableEnum<CollisionPolicy>(ScalarText(policyJson["spanCollisionPolicy"]), "spanCollisionPolicy"),
            ParseNullableEnum<PriorityDirection>(ScalarText(policyJson["priorityDirection"]), "priorityDirection"),
            ParseNullableEnum<ObservedDateRangePolicy>(ScalarText(policyJson["observedDateRangePolicy"]), "observedDateRangePolicy"),
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
        AdjustmentPolicyBuilder policy = new(ScalarText(policyJson["id"]) ?? string.Empty);
        policy.SetParsedHeader(
            JsonInt(policyJson["priority"], "adjustment policy priority"),
            UnreadText(policyJson["description"], "an adjustment policy description that is an object or an array"),
            ReadScopeJson(policyJson["scope"] as JsonObject));

        if (policyJson["trigger"] is JsonObject trigger)
        {
            policy.SetParsedTrigger(
                ParseNullableEnum<AdjustmentTrigger>(ScalarText(trigger["type"]), "trigger type"),
                ReadNameArray(trigger["weekdays"] as JsonArray).Select(day => ParseEnum<DayOfWeek>(day, "trigger weekdays")),
                ParseTriggerMonth(ScalarText(trigger["month"]), "trigger month"),
                JsonInt(trigger["day"], "trigger day"),
                ParseNullableEnum<WeekOrdinal>(ScalarText(trigger["weekOrdinal"]), "trigger weekOrdinal"),
                ScalarText(trigger["handlerKey"]));
        }

        if (policyJson["action"] is JsonObject action)
        {
            policy.SetParsedAction(
                ParseNullableEnum<AdjustmentAction>(ScalarText(action["type"]), "action type"),
                JsonInt(action["days"], "action days"),
                ParseNullableEnum<DayOfWeek>(ScalarText(action["dayOfWeek"]), "action dayOfWeek"),
                JsonInt(action["maxSearchDays"], "action maxSearchDays"),
                JsonBool(action["skipWeekends"], "action skipWeekends"),
                JsonBool(action["skipNonWorkingDates"], "action skipNonWorkingDates"),
                ScalarText(action["notableDateRef"]),
                ScalarText(action["ruleRef"]),
                ScalarText(action["handlerKey"]));
        }

        if (policyJson["emission"] is JsonObject emission)
        {
            policy.SetParsedEmission(
                ParseNullableEnum<EmissionMode>(ScalarText(emission["mode"]), "emission mode"),
                ScalarText(emission["reason"]),
                JsonBool(emission["nonWorking"], "emission nonWorking"));
        }

        if (policyJson["parameters"] is JsonObject parameters)
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
            ReadTextArray(scopeJson["territories"] as JsonArray, "scope territories"),
            ReadNameArray(scopeJson["calendars"] as JsonArray).Select(calendar => ParseEnum<CalendarSystem>(calendar, "scope calendars")),
            ReadNameArray(scopeJson["categories"] as JsonArray).Select(category => ParseEnum<NotableDateCategory>(category, "scope categories")),
            ReadTextArray(scopeJson["notableDateRefs"] as JsonArray, "scope notableDateRefs"),
            ReadScopeRuleRefsJson(scopeJson["ruleRefs"] as JsonArray),
            ReadIntArray(scopeJson["onlyYears"] as JsonArray, "scope onlyYears"),
            ReadIntArray(scopeJson["exceptYears"] as JsonArray, "scope exceptYears"),
            JsonInt(scopeJson["fromYear"], "scope fromYear"),
            JsonInt(scopeJson["toYear"], "scope toYear"));

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
            if (node is JsonObject reference && ScalarText(reference["notableDateRef"]) is string notableDateRef)
                references.Add((notableDateRef, ScalarText(reference["ruleRef"]) ?? string.Empty));
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
            ScalarText(definitionJson["id"]) ?? string.Empty,
            ScalarText(definitionJson["displayName"]) ?? string.Empty,
            ParseEnum<NotableDateCategory>(ScalarText(definitionJson["category"]), "category"));

        definition.SetParsedValues(
            JsonInt(definitionJson["defaultDurationDays"], "defaultDurationDays"),
            JsonBool(definitionJson["defaultNonWorkingDay"], "defaultNonWorkingDay"),
            ReadTextArray(definitionJson["tags"] as JsonArray, "tags"));

        if (definitionJson["rules"] is JsonArray rules)
        {
            foreach (JsonNode? node in rules)
            {
                if (node is JsonObject ruleJson)
                    definition.AddRule(ReadRuleJson(ruleJson));
            }
        }

        return definition;
    }

    /// <summary>
    /// Reads a single JSON rule object into a rule builder.
    /// </summary>
    /// <param name="ruleJson">The rule object.</param>
    /// <returns>The reconstructed rule builder.</returns>
    private static NotableDateRuleBuilder ReadRuleJson(JsonObject ruleJson)
    {
        NotableDateRuleBuilder rule = new(ScalarText(ruleJson["id"]) ?? string.Empty);
        var applicability = ruleJson["applicability"] as JsonObject;

        rule.SetParsedScalars(
            JsonInt(ruleJson["priority"], "rule priority"),
            ParseNullableEnum<NotableDateCategory>(ScalarText(ruleJson["category"]), "category"),
            JsonBool(ruleJson["nonWorking"], "rule nonWorking"),
            JsonInt(ruleJson["durationDays"], "durationDays"),
            UnreadText(ruleJson["comment"], "a rule comment that is an object or an array"),
            applicability is null ? null : ParseNullableEnum<CalendarSystem>(ScalarText(applicability["calendar"]), "calendar"),
            applicability is null ? null : JsonInt(applicability["fromYear"], "applicability fromYear"),
            applicability is null ? null : JsonInt(applicability["toYear"], "applicability toYear"),
            applicability is null ? null : JsonInt(applicability["everyYears"], "applicability everyYears"),
            applicability is null ? null : JsonInt(applicability["anchorYear"], "applicability anchorYear"));

        rule.SetParsedCollections(
            applicability is null ? Array.Empty<string>() : ReadTextArray(applicability["territories"] as JsonArray, "applicability territories"),
            applicability is null ? Array.Empty<int>() : ReadIntArray(applicability["onlyYears"] as JsonArray, "applicability onlyYears"),
            applicability is null ? Array.Empty<int>() : ReadIntArray(applicability["exceptYears"] as JsonArray, "applicability exceptYears"),
            ReadTextArray(ruleJson["tags"] as JsonArray, "rule tags"),
            ReadTextArray(ruleJson["adjustments"] as JsonArray, "rule adjustments"));

        rule.SetParsedStrategy(BuildStrategyElement(ruleJson["strategy"] as JsonObject));

        if (BuildRecurrenceElement(ruleJson["recurrence"] as JsonObject) is XElement recurrence)
            rule.SetParsedRecurrence(recurrence);

        if ((ruleJson["duration"] as JsonObject)?["untilDate"] is JsonObject untilDate)
        {
            rule.SetParsedDuration(
                BuildStrategyElement(untilDate["strategy"] as JsonObject),
                ParseNullableEnum<DateBoundary>(ScalarText(untilDate["startBoundary"]), "untilDate startBoundary") ?? DateBoundary.Inclusive,
                ParseNullableEnum<DateBoundary>(ScalarText(untilDate["endBoundary"]), "untilDate endBoundary") ?? DateBoundary.Inclusive,
                ParseNullableEnum<EndDateSelection>(ScalarText(untilDate["selection"]), "untilDate selection") ?? EndDateSelection.FirstOnOrAfterStart);
        }

        return rule;
    }

    /// <summary>
    /// Reconstructs a <c>Recurrence</c> element from a JSON recurrence object.
    /// </summary>
    /// <param name="recurrenceJson">The recurrence object, or <see langword="null" /> when absent.</param>
    /// <returns>The reconstructed recurrence element, or <see langword="null" /> when absent or empty.</returns>
    private static XElement? BuildRecurrenceElement(JsonObject? recurrenceJson)
    {
        if (recurrenceJson is null)
            return null;

        foreach (KeyValuePair<string, JsonNode?> property in recurrenceJson)
        {
            if (property.Value is not JsonObject body || !s_jsonRecurrenceElementNames.TryGetValue(property.Key, out string? elementName))
                continue;

            XElement kind = new(BuilderXml.Namespace + elementName);
            foreach (KeyValuePair<string, JsonNode?> attribute in body)
            {
                if (string.Equals(attribute.Key, "days", StringComparison.Ordinal) && attribute.Value is JsonArray days)
                {
                    foreach (JsonNode? day in days)
                        kind.Add(new XElement(BuilderXml.Namespace + "Day", new XAttribute("dayOfWeek", ParseEnum<DayOfWeek>(ItemText(day), "weekly days").ToString())));
                }
                else
                {
                    kind.SetAttributeValue(attribute.Key, ConvertStrategyAttribute(attribute.Key, attribute.Value));
                }
            }

            return new XElement(BuilderXml.Namespace + "Recurrence", kind);
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
    private static OverrideEntry ReadOverrideJson(JsonObject overrideJson)
    {
        string notableDateRef = ScalarText(overrideJson["notableDateRef"]) ?? string.Empty;
        string operation = ScalarText(overrideJson["operation"]) ?? string.Empty;

        switch (operation)
        {
            case "AddRule":
                NotableDateRuleBuilder addRule = overrideJson["rule"] is JsonObject ruleJson
                    ? ReadRuleJson(ruleJson)
                    : new NotableDateRuleBuilder(string.Empty);
                return new OverrideEntry(OverrideOperation.AddRule, notableDateRef, null, addRule);

            case "RemoveRule":
                return new OverrideEntry(OverrideOperation.RemoveRule, notableDateRef, ScalarText(overrideJson["ruleRef"]) ?? string.Empty, null);

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
    /// one.
    /// </exception>
    private static OverrideEntry ReadPatchOverrideJson(JsonObject overrideJson, string notableDateRef)
    {
        string ruleRef = ScalarText(overrideJson["ruleRef"]) ?? string.Empty;
        var strategyJson = overrideJson["strategy"] as JsonObject;
        var recurrenceJson = overrideJson["recurrence"] as JsonObject;
        ThrowIfPatchOccurrenceSourceInvalid(strategyJson, recurrenceJson, notableDateRef, ruleRef);

        NotableDateRuleBuilder rule = new(ruleRef);
        var applicability = overrideJson["applicability"] as JsonObject;

        // A patch that carries an applicability replaces the rule's whole applicability, an empty one included, so a
        // present applicability always declares its calendar: the default, Gregorian, when the patch names none.
        rule.SetParsedScalars(
            JsonInt(overrideJson["priority"], "patch priority"),
            ParseNullableEnum<NotableDateCategory>(ScalarText(overrideJson["category"]), "category"),
            JsonBool(overrideJson["nonWorking"], "patch nonWorking"),
            JsonInt(overrideJson["durationDays"], "durationDays"),
            null,
            applicability is null ? null : ParseNullableEnum<CalendarSystem>(ScalarText(applicability["calendar"]), "calendar") ?? CalendarSystem.Gregorian,
            applicability is null ? null : JsonInt(applicability["fromYear"], "applicability fromYear"),
            applicability is null ? null : JsonInt(applicability["toYear"], "applicability toYear"),
            applicability is null ? null : JsonInt(applicability["everyYears"], "applicability everyYears"),
            applicability is null ? null : JsonInt(applicability["anchorYear"], "applicability anchorYear"));

        rule.SetParsedCollections(
            applicability is null ? Array.Empty<string>() : ReadTextArray(applicability["territories"] as JsonArray, "applicability territories"),
            applicability is null ? Array.Empty<int>() : ReadIntArray(applicability["onlyYears"] as JsonArray, "applicability onlyYears"),
            applicability is null ? Array.Empty<int>() : ReadIntArray(applicability["exceptYears"] as JsonArray, "applicability exceptYears"),
            ReadPatchListJson(overrideJson["tags"] as JsonArray, "patch tags", "an empty tags list on a PatchRule override"),
            ReadPatchListJson(overrideJson["adjustments"] as JsonArray, "patch adjustments", "an empty adjustments list on a PatchRule override"));

        rule.SetParsedStrategy(BuildStrategyElement(strategyJson));

        if (BuildRecurrenceElement(recurrenceJson) is XElement recurrence)
            rule.SetParsedRecurrence(recurrence);

        if ((overrideJson["duration"] as JsonObject)?["untilDate"] is JsonObject untilDate)
        {
            rule.SetParsedDuration(
                BuildStrategyElement(untilDate["strategy"] as JsonObject),
                ParseNullableEnum<DateBoundary>(ScalarText(untilDate["startBoundary"]), "untilDate startBoundary") ?? DateBoundary.Inclusive,
                ParseNullableEnum<DateBoundary>(ScalarText(untilDate["endBoundary"]), "untilDate endBoundary") ?? DateBoundary.Inclusive,
                ParseNullableEnum<EndDateSelection>(ScalarText(untilDate["selection"]), "untilDate selection") ?? EndDateSelection.FirstOnOrAfterStart);
        }

        return new OverrideEntry(OverrideOperation.PatchRule, notableDateRef, ruleRef, rule);
    }

    /// <summary>
    /// Rejects a <c>PatchRule</c> override that replaces the rule's occurrence source without naming exactly one, as
    /// the loader rejects it: a present <c>strategy</c> or <c>recurrence</c> must name one recognized kind between
    /// them.
    /// </summary>
    /// <param name="strategyJson">The override's strategy object, or <see langword="null" /> when absent.</param>
    /// <param name="recurrenceJson">The override's recurrence object, or <see langword="null" /> when absent.</param>
    /// <param name="notableDateRef">The identifier of the targeted concept, used in the exception message.</param>
    /// <param name="ruleRef">The identifier of the targeted rule, used in the exception message.</param>
    /// <exception cref="FormatException">
    /// A strategy or a recurrence is present, and the two name no recognized kind or more than one.
    /// </exception>
    private static void ThrowIfPatchOccurrenceSourceInvalid(JsonObject? strategyJson, JsonObject? recurrenceJson, string notableDateRef, string ruleRef)
    {
        if (strategyJson is null && recurrenceJson is null)
            return;

        int sources = CountKinds(strategyJson, s_jsonStrategyElementNames.Keys) + CountKinds(recurrenceJson, s_jsonRecurrenceElementNames.Keys);
        if (sources == 1)
            return;

        string format = sources == 0
            ? CalendarResourceStrings.Validation_PatchOccurrenceSourceMissing
            : CalendarResourceStrings.Validation_PatchOccurrenceSourceConflict;

        throw new FormatException(string.Format(CultureInfo.CurrentCulture, format, notableDateRef, ruleRef));
    }

    /// <summary>
    /// Counts the recognized kinds a JSON strategy or recurrence object names, as the loader counts them.
    /// </summary>
    /// <param name="source">The strategy or recurrence object, or <see langword="null" /> when absent.</param>
    /// <param name="kinds">The recognized kind names.</param>
    /// <returns>The number of recognized kinds present; zero when <paramref name="source" /> is absent.</returns>
    private static int CountKinds(JsonObject? source, IEnumerable<string> kinds) =>
        source is null ? 0 : kinds.Count(kind => FindKind(source, kind) is not null);

    /// <summary>
    /// Finds the body of a kind in a JSON strategy or recurrence object as the loader finds it: the first property that
    /// bears the kind's name, ignoring case, and is not the JSON literal <c>null</c>, when that property holds an
    /// object.
    /// </summary>
    /// <param name="source">The strategy or recurrence object.</param>
    /// <param name="kind">The kind name.</param>
    /// <returns>The kind's body, or <see langword="null" /> when the object does not name the kind.</returns>
    private static JsonObject? FindKind(JsonObject source, string kind)
    {
        foreach (KeyValuePair<string, JsonNode?> property in source)
        {
            if (property.Value is not null && string.Equals(property.Key, kind, StringComparison.OrdinalIgnoreCase))
                return property.Value as JsonObject;
        }

        return null;
    }

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

            ImportBuilder import = new(ScalarText(importJson["resource"]) ?? string.Empty);
            if (importJson["uses"] is JsonArray uses)
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
        ImportUseBuilder use = new(ScalarText(useJson["notableDateRef"]) ?? string.Empty);
        use.SetParsedValues(
            ScalarText(useJson["as"]),
            ScalarText(useJson["territory"]),
            ParseNullableEnum<NotableDateCategory>(ScalarText(useJson["category"]), "import use category"),
            JsonBool(useJson["nonWorking"], "import use nonWorking"),
            ReadPatchListJson(useJson["adjustments"] as JsonArray, "import use adjustments", "an empty adjustments list on an import use"));

        return use;
    }

    /// <summary>
    /// Builds an XML strategy element from a JSON strategy object, mapping the single populated strategy property to
    /// its element name and attributes.
    /// </summary>
    /// <param name="strategyJson">The strategy object, or <see langword="null" /> when absent.</param>
    /// <returns>
    /// The reconstructed strategy element, or <see langword="null" /> when the object is absent or empty.
    /// </returns>
    private static XElement? BuildStrategyElement(JsonObject? strategyJson)
    {
        if (strategyJson is null)
            return null;

        foreach (KeyValuePair<string, JsonNode?> property in strategyJson)
        {
            if (property.Value is not JsonObject body || !s_jsonStrategyElementNames.TryGetValue(property.Key, out string? elementName))
                continue;

            XElement element = new(BuilderXml.Namespace + elementName);
            foreach (KeyValuePair<string, JsonNode?> attribute in body)
                element.SetAttributeValue(attribute.Key, ConvertStrategyAttribute(attribute.Key, attribute.Value));

            return element;
        }

        return null;
    }

    /// <summary>
    /// Converts a JSON strategy attribute value into the string form expected by the XML schema, normalizing months and
    /// Boolean literals.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value, or <see langword="null" />.</param>
    /// <returns>The string form of the attribute value.</returns>
    private static string ConvertStrategyAttribute(string name, JsonNode? value)
    {
        if (value is null)
            return string.Empty;

        if (string.Equals(name, "month", StringComparison.Ordinal) && value.GetValueKind() == JsonValueKind.Number)
            return BuilderXml.GetMonthName((int)value);

        return value.GetValueKind() switch
        {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => ((int)value).ToString(CultureInfo.InvariantCulture),
            _ => ScalarText(value) ?? string.Empty,
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
            if (UnreadText(node, feature) is string text)
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
    /// <param name="node">The property value, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the value belongs to, used in the exception message.</param>
    /// <returns>The integer, or <see langword="null" /> when the property is absent or not a scalar.</returns>
    /// <exception cref="FormatException">The value is not a 32-bit integer.</exception>
    private static int? JsonInt(JsonNode? node, string field) =>
        ParseNullableInt(ScalarText(node), field);

    /// <summary>
    /// Reads an optional JSON Boolean property with the loader's rules: a Boolean, or an XML-schema Boolean literal as
    /// text or a number (<c>true</c>, <c>false</c>, <c>1</c> or <c>0</c>).
    /// </summary>
    /// <param name="node">The property value, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the value belongs to, used in the exception message.</param>
    /// <returns>The Boolean, or <see langword="null" /> when the property is absent or not a scalar.</returns>
    /// <exception cref="FormatException">The value is not a Boolean.</exception>
    private static bool? JsonBool(JsonNode? node, string field) =>
        ParseNullableBool(ScalarText(node), field);

    /// <summary>
    /// Reads a JSON scalar property as the loader does: a string verbatim, a number by its invariant digits, and a
    /// Boolean as <c>true</c> or <c>false</c>.
    /// </summary>
    /// <param name="node">
    /// The property's value, or <see langword="null" /> when absent or the JSON literal <c>null</c>.
    /// </param>
    /// <returns>The scalar's text, or <see langword="null" /> when the value is absent.</returns>
    /// <exception cref="FormatException">
    /// The value is an object or an array, which the loader reports with <c>BODU-CAL-VALUE</c>.
    /// </exception>
    private static string? ScalarText(JsonNode? node)
    {
        if (node is null)
            return null;

        if (node is not JsonValue value)
            throw new FormatException(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_InvalidScalarValue, node.GetPropertyName(), node.ToJsonString()));

        return value.GetValueKind() switch
        {
            JsonValueKind.String => value.GetValue<string>(),
            JsonValueKind.Number => value.TryGetValue(out int number) ? number.ToString(CultureInfo.InvariantCulture) : value.ToJsonString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }

    /// <summary>
    /// Reads a text field the loader does not read, accepting the scalars a text field accepts: a string verbatim, a
    /// number by its invariant digits, and a Boolean as <c>true</c> or <c>false</c>.
    /// </summary>
    /// <param name="node">The value, or <see langword="null" /> when absent.</param>
    /// <param name="feature">
    /// A description of the field holding an object or an array, used in the exception message.
    /// </param>
    /// <returns>The scalar's text, or <see langword="null" /> when the value is absent.</returns>
    /// <exception cref="NotSupportedException">
    /// The value is an object or an array, which the builder's document model, holding the field as text, cannot hold.
    /// </exception>
    private static string? UnreadText(JsonNode? node, string feature) =>
        node is null or JsonValue ? ScalarText(node) : throw UnrepresentableJsonField(feature);
}
