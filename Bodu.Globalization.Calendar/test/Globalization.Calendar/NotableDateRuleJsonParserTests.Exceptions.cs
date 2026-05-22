// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.Exceptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateRuleJsonParser.ParseJson(string)" /> throws when given a null payload.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenInputIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleJsonParser.ParseJson(string)" /> throws when given a whitespace payload.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenInputIsWhitespace_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson("   ");
        });
    }

    /// <summary>
    /// Verifies that malformed JSON surfaces as a <see cref="JsonException" /> from
    /// <see cref="System.Text.Json.JsonSerializer" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenInputIsMalformedJson_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson("{ this is not json }");
        });
    }

    /// <summary>
    /// Verifies that a rule declaring no strategy at all is rejected by schema validation as
    /// <see cref="JsonException" /> via the <c>oneOf</c> exactly-one-strategy clause.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenRuleHasNoStrategy_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""No Strategy"", ""rules"": [ { ""name"": ""Bad"", ""category"": ""Holiday"" } ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that a rule declaring more than one strategy is rejected by schema validation as
    /// <see cref="JsonException" /> via the <c>oneOf</c> exactly-one-strategy clause.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenRuleHasMultipleStrategies_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Multiple"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""algorithm"": { ""key"": ""whatever"" }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that two adjustments sharing the same key on a single rule surface as
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentKeysDuplicate_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Dup"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""January"", ""day"": 1 },
					""adjustments"": [
						{ ""key"": ""same"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"" },
						{ ""key"": ""same"", ""when"": ""Always"", ""action"": ""None"" }
					]
				} ] }
			]
		}";

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that an unrecognised category enum value is rejected by schema validation as
    /// <see cref="JsonException" /> via the <c>notableDateCategory</c> enum constraint.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenCategoryIsUnknown_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bogus"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""SomethingMadeUp"",
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
    /// Verifies that an unrecognised month token on a Fixed rule is rejected by schema validation as
    /// <see cref="JsonException" /> via the <c>monthOrNumber</c> oneOf constraint.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMonthIsUnknown_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bogus"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""month"": ""Smarch"", ""day"": 1 }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }

    /// <summary>
    /// Verifies that omitting the required <c>month</c> field on a Fixed strategy is rejected by
    /// schema validation as <see cref="JsonException" /> via the <c>fixedStrategy</c> required clause.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedMissingMonth_ShouldThrowExactly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Bogus"", ""rules"": [ {
					""name"": ""Bad"",
					""category"": ""Holiday"",
					""fixed"": { ""day"": 1 }
				} ] }
			]
		}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }
}
