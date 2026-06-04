// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateJsonDocumentParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Parses a notable-date document expressed in JSON into its component model objects, collecting structural and
/// semantic diagnostics. The JSON shape mirrors the XML vocabulary one-to-one (camelCase strategy discriminators, a
/// <c>rules[].adjustments</c> array of policy identifiers).
/// </summary>
internal static class NotableDateJsonDocumentParser
{
    /// <summary>
    /// The full English month names, indexed so that January is at index zero.
    /// </summary>
    private static readonly string[] s_monthNames =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    };

    /// <summary>
    /// Parses a notable-date document from its JSON content.
    /// </summary>
    /// <param name="json">The notable-date document JSON content.</param>
    /// <param name="diagnostics">The collection that receives structural and semantic diagnostics.</param>
    /// <returns>The parsed <see cref="ParsedNotableDateDocument" />.</returns>
    /// <exception cref="FormatException"><paramref name="json" /> is not well-formed JSON.</exception>
    public static ParsedNotableDateDocument Parse(string json, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        using JsonDocument document = ParseDocument(json);
        JsonElement root = document.RootElement;

        return new ParsedNotableDateDocument(
            GetString(root, "resourceId") ?? string.Empty,
            GetString(root, "schemaVersion") ?? string.Empty,
            ParseResolutionPolicy(GetProperty(root, "resolutionPolicy")),
            ParseAdjustmentPolicies(GetProperty(root, "adjustmentPolicies")),
            ParseNotableDates(GetProperty(root, "notableDates"), diagnostics),
            ParseOverrides(GetProperty(root, "overrides"), diagnostics),
            ParseImports(GetProperty(root, "imports")));
    }

    /// <summary>
    /// Parses the import directives declared by the resource.
    /// </summary>
    /// <param name="element">The <c>imports</c> array, or <see langword="null" />.</param>
    /// <returns>The parsed import directives.</returns>
    private static List<NotableDateImport> ParseImports(JsonElement? element)
    {
        List<NotableDateImport> imports = new();
        if (element is not JsonElement array || array.ValueKind != JsonValueKind.Array)
            return imports;

        foreach (JsonElement import in array.EnumerateArray())
        {
            List<NotableDateImportUse> uses = new();
            if (GetProperty(import, "uses") is JsonElement useArray && useArray.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement use in useArray.EnumerateArray())
                {
                    uses.Add(new NotableDateImportUse(
                        GetString(use, "notableDateRef") ?? string.Empty,
                        GetString(use, "as"),
                        GetString(use, "territory"),
                        ParseNullableEnum<NotableDateCategory>(GetString(use, "category")),
                        GetNullableBool(use, "nonWorking"),
                        GetProperty(use, "adjustments") is JsonElement adjustments && adjustments.ValueKind == JsonValueKind.Array
                            ? adjustments.EnumerateArray().Select(adjustment => adjustment.GetString() ?? string.Empty).ToList()
                            : null));
                }
            }

            imports.Add(new NotableDateImport(GetString(import, "resource") ?? string.Empty, uses));
        }

        return imports;
    }

    /// <summary>
    /// Parses the raw JSON text, wrapping a syntax error as a <see cref="FormatException" />.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <returns>The parsed document.</returns>
    private static JsonDocument ParseDocument(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException(
                string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Format_Invalid_JsonNotWellFormed, ex.Message),
                ex);
        }
    }

    /// <summary>
    /// Parses the resource-level resolution policy, falling back to the recommended defaults when absent.
    /// </summary>
    /// <param name="element">The <c>resolutionPolicy</c> object, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="ResolutionPolicy" />.</returns>
    private static ResolutionPolicy ParseResolutionPolicy(JsonElement? element)
    {
        if (element is not JsonElement policy)
            return ResolutionPolicy.Default;

        return new ResolutionPolicy(
            ParseEnum(GetString(policy, "duplicatePolicy"), DuplicatePolicy.Error),
            ParseEnum(GetString(policy, "sameDayCollisionPolicy"), CollisionPolicy.KeepAll),
            ParseEnum(GetString(policy, "spanCollisionPolicy"), CollisionPolicy.KeepAll),
            ParseEnum(GetString(policy, "priorityDirection"), PriorityDirection.HigherWins),
            ParseEnum(GetString(policy, "observedDateRangePolicy"), ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion),
            ParseWorkingWeek(GetString(policy, "workingDays")));
    }

    /// <summary>
    /// Parses a Sunday-first seven-character working-week pattern, falling back to the default working week when the
    /// value is absent or blank.
    /// </summary>
    /// <param name="value">The working-week pattern, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="WeekPattern" />, or <see langword="null" /> when unspecified.</returns>
    private static WeekPattern? ParseWorkingWeek(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : WeekPattern.Parse(value);

    /// <summary>
    /// Parses the reusable adjustment policies declared by the resource.
    /// </summary>
    /// <param name="element">The <c>adjustmentPolicies</c> array, or <see langword="null" />.</param>
    /// <returns>The parsed adjustment policies.</returns>
    private static List<AdjustmentPolicy> ParseAdjustmentPolicies(JsonElement? element)
    {
        List<AdjustmentPolicy> policies = new();
        if (element is not JsonElement array || array.ValueKind != JsonValueKind.Array)
            return policies;

        foreach (JsonElement policy in array.EnumerateArray())
        {
            JsonElement? trigger = GetProperty(policy, "trigger");
            JsonElement? action = GetProperty(policy, "action");
            JsonElement? emission = GetProperty(policy, "emission");

            IEnumerable<DayOfWeek> weekdays = trigger is JsonElement t && GetProperty(t, "weekdays") is JsonElement w && w.ValueKind == JsonValueKind.Array
                ? w.EnumerateArray().Select(v => ParseEnum(v.GetString(), DayOfWeek.Monday))
                : Array.Empty<DayOfWeek>();

            policies.Add(new AdjustmentPolicy(
                GetString(policy, "id") ?? string.Empty,
                GetInt(policy, "priority", 0),
                ParseScope(GetProperty(policy, "scope")),
                ParseEnum(trigger is JsonElement tr ? GetString(tr, "type") : null, AdjustmentTrigger.Always),
                weekdays,
                ParseEnum(action is JsonElement ac ? GetString(ac, "type") : null, AdjustmentAction.None),
                action is JsonElement a ? ParseNullableEnum<DayOfWeek>(GetString(a, "dayOfWeek")) : null,
                action is JsonElement a2 ? GetInt(a2, "days", 0) : 0,
                action is JsonElement a3 ? GetNullableInt(a3, "maxSearchDays") : null,
                action is JsonElement a4 ? GetBool(a4, "skipWeekends", true) : true,
                action is JsonElement a5 && GetBool(a5, "skipNonWorkingDates", false),
                ParseEnum(emission is JsonElement em ? GetString(em, "mode") : null, EmissionMode.ActualOnly),
                emission is JsonElement e ? GetString(e, "reason") : null,
                emission is JsonElement e2 ? GetNullableBool(e2, "nonWorking") : null,
                trigger is JsonElement tm ? ParseTriggerMonth(GetMonthToken(tm)) : null,
                trigger is JsonElement td ? GetNullableInt(td, "day") : null,
                trigger is JsonElement two ? ParseNullableEnum<WeekOrdinal>(GetString(two, "weekOrdinal")) : null,
                action is JsonElement an ? GetString(an, "notableDateRef") : null,
                action is JsonElement ar ? GetString(ar, "ruleRef") : null,
                action is JsonElement ah ? GetString(ah, "handlerKey") : null,
                trigger is JsonElement th ? GetString(th, "handlerKey") : null,
                ParseHandlerParameters(GetProperty(policy, "parameters"))));
        }

        return policies;
    }

    /// <summary>
    /// Parses the custom-handler parameter map declared by a policy as a JSON object of string values.
    /// </summary>
    /// <param name="element">The <c>parameters</c> object, or <see langword="null" />.</param>
    /// <returns>The parameter map, or <see langword="null" /> when none are declared.</returns>
    private static IReadOnlyDictionary<string, string>? ParseHandlerParameters(JsonElement? element)
    {
        if (element is not JsonElement parameters || parameters.ValueKind != JsonValueKind.Object)
            return null;

        Dictionary<string, string> result = new(StringComparer.Ordinal);
        foreach (JsonProperty property in parameters.EnumerateObject())
            result[property.Name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString();

        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Parses a trigger comparison month expressed as a full English month name or an integer between 1 and 12.
    /// </summary>
    /// <param name="value">The raw month value, or <see langword="null" />.</param>
    /// <returns>The one-based month, or <see langword="null" /> when absent or invalid.</returns>
    private static int? ParseTriggerMonth(string? value)
    {
        if (value is null)
            return null;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 12)
            return numeric;

        int index = Array.FindIndex(s_monthNames, n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index + 1 : null;
    }

    /// <summary>
    /// Parses an adjustment policy scope, falling back to a global scope when absent.
    /// </summary>
    /// <param name="element">The <c>scope</c> object, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="AdjustmentScope" />.</returns>
    private static AdjustmentScope ParseScope(JsonElement? element)
    {
        if (element is not JsonElement scope)
            return AdjustmentScope.Global;

        return new AdjustmentScope(
            StringArray(scope, "territories"),
            StringArray(scope, "calendars").Select(c => ParseEnum(c, CalendarSystem.Gregorian)),
            StringArray(scope, "categories").Select(c => ParseEnum(c, NotableDateCategory.None)),
            StringArray(scope, "notableDateRefs"),
            StringArray(scope, "ruleRefs"),
            GetNullableInt(scope, "fromYear"),
            GetNullableInt(scope, "toYear"),
            IntArray(scope, "onlyYears"),
            IntArray(scope, "exceptYears"));
    }

    /// <summary>
    /// Parses the notable-date concepts declared by the resource.
    /// </summary>
    /// <param name="element">The <c>notableDates</c> array, or <see langword="null" />.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed notable-date concepts.</returns>
    private static List<NotableDateDefinition> ParseNotableDates(JsonElement? element, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateDefinition> definitions = new();
        if (element is not JsonElement array || array.ValueKind != JsonValueKind.Array)
            return definitions;

        foreach (JsonElement notableDate in array.EnumerateArray())
        {
            string id = GetString(notableDate, "id") ?? string.Empty;

            definitions.Add(new NotableDateDefinition(
                id,
                GetString(notableDate, "displayName") ?? string.Empty,
                ParseEnum(GetString(notableDate, "category"), NotableDateCategory.None),
                GetBool(notableDate, "defaultNonWorkingDay", false),
                GetInt(notableDate, "defaultDurationDays", 1),
                StringArray(notableDate, "tags").ToList(),
                ParseRules(GetProperty(notableDate, "rules"), id, diagnostics)));
        }

        return definitions;
    }

    /// <summary>
    /// Parses the rules belonging to a notable-date concept.
    /// </summary>
    /// <param name="element">The <c>rules</c> array, or <see langword="null" />.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed rules.</returns>
    private static List<NotableDateRule> ParseRules(JsonElement? element, string notableDateId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateRule> rules = new();
        if (element is not JsonElement array || array.ValueKind != JsonValueKind.Array)
            return rules;

        foreach (JsonElement rule in array.EnumerateArray())
            rules.Add(ParseRule(rule, notableDateId, diagnostics));

        return rules;
    }

    /// <summary>
    /// Parses a single rule object.
    /// </summary>
    /// <param name="element">The rule object.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="NotableDateRule" />.</returns>
    private static NotableDateRule ParseRule(JsonElement element, string notableDateId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        string id = GetString(element, "id") ?? string.Empty;
        RuleApplicability applicability = ParseApplicability(GetProperty(element, "applicability"));

        return new NotableDateRule(
            id,
            GetInt(element, "priority", 0),
            ParseNullableEnum<NotableDateCategory>(GetString(element, "category")),
            GetNullableBool(element, "nonWorking"),
            GetNullableInt(element, "durationDays"),
            applicability,
            ParseStrategy(GetProperty(element, "strategy"), applicability.Calendar, notableDateId, id, diagnostics),
            StringArray(element, "adjustments").ToList(),
            StringArray(element, "tags").ToList());
    }

    /// <summary>
    /// Parses a rule's applicability, falling back to a global Gregorian applicability when absent.
    /// </summary>
    /// <param name="element">The <c>applicability</c> object, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="RuleApplicability" />.</returns>
    private static RuleApplicability ParseApplicability(JsonElement? element)
    {
        if (element is not JsonElement applicability)
            return new RuleApplicability(CalendarSystem.Gregorian, null, null, Array.Empty<string>(), Array.Empty<int>(), Array.Empty<int>());

        return new RuleApplicability(
            ParseEnum(GetString(applicability, "calendar"), CalendarSystem.Gregorian),
            GetNullableInt(applicability, "fromYear"),
            GetNullableInt(applicability, "toYear"),
            StringArray(applicability, "territories"),
            IntArray(applicability, "onlyYears"),
            IntArray(applicability, "exceptYears"),
            GetNullableInt(applicability, "everyYears"),
            GetNullableInt(applicability, "anchorYear"));
    }

    /// <summary>
    /// Parses a rule's strategy by dispatching on the single populated strategy property.
    /// </summary>
    /// <param name="element">The <c>strategy</c> object, or <see langword="null" />.</param>
    /// <param name="calendar">The calendar system the rule's applicability declares.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="IDateCalculationStrategy" />.</returns>
    private static IDateCalculationStrategy ParseStrategy(JsonElement? element, CalendarSystem calendar, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (element is not JsonElement strategy)
            return new FixedDateStrategy(1, 1);

        if (GetProperty(strategy, "fixed") is JsonElement fixedStrategy)
        {
            (int month, string? monthAlias) = ParseFixedMonth(GetMonthToken(fixedStrategy), calendar, notableDateId, ruleId, diagnostics);
            return new FixedDateStrategy(
                month,
                Math.Clamp(GetInt(fixedStrategy, "day", 1), 1, 31),
                calendar,
                GetBool(fixedStrategy, "sweepCalendarYears", false),
                GetBool(fixedStrategy, "skipLeapMonth", false),
                monthAlias);
        }

        if (GetProperty(strategy, "dayOfWeekInMonth") is JsonElement dayOfWeekInMonth)
        {
            return new DayOfWeekInMonthStrategy(
                ParseMonth(GetMonthToken(dayOfWeekInMonth), notableDateId, ruleId, diagnostics),
                ParseEnum(GetString(dayOfWeekInMonth, "dayOfWeek"), DayOfWeek.Monday),
                ParseEnum(GetString(dayOfWeekInMonth, "weekOrdinal"), WeekOrdinal.First));
        }

        if (GetProperty(strategy, "weekdayNearDate") is JsonElement weekdayNearDate)
        {
            return new WeekdayNearDateStrategy(
                ParseMonth(GetMonthToken(weekdayNearDate), notableDateId, ruleId, diagnostics),
                Math.Clamp(GetInt(weekdayNearDate, "day", 1), 1, 31),
                ParseEnum(GetString(weekdayNearDate, "dayOfWeek"), DayOfWeek.Monday),
                ParseEnum(GetString(weekdayNearDate, "direction"), WeekdayProximity.OnOrAfter));
        }

        if (GetProperty(strategy, "relativeWeekdayInMonth") is JsonElement relativeWeekdayInMonth)
        {
            return new RelativeWeekdayInMonthStrategy(
                ParseMonth(GetMonthToken(relativeWeekdayInMonth), notableDateId, ruleId, diagnostics),
                ParseEnum(GetString(relativeWeekdayInMonth, "dayOfWeek"), DayOfWeek.Monday),
                ParseEnum(GetString(relativeWeekdayInMonth, "weekOrdinal"), WeekOrdinal.First),
                ParseEnum(GetString(relativeWeekdayInMonth, "relativeDayOfWeek"), DayOfWeek.Monday),
                ParseEnum(GetString(relativeWeekdayInMonth, "direction"), WeekdayProximity.After));
        }

        if (GetProperty(strategy, "offsetFromRule") is JsonElement offsetFromRule)
        {
            string? ruleRef = GetString(offsetFromRule, "ruleRef");
            return new OffsetFromRuleStrategy(
                GetString(offsetFromRule, "notableDateRef") ?? string.Empty,
                string.IsNullOrEmpty(ruleRef) ? null : ruleRef,
                GetInt(offsetFromRule, "offsetDays", 0));
        }

        if (GetProperty(strategy, "algorithm") is JsonElement algorithm)
            return new AlgorithmDateStrategy(GetString(algorithm, "key") ?? string.Empty);

        return new FixedDateStrategy(1, 1);
    }

    /// <summary>
    /// Parses the override operations declared by the resource.
    /// </summary>
    /// <param name="element">The <c>overrides</c> array, or <see langword="null" />.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed override operations.</returns>
    private static List<NotableDateRuleOverride> ParseOverrides(JsonElement? element, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateRuleOverride> overrides = new();
        if (element is not JsonElement array || array.ValueKind != JsonValueKind.Array)
            return overrides;

        foreach (JsonElement operation in array.EnumerateArray())
        {
            string notableDateRef = GetString(operation, "notableDateRef") ?? string.Empty;
            string op = GetString(operation, "operation") ?? string.Empty;

            switch (op)
            {
                case "RemoveRule":
                    overrides.Add(new RemoveRuleOverride(notableDateRef, GetString(operation, "ruleRef") ?? string.Empty));
                    break;

                case "PatchRule":
                    overrides.Add(new PatchRuleOverride(
                        notableDateRef,
                        GetString(operation, "ruleRef") ?? string.Empty,
                        GetNullableInt(operation, "priority"),
                        ParseNullableEnum<NotableDateCategory>(GetString(operation, "category")),
                        GetNullableBool(operation, "nonWorking"),
                        GetNullableInt(operation, "durationDays"),
                        null,
                        null,
                        null,
                        null));
                    break;

                case "AddRule" when GetProperty(operation, "rule") is JsonElement rule:
                    overrides.Add(new AddRuleOverride(notableDateRef, ParseRule(rule, notableDateRef, diagnostics)));
                    break;

                default:
                    break;
            }
        }

        return overrides;
    }

    /// <summary>
    /// Reads the <c>month</c> property of a strategy as a string token, accepting a JSON number or string.
    /// </summary>
    /// <param name="strategy">The strategy object.</param>
    /// <returns>The month token, or <see langword="null" /> when absent.</returns>
    private static string? GetMonthToken(JsonElement strategy)
    {
        if (GetProperty(strategy, "month") is not JsonElement month)
            return null;

        return month.ValueKind == JsonValueKind.Number
            ? month.GetInt32().ToString(CultureInfo.InvariantCulture)
            : month.GetString();
    }

    /// <summary>
    /// Parses a month value expressed as a full English month name or an integer between 1 and 12.
    /// </summary>
    /// <param name="value">The raw month value.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The one-based month, or 1 when the value is invalid.</returns>
    private static int ParseMonth(string? value, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is not null)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 12)
                return numeric;

            int index = Array.FindIndex(s_monthNames, n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                return index + 1;
        }

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL2-MONTH",
            string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Validation_InvalidMonthValue, notableDateId, ruleId, value ?? string.Empty)));

        return 1;
    }

    /// <summary>
    /// Parses a fixed-strategy month for a given calendar system, returning either a numeric month or a Hebrew alias.
    /// </summary>
    /// <param name="value">The raw month value.</param>
    /// <param name="calendar">The calendar system the month is expressed in.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>A tuple of the one-based month (or <c>0</c> when an alias supplies it) and an optional Hebrew alias.</returns>
    private static (int Month, string? Alias) ParseFixedMonth(string? value, CalendarSystem calendar, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (calendar == CalendarSystem.Gregorian)
            return (ParseMonth(value, notableDateId, ruleId, diagnostics), null);

        if (value is not null)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 13)
                return (numeric, null);

            switch (value)
            {
                case "Tishri": return (1, null);
                case "Heshvan": return (2, null);
                case "Kislev": return (3, null);
                case "Tevet": return (4, null);
                case "Shevat": return (5, null);
                case "AdarI": return (6, null);
                case "AdarII":
                case "LastAdar":
                case "Nisan":
                case "Iyar":
                case "Sivan":
                case "Tammuz":
                case "Av":
                case "Elul":
                    return (0, value);
                default:
                    break;
            }
        }

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL2-MONTH",
            string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Validation_InvalidMonthValue, notableDateId, ruleId, value ?? string.Empty)));

        return (1, null);
    }

    /// <summary>
    /// Returns a child property of an object element, or <see langword="null" /> when absent or null-valued.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property element, or <see langword="null" />.</returns>
    private static JsonElement? GetProperty(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? value
            : null;

    /// <summary>
    /// Reads a string property.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string value, or <see langword="null" /> when absent.</returns>
    private static string? GetString(JsonElement element, string name) =>
        GetProperty(element, name) is JsonElement value && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>
    /// Reads an integer property, falling back to a default when absent.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <param name="fallback">The value returned when the property is absent.</param>
    /// <returns>The integer value, or <paramref name="fallback" />.</returns>
    private static int GetInt(JsonElement element, string name, int fallback) =>
        GetProperty(element, name) is JsonElement value && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result) ? result : fallback;

    /// <summary>
    /// Reads an optional integer property.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The integer value, or <see langword="null" /> when absent.</returns>
    private static int? GetNullableInt(JsonElement element, string name) =>
        GetProperty(element, name) is JsonElement value && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result) ? result : null;

    /// <summary>
    /// Reads a boolean property, falling back to a default when absent.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <param name="fallback">The value returned when the property is absent.</param>
    /// <returns>The boolean value, or <paramref name="fallback" />.</returns>
    private static bool GetBool(JsonElement element, string name, bool fallback) =>
        GetProperty(element, name) is JsonElement value && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) ? value.GetBoolean() : fallback;

    /// <summary>
    /// Reads an optional boolean property.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The boolean value, or <see langword="null" /> when absent.</returns>
    private static bool? GetNullableBool(JsonElement element, string name) =>
        GetProperty(element, name) is JsonElement value && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) ? value.GetBoolean() : null;

    /// <summary>
    /// Reads a string-array property into a list, treating an absent or non-array property as empty.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string values.</returns>
    private static List<string> StringArray(JsonElement element, string name)
    {
        List<string> values = new();
        if (GetProperty(element, name) is JsonElement array && array.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    values.Add(item.GetString() ?? string.Empty);
            }
        }

        return values;
    }

    /// <summary>
    /// Reads an integer-array property into a list, treating an absent or non-array property as empty.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The integer values.</returns>
    private static List<int> IntArray(JsonElement element, string name)
    {
        List<int> values = new();
        if (GetProperty(element, name) is JsonElement array && array.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out int result))
                    values.Add(result);
            }
        }

        return values;
    }

    /// <summary>
    /// Parses an enumeration value case-insensitively, falling back to a default when absent or unrecognised.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The raw value.</param>
    /// <param name="fallback">The value returned when parsing fails.</param>
    /// <returns>The parsed value, or <paramref name="fallback" />.</returns>
    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum =>
        value is not null && Enum.TryParse(value, ignoreCase: true, out TEnum result) && Enum.IsDefined(result) ? result : fallback;

    /// <summary>
    /// Parses an optional enumeration value case-insensitively.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The raw value, or <see langword="null" />.</param>
    /// <returns>The parsed value, or <see langword="null" /> when absent or unrecognised.</returns>
    private static TEnum? ParseNullableEnum<TEnum>(string? value)
        where TEnum : struct, Enum =>
        value is not null && Enum.TryParse(value, ignoreCase: true, out TEnum result) && Enum.IsDefined(result) ? result : null;
}
