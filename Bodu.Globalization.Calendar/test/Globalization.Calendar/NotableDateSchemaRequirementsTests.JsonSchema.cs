// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRequirementsTests.JsonSchema.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSchemaRequirementsTests
{
    /// <summary>
    /// The definition of the companion JSON schema each schema type stands for, as a path of property names from the
    /// schema's root; an empty path is the document's root object.
    /// </summary>
    private static readonly Dictionary<NotableDateSchemaType, string[]> s_jsonSchemaDefinitions = new()
    {
        [NotableDateSchemaType.NotableDateResource] = [],
        [NotableDateSchemaType.AdjustmentPolicy] = ["$defs", "adjustmentPolicy"],
        [NotableDateSchemaType.AdjustmentTrigger] = ["$defs", "adjustmentPolicy", "properties", "trigger"],
        [NotableDateSchemaType.AdjustmentAction] = ["$defs", "adjustmentPolicy", "properties", "action"],
        [NotableDateSchemaType.AdjustmentEmission] = ["$defs", "adjustmentPolicy", "properties", "emission"],
        [NotableDateSchemaType.NotableDate] = ["$defs", "notableDate"],
        [NotableDateSchemaType.Rule] = ["$defs", "rule"],
        [NotableDateSchemaType.Duration] = ["$defs", "duration"],
        [NotableDateSchemaType.FixedStrategy] = ["$defs", "fixedStrategy"],
        [NotableDateSchemaType.DayOfWeekInMonthStrategy] = ["$defs", "dayOfWeekInMonthStrategy"],
        [NotableDateSchemaType.WeekdayNearDateStrategy] = ["$defs", "weekdayNearDateStrategy"],
        [NotableDateSchemaType.RelativeWeekdayInMonthStrategy] = ["$defs", "relativeWeekdayInMonthStrategy"],
        [NotableDateSchemaType.OffsetFromRuleStrategy] = ["$defs", "offsetFromRuleStrategy"],
        [NotableDateSchemaType.AlgorithmStrategy] = ["$defs", "algorithmStrategy"],
        [NotableDateSchemaType.WeekdayNearRuleStrategy] = ["$defs", "weekdayNearRuleStrategy"],
        [NotableDateSchemaType.NthWeekdayFromRuleStrategy] = ["$defs", "nthWeekdayFromRuleStrategy"],
        [NotableDateSchemaType.WorkingDayOffsetFromRuleStrategy] = ["$defs", "workingDayOffsetFromRuleStrategy"],
        [NotableDateSchemaType.WorkingDayInMonthStrategy] = ["$defs", "workingDayInMonthStrategy"],
        [NotableDateSchemaType.OrdinalDayOfMonthStrategy] = ["$defs", "ordinalDayOfMonthStrategy"],
        [NotableDateSchemaType.IsoWeekDateStrategy] = ["$defs", "isoWeekDateStrategy"],
        [NotableDateSchemaType.DayOfYearStrategy] = ["$defs", "dayOfYearStrategy"],
        [NotableDateSchemaType.DailyIntervalRecurrence] = ["$defs", "dailyIntervalRecurrence"],
        [NotableDateSchemaType.WeeklyRecurrence] = ["$defs", "weeklyRecurrence"],
        [NotableDateSchemaType.MonthlyDayRecurrence] = ["$defs", "monthlyDayRecurrence"],
        [NotableDateSchemaType.MonthlyWeekdayRecurrence] = ["$defs", "monthlyWeekdayRecurrence"],
        [NotableDateSchemaType.Import] = ["$defs", "import"],
        [NotableDateSchemaType.ImportUse] = ["$defs", "importUse"],
    };

    /// <summary>
    /// The operation of the companion JSON schema's <c>override</c> definition each override schema type stands for.
    /// </summary>
    private static readonly Dictionary<NotableDateSchemaType, string> s_jsonSchemaOperations = new()
    {
        [NotableDateSchemaType.AddRule] = "AddRule",
        [NotableDateSchemaType.PatchRule] = "PatchRule",
        [NotableDateSchemaType.RuleRef] = "RemoveRule",
    };

    /// <summary>
    /// Verifies that the companion JSON schema requires of each object the properties the loader requires of it, no
    /// more and no fewer, so a document the schema accepts loads and one the loader accepts passes the schema.
    /// </summary>
    [TestMethod]
    public void GetRequiredProperties_ForEachSchemaType_ShouldBeWhatTheJsonSchemaRequires()
    {
        JsonObject schema = JsonSchema();

        foreach ((NotableDateSchemaType type, string[] path) in s_jsonSchemaDefinitions)
            AssertRequiredPropertiesMatch(type, JsonSchemaRequired(Definition(schema, path)));

        JsonObject overrideDefinition = Definition(schema, ["$defs", "override"]);
        foreach ((NotableDateSchemaType type, string operation) in s_jsonSchemaOperations)
            AssertRequiredPropertiesMatch(type, JsonSchemaOperationRequired(overrideDefinition, operation));
    }

    /// <summary>
    /// Verifies that every list the companion JSON schema requires to hold an item is one the loader requires to hold an
    /// item, so the schema rejects no empty list the loader accepts, such as a document's <c>notableDates</c>.
    /// </summary>
    [TestMethod]
    public void RequiresItems_ForEachJsonSchemaListWithAMinimum_ShouldBeAListTheLoaderRequires()
    {
        JsonObject schema = JsonSchema();

        foreach ((NotableDateSchemaType type, string[] path) in s_jsonSchemaDefinitions)
        {
            if (Definition(schema, path)["properties"] is not JsonObject properties)
                continue;

            foreach ((string property, JsonNode? definition) in properties)
            {
                bool hasMinimum = definition?["minItems"] is JsonValue minimum && minimum.GetValue<int>() > 0;
                Assert.AreEqual(NotableDateSchemaRequirements.RequiresItems(type, property), hasMinimum, type + " " + property);
            }
        }
    }

    /// <summary>
    /// Returns the companion JSON schema embedded beside the XSD.
    /// </summary>
    /// <returns>The schema's root object.</returns>
    private static JsonObject JsonSchema()
    {
        using Stream? stream = typeof(NotableDateSchemaRequirements).Assembly.GetManifestResourceStream("Bodu.Globalization.Calendar.NotableDates.schema.json");
        Assert.IsNotNull(stream);

        return JsonNode.Parse(stream)!.AsObject();
    }

    /// <summary>
    /// Returns the definition at a path of property names from the JSON schema's root.
    /// </summary>
    /// <param name="schema">The schema's root object.</param>
    /// <param name="path">The property names, from the root.</param>
    /// <returns>The definition.</returns>
    private static JsonObject Definition(JsonObject schema, string[] path)
    {
        JsonObject definition = schema;
        foreach (string name in path)
            definition = definition[name]!.AsObject();

        return definition;
    }

    /// <summary>
    /// Returns the properties a JSON schema definition lists as <c>required</c>.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The required properties.</returns>
    private static IEnumerable<string> JsonSchemaRequired(JsonObject definition) =>
        definition["required"] is JsonArray required ? required.Select(name => name!.GetValue<string>()) : [];

    /// <summary>
    /// Returns the properties the JSON schema's <c>override</c> definition requires of an override with the given
    /// operation, besides the <c>operation</c> itself, which only JSON has: those it requires of every override and
    /// those an <c>if</c> on the operation's name adds.
    /// </summary>
    /// <param name="definition">The <c>override</c> definition.</param>
    /// <param name="operation">The operation's name.</param>
    /// <returns>The required properties.</returns>
    private static IEnumerable<string> JsonSchemaOperationRequired(JsonObject definition, string operation)
    {
        IEnumerable<string> required = JsonSchemaRequired(definition).Where(name => name != "operation");
        if (definition["allOf"] is not JsonArray conditions)
            return required;

        foreach (JsonNode? condition in conditions)
        {
            if (condition?["if"]?["properties"]?["operation"]?["const"]?.GetValue<string>() == operation && condition["then"] is JsonObject then)
                required = required.Concat(JsonSchemaRequired(then));
        }

        return required;
    }

    /// <summary>
    /// Asserts that the properties the JSON schema requires of an object are the ones its schema type requires.
    /// </summary>
    /// <param name="type">The schema type.</param>
    /// <param name="jsonSchemaRequired">The properties the JSON schema requires.</param>
    private static void AssertRequiredPropertiesMatch(NotableDateSchemaType type, IEnumerable<string> jsonSchemaRequired)
    {
        string[] expected = [.. NotableDateSchemaRequirements.GetRequiredProperties(type).Order(StringComparer.Ordinal)];
        string[] actual = [.. jsonSchemaRequired.Distinct().Order(StringComparer.Ordinal)];

        CollectionAssert.AreEqual(expected, actual, type + ": " + string.Join(", ", actual));
    }
}
