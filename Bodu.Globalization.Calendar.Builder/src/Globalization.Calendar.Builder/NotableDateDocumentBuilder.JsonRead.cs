// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilder.JsonRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
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
    /// <summary>
    /// Maps each JSON strategy property name to the corresponding XML strategy element local name.
    /// </summary>
    private static readonly Dictionary<string, string> s_jsonStrategyElementNames = new(StringComparer.Ordinal)
    {
        ["fixed"] = "Fixed",
        ["dayOfWeekInMonth"] = "DayOfWeekInMonth",
        ["weekdayNearDate"] = "WeekdayNearDate",
        ["relativeWeekdayInMonth"] = "RelativeWeekdayInMonth",
        ["offsetFromRule"] = "OffsetFromRule",
        ["algorithm"] = "Algorithm",
    };

    /// <summary>
    /// Reads the JSON <c>metadata</c> object into the builder.
    /// </summary>
    /// <param name="metadata">The metadata object, or <see langword="null" /> when absent.</param>
    private void ReadMetadataJson(JsonObject? metadata)
    {
        if (metadata is null)
            return;

        _metadataName = (string?)metadata["name"];
        _metadataDescription = (string?)metadata["description"];
        _sources.Clear();
        _sources.AddRange(ReadStringArray(metadata["sources"] as JsonArray));
    }

    /// <summary>
    /// Reads the JSON <c>resolutionPolicy</c> object into the builder.
    /// </summary>
    /// <param name="policyJson">The resolution-policy object, or <see langword="null" /> when absent.</param>
    private void ReadResolutionPolicyJson(JsonObject? policyJson)
    {
        if (policyJson is null)
            return;

        WeekPattern? workingWeek = null;
        string? workingDays = (string?)policyJson["workingDays"];
        if (!string.IsNullOrEmpty(workingDays) && WeekPattern.TryParse(workingDays, out WeekPattern parsed))
            workingWeek = parsed;

        IReadOnlyList<NotableDateCategory>? categoryPrecedence = policyJson["categoryPrecedence"] is not JsonArray array
            ? null
            : [.. array.Select(item => ParseNullableEnum<NotableDateCategory>((string?)item) ?? NotableDateCategory.None)];

        ResolutionPolicyBuilder policy = new();
        policy.SetParsedValues(
            ParseNullableEnum<DuplicatePolicy>((string?)policyJson["duplicatePolicy"]),
            ParseNullableEnum<CollisionPolicy>((string?)policyJson["sameDayCollisionPolicy"]),
            ParseNullableEnum<CollisionPolicy>((string?)policyJson["spanCollisionPolicy"]),
            ParseNullableEnum<PriorityDirection>((string?)policyJson["priorityDirection"]),
            ParseNullableEnum<ObservedDateRangePolicy>((string?)policyJson["observedDateRangePolicy"]),
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
        AdjustmentPolicyBuilder policy = new((string?)policyJson["id"] ?? string.Empty);
        policy.SetParsedHeader(
            (int?)policyJson["priority"],
            (string?)policyJson["description"],
            ReadScopeJson(policyJson["scope"] as JsonObject));

        if (policyJson["trigger"] is JsonObject trigger)
        {
            policy.SetParsedTrigger(
                ParseNullableEnum<AdjustmentTrigger>((string?)trigger["type"]),
                ReadStringArray(trigger["weekdays"] as JsonArray).Select(ParseEnum<DayOfWeek>),
                null,
                null,
                null,
                null);
        }

        if (policyJson["action"] is JsonObject action)
        {
            policy.SetParsedAction(
                ParseNullableEnum<AdjustmentAction>((string?)action["type"]),
                (int?)action["days"],
                ParseNullableEnum<DayOfWeek>((string?)action["dayOfWeek"]),
                (int?)action["maxSearchDays"],
                (bool?)action["skipWeekends"],
                (bool?)action["skipNonWorkingDates"],
                null,
                null,
                null);
        }

        if (policyJson["emission"] is JsonObject emission)
        {
            policy.SetParsedEmission(
                ParseNullableEnum<EmissionMode>((string?)emission["mode"]),
                (string?)emission["reason"],
                (bool?)emission["nonWorking"]);
        }

        return policy;
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
            ReadStringArray(scopeJson["territories"] as JsonArray),
            ReadStringArray(scopeJson["calendars"] as JsonArray).Select(ParseEnum<CalendarSystem>),
            ReadStringArray(scopeJson["categories"] as JsonArray).Select(ParseEnum<NotableDateCategory>),
            ReadStringArray(scopeJson["notableDateRefs"] as JsonArray),
            Array.Empty<(string, string)>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            null,
            null);

        return scope;
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
            (string?)definitionJson["id"] ?? string.Empty,
            (string?)definitionJson["displayName"] ?? string.Empty,
            ParseEnum<NotableDateCategory>((string?)definitionJson["category"]));

        definition.SetParsedValues(
            (int?)definitionJson["defaultDurationDays"],
            (bool?)definitionJson["defaultNonWorkingDay"],
            ReadStringArray(definitionJson["tags"] as JsonArray));

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
        NotableDateRuleBuilder rule = new((string?)ruleJson["id"] ?? string.Empty);
        var applicability = ruleJson["applicability"] as JsonObject;

        rule.SetParsedScalars(
            (int?)ruleJson["priority"],
            ParseNullableEnum<NotableDateCategory>((string?)ruleJson["category"]),
            (bool?)ruleJson["nonWorking"],
            (int?)ruleJson["durationDays"],
            (string?)ruleJson["comment"],
            applicability is null ? null : ParseNullableEnum<CalendarSystem>((string?)applicability["calendar"]),
            applicability is null ? null : (int?)applicability["fromYear"],
            applicability is null ? null : (int?)applicability["toYear"],
            applicability is null ? null : (int?)applicability["everyYears"],
            applicability is null ? null : (int?)applicability["anchorYear"]);

        rule.SetParsedCollections(
            applicability is null ? Array.Empty<string>() : ReadStringArray(applicability["territories"] as JsonArray),
            applicability is null ? Array.Empty<int>() : ReadIntArray(applicability["onlyYears"] as JsonArray),
            applicability is null ? Array.Empty<int>() : ReadIntArray(applicability["exceptYears"] as JsonArray),
            ReadStringArray(ruleJson["tags"] as JsonArray),
            ReadStringArray(ruleJson["adjustments"] as JsonArray));

        rule.SetParsedStrategy(BuildStrategyElement(ruleJson["strategy"] as JsonObject));

        return rule;
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
        string notableDateRef = (string?)overrideJson["notableDateRef"] ?? string.Empty;
        string operation = (string?)overrideJson["operation"] ?? string.Empty;

        switch (operation)
        {
            case "AddRule":
                NotableDateRuleBuilder addRule = overrideJson["rule"] is JsonObject ruleJson
                    ? ReadRuleJson(ruleJson)
                    : new NotableDateRuleBuilder(string.Empty);
                return new OverrideEntry(OverrideOperation.AddRule, notableDateRef, null, addRule);

            case "RemoveRule":
                return new OverrideEntry(OverrideOperation.RemoveRule, notableDateRef, (string?)overrideJson["ruleRef"] ?? string.Empty, null);

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
    private static OverrideEntry ReadPatchOverrideJson(JsonObject overrideJson, string notableDateRef)
    {
        string ruleRef = (string?)overrideJson["ruleRef"] ?? string.Empty;
        NotableDateRuleBuilder rule = new(ruleRef);
        rule.SetParsedScalars(
            (int?)overrideJson["priority"],
            ParseNullableEnum<NotableDateCategory>((string?)overrideJson["category"]),
            (bool?)overrideJson["nonWorking"],
            (int?)overrideJson["durationDays"],
            null,
            null,
            null,
            null,
            null,
            null);

        return new OverrideEntry(OverrideOperation.PatchRule, notableDateRef, ruleRef, rule);
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

        if (string.Equals(name, "month", StringComparison.Ordinal) && value.GetValueKind() == System.Text.Json.JsonValueKind.Number)
            return BuilderXml.GetMonthName((int)value);

        return value.GetValueKind() switch
        {
            System.Text.Json.JsonValueKind.True => "true",
            System.Text.Json.JsonValueKind.False => "false",
            System.Text.Json.JsonValueKind.Number => ((int)value).ToString(CultureInfo.InvariantCulture),
            _ => value.GetValue<string>(),
        };
    }

    /// <summary>
    /// Reads a JSON array into a list of string values.
    /// </summary>
    /// <param name="array">The array, or <see langword="null" /> when absent.</param>
    /// <returns>The string values; empty when the array is absent.</returns>
    private static List<string> ReadStringArray(JsonArray? array)
    {
        List<string> values = new();
        if (array is null)
            return values;

        foreach (JsonNode? node in array)
        {
            if (node is not null)
                values.Add(node.GetValue<string>());
        }

        return values;
    }

    /// <summary>
    /// Reads a JSON array into a list of integer values.
    /// </summary>
    /// <param name="array">The array, or <see langword="null" /> when absent.</param>
    /// <returns>The integer values; empty when the array is absent.</returns>
    private static List<int> ReadIntArray(JsonArray? array)
    {
        List<int> values = new();
        if (array is null)
            return values;

        foreach (JsonNode? node in array)
        {
            if (node is not null)
                values.Add((int)node);
        }

        return values;
    }
}
