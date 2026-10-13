// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserValidationTests.JsonSchema.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json.Nodes;

namespace Bodu.Globalization.Calendar;

public sealed partial class ParserValidationTests
{
    /// <summary>
    /// The JSON pointer into <see cref="BoundsTemplateJson" /> of each integer the bound tests set, keyed by the name
    /// that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_boundFields = new(StringComparer.Ordinal)
    {
        ["rule.priority"] = "/notableDates/0/rules/0/priority",
        ["rule.durationDays"] = "/notableDates/0/rules/0/durationDays",
        ["applicability.everyYears"] = "/notableDates/0/rules/0/applicability/everyYears",
        ["applicability.onlyYears.item"] = "/notableDates/0/rules/0/applicability/onlyYears/0",
        ["applicability.exceptYears.item"] = "/notableDates/0/rules/0/applicability/exceptYears/0",
        ["nthWeekdayFromRule.ordinal"] = "/notableDates/0/rules/1/strategy/nthWeekdayFromRule/ordinal",
        ["workingDayInMonth.ordinal"] = "/notableDates/0/rules/2/strategy/workingDayInMonth/ordinal",
        ["ordinalDayOfMonth.ordinal"] = "/notableDates/0/rules/3/strategy/ordinalDayOfMonth/ordinal",
        ["dayOfYear.ordinal"] = "/notableDates/0/rules/4/strategy/dayOfYear/ordinal",
    };

    /// <summary>
    /// The JSON document under the bound tests, which loads and which the companion JSON schema accepts, holding one of
    /// each integer the bound tests set.
    /// </summary>
    private const string BoundsTemplateJson = """
        { "schemaVersion": "1.0", "resourceId": "test.bounds",
          "notableDates": [ { "id": "anchor", "displayName": "Anchor", "category": "Observance",
            "rules": [
              { "id": "r", "priority": 1, "durationDays": 1, "applicability": { "everyYears": 1, "anchorYear": 2000, "onlyYears": [ 2026 ], "exceptYears": [ 2027 ] },
                "strategy": { "fixed": { "month": 3, "day": 1 } } },
              { "id": "nth", "strategy": { "nthWeekdayFromRule": { "notableDateRef": "anchor", "ruleRef": "r", "dayOfWeek": "Sunday", "ordinal": 2 } } },
              { "id": "working", "strategy": { "workingDayInMonth": { "month": 6, "ordinal": 1 } } },
              { "id": "ordinal-day", "strategy": { "ordinalDayOfMonth": { "month": 7, "ordinal": 2 } } },
              { "id": "day-of-year", "strategy": { "dayOfYear": { "ordinal": 100 } } } ] } ] }
        """;

    /// <summary>
    /// Verifies that the bound test document loads and that the companion JSON schema accepts it, so each verdict the
    /// per-value test compares is caused by the one integer it sets.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEveryIntegerIsWithinItsBounds_ShouldLoadAndPassTheJsonSchema()
    {
        string json = BoundFieldJson(null, null);

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json);
        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(CompanionJsonSchema(), JsonNode.Parse(json));

        Assert.AreEqual("test.bounds", resource.ResourceId);
        Assert.IsEmpty(errors, string.Join(" | ", errors));
    }

    /// <summary>
    /// Verifies that the companion JSON schema accepts a JSON integer at or beyond a bound of the range the loader holds
    /// it to exactly when <c>LoadJson</c> loads the document, so the schema states the loader's bounds: an ordinal
    /// excludes zero, <c>everyYears</c> has no upper bound short of a 32-bit integer, a year in a list is held to 0 to
    /// 65535, and every other integer to the 32-bit range the loader reads.
    /// </summary>
    /// <param name="field">The name of the integer.</param>
    /// <param name="literal">The integer, as a JSON literal.</param>
    [TestMethod]
    [DataRow("rule.priority", "-2147483649")]
    [DataRow("rule.priority", "-2147483648")]
    [DataRow("rule.priority", "2147483647")]
    [DataRow("rule.priority", "2147483648")]
    [DataRow("rule.durationDays", "2147483647")]
    [DataRow("rule.durationDays", "2147483648")]
    [DataRow("applicability.everyYears", "0")]
    [DataRow("applicability.everyYears", "1")]
    [DataRow("applicability.everyYears", "65535")]
    [DataRow("applicability.everyYears", "65536")]
    [DataRow("applicability.everyYears", "2147483647")]
    [DataRow("applicability.everyYears", "2147483648")]
    [DataRow("applicability.onlyYears.item", "-1")]
    [DataRow("applicability.onlyYears.item", "0")]
    [DataRow("applicability.onlyYears.item", "65535")]
    [DataRow("applicability.onlyYears.item", "65536")]
    [DataRow("applicability.exceptYears.item", "-1")]
    [DataRow("applicability.exceptYears.item", "0")]
    [DataRow("applicability.exceptYears.item", "65535")]
    [DataRow("applicability.exceptYears.item", "70000")]
    [DataRow("nthWeekdayFromRule.ordinal", "-367")]
    [DataRow("nthWeekdayFromRule.ordinal", "-366")]
    [DataRow("nthWeekdayFromRule.ordinal", "-1")]
    [DataRow("nthWeekdayFromRule.ordinal", "0")]
    [DataRow("nthWeekdayFromRule.ordinal", "1")]
    [DataRow("nthWeekdayFromRule.ordinal", "366")]
    [DataRow("nthWeekdayFromRule.ordinal", "367")]
    [DataRow("workingDayInMonth.ordinal", "0")]
    [DataRow("ordinalDayOfMonth.ordinal", "0")]
    [DataRow("dayOfYear.ordinal", "0")]
    [DataRow("dayOfYear.ordinal", "-1")]
    public void LoadJson_WhenIntegerIsAtOrBeyondItsBound_ShouldLoadExactlyWhenTheJsonSchemaAcceptsIt(string field, string literal)
    {
        string json = BoundFieldJson(field, literal);

        bool loaded = NotableDateResourceLoader.TryLoadJson(json, static _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);
        IReadOnlyList<string> errors = JsonSchemaValidator.Validate(CompanionJsonSchema(), JsonNode.Parse(json));

        Assert.AreEqual(
            loaded,
            errors.Count == 0,
            "LoadJson " + (loaded ? "loaded" : "rejected") + " " + field + " " + literal + string.Concat(diagnostics.Select(d => "; " + d.Code + ": " + d.Message)) + "; the schema reported: " + string.Join(" | ", errors));
    }

    /// <summary>
    /// Returns the companion JSON schema embedded beside the XSD.
    /// </summary>
    /// <returns>The schema's root object.</returns>
    private static JsonObject CompanionJsonSchema()
    {
        using Stream? stream = typeof(NotableDateResourceLoader).Assembly.GetManifestResourceStream("Bodu.Globalization.Calendar.NotableDates.schema.json");
        Assert.IsNotNull(stream);

        return JsonNode.Parse(stream)!.AsObject();
    }

    /// <summary>
    /// Returns <see cref="BoundsTemplateJson" /> with one integer replaced.
    /// </summary>
    /// <param name="field">The name of the integer, or <see langword="null" /> to change none.</param>
    /// <param name="literal">The JSON literal the integer is replaced with.</param>
    /// <returns>The JSON document.</returns>
    private static string BoundFieldJson(string? field, string? literal)
    {
        JsonNode root = JsonNode.Parse(BoundsTemplateJson)!;
        if (field is null)
            return root.ToJsonString();

        string[] segments = s_boundFields[field].Split('/', StringSplitOptions.RemoveEmptyEntries);
        JsonNode parent = root;
        foreach (string segment in segments[..^1])
            parent = parent is JsonArray array ? array[int.Parse(segment, CultureInfo.InvariantCulture)]! : parent[segment]!;

        string last = segments[^1];
        if (parent is JsonArray items)
            items[int.Parse(last, CultureInfo.InvariantCulture)] = JsonNode.Parse(literal!);
        else
            parent[last] = JsonNode.Parse(literal!);

        return root.ToJsonString();
    }
}
