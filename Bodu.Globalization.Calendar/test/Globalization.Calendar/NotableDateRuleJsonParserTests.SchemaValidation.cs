// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.SchemaValidation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleJsonParser" /> validates inputs against the embedded
/// <c>NotableDates.schema.json</c> schema before parsing, so authoring errors surface as
/// <see cref="JsonException" /> at parse time — mirroring the XML pathway that surfaces
/// <see cref="System.Xml.Schema.XmlSchemaValidationException" /> from XSD validation.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that an unknown property at the document root is rejected by schema validation
    /// (root <c>additionalProperties: false</c>).
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenRootDeclaresUnknownProperty_ShouldThrowExactly()
    {
        const string json = @"{
			""nonsense"": ""value"",
			""notableDates"": []
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that <c>useAll: false</c> is rejected by schema validation, since the schema models
    /// <c>useAll</c> as a presence flag (<c>const: true</c>) — false is not meaningful.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenUseAllIsFalse_ShouldThrowExactly()
    {
        const string json = @"{
			""useFrom"": [
				{ ""resource"": ""some.resource"", ""useAll"": false }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a <c>useFrom</c> entry with neither <c>useAll</c> nor <c>uses</c> is rejected by
    /// schema validation (the <c>anyOf</c> clause requires at least one of them).
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenUseFromHasNoDirective_ShouldThrowExactly()
    {
        const string json = @"{
			""useFrom"": [
				{ ""resource"": ""some.resource"" }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a Fixed strategy with <c>day: 32</c> is rejected by schema validation
    /// (the <c>day</c> simple type maxes at 31).
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedDayExceedsMaximum_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Out Of Range"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 32 }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that an adjustment trigger outside the enumerated set is rejected by schema
    /// validation (<c>adjustmentTrigger</c> enum constraint).
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentTriggerIsUnknownEnum_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bad Trigger"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""key"": ""x"", ""when"": ""NotARealTrigger"", ""action"": ""None"" }
					]
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a territory code that does not match the ISO-3166 pattern is rejected by
    /// schema validation (<c>territory</c> pattern constraint).
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenTerritoryViolatesPattern_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bad Territory"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""territory"": ""bogus value"",
					""fixed"": { ""month"": ""January"", ""day"": 1 }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a notable date with an empty <c>rules</c> array is rejected by schema
    /// validation (<c>minItems: 1</c> on the rules array).
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenNotableDateHasEmptyRules_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Empty"", ""rules"": [] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a <c>useFrom</c> directive's override-body (the nested <c>rule</c>) is rejected by
    /// schema validation when it omits the required <c>name</c> attribute — matching the XSD's literal
    /// <c>OverrideRule</c> requirement.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenOverrideRuleOmitsName_ShouldThrowExactly()
    {
        const string json = @"{
			""useFrom"": [
				{
					""resource"": ""some.resource"",
					""uses"": [
						{
							""name"": ""Source Rule"",
							""rule"": { ""category"": ""Holiday"" }
						}
					]
				}
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a <c>useFrom</c> directive's override-body is rejected by schema validation when
    /// it omits the required <c>category</c> attribute — matching the XSD's literal <c>OverrideRule</c>
    /// requirement.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenOverrideRuleOmitsCategory_ShouldThrowExactly()
    {
        const string json = @"{
			""useFrom"": [
				{
					""resource"": ""some.resource"",
					""uses"": [
						{
							""name"": ""Source Rule"",
							""rule"": { ""name"": ""Local Override"" }
						}
					]
				}
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a numeric Fixed-strategy month outside the <c>1..13</c> calendar-month range is
    /// rejected by schema validation (<c>monthOrNumber</c> integer branch range).
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedNumericMonthExceedsMaximum_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bad Numeric Month"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": 14, ""day"": 1 }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a fully-populated valid document round-trips through parse without throwing,
    /// confirming the schema's positive path admits the canonical authoring shape.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenDocumentIsValid_ShouldRoundTripThroughSchema()
    {
        List<NotableDateRule> rules = NotableDateRuleJsonParser.ParseJson(MultiRuleJson);

        Assert.AreEqual(4, rules.Count);
    }
}
