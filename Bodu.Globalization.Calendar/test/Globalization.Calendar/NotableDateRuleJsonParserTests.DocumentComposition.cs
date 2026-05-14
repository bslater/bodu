// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.DocumentComposition.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleJsonParser" /> orchestrates realistic document shapes — combined
/// <c>useFrom</c> + <c>notableDates</c> documents, multiple <c>useFrom</c> groups, multiple notable dates, multi-rule
/// notable dates, and rules that span every supported strategy. These tests exercise the iterators and orchestration
/// paths in <see cref="NotableDateRuleJsonParser.ParseDocument(string)" /> that single-rule snippets miss.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that a document containing both <c>useFrom</c> and <c>notableDates</c> populates the corresponding
    /// collections on <see cref="ParsedNotableDateDocument" /> independently.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenDocumentContainsUseFromAndNotableDates_ShouldMapBothCollections()
    {
        const string json = @"{
			""useFrom"": [
				{ ""resource"": ""shared.json"", ""uses"": [ { ""name"": ""Christmas Day"" } ] }
			],
			""notableDates"": [
				{ ""name"": ""Local Holiday"", ""rules"": [
					{ ""name"": ""Local Holiday Rule"", ""category"": ""Holiday"", ""fixed"": { ""month"": ""March"", ""day"": 15 } }
				] }
			]
		}";

        ParsedNotableDateDocument document = NotableDateRuleJsonParser.ParseDocument(json);

        Assert.AreEqual(1, document.UseGroups.Length);
        Assert.AreEqual("shared.json", document.UseGroups[0].SourceResource);
        Assert.AreEqual(1, document.LocalRules.Length);
        Assert.AreEqual("Local Holiday", document.LocalRules[0].Name);
    }

    /// <summary>
    /// Verifies that multiple <c>useFrom</c> groups are preserved in source order to expose the iterator path in
    /// <see cref="NotableDateRuleJsonParser.ParseDocument(string)" /> that maps each group onto a
    /// <see cref="NotableDateRuleUseGroup" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenDocumentContainsMultipleUseFromGroups_ShouldPreserveGroupOrder()
    {
        const string json = @"{
			""useFrom"": [
				{ ""resource"": ""first.json"", ""useAll"": true },
				{ ""resource"": ""second.json"", ""uses"": [ { ""name"": ""Foo"" } ] },
				{ ""resource"": ""third.json"", ""uses"": [ { ""name"": ""Bar"" } ] }
			]
		}";

        ParsedNotableDateDocument document = NotableDateRuleJsonParser.ParseDocument(json);

        Assert.AreEqual(3, document.UseGroups.Length);
        Assert.AreEqual("first.json", document.UseGroups[0].SourceResource);
        Assert.AreEqual("second.json", document.UseGroups[1].SourceResource);
        Assert.AreEqual("third.json", document.UseGroups[2].SourceResource);
    }

    /// <summary>
    /// Verifies that multiple <c>notableDates</c> entries are returned in source order, exercising the iterator path
    /// in <see cref="NotableDateRuleJsonParser.ParseDocument(string)" /> that maps each notable date entry through the
    /// internal <c>MapNotableDate</c> sequence.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenDocumentContainsMultipleNotableDates_ShouldPreserveNotableDateOrder()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""First"", ""rules"": [
					{ ""name"": ""First Rule"", ""category"": ""Holiday"", ""fixed"": { ""month"": ""January"", ""day"": 1 } }
				] },
				{ ""name"": ""Second"", ""rules"": [
					{ ""name"": ""Second Rule"", ""category"": ""Holiday"", ""fixed"": { ""month"": ""February"", ""day"": 2 } }
				] },
				{ ""name"": ""Third"", ""rules"": [
					{ ""name"": ""Third Rule"", ""category"": ""Holiday"", ""fixed"": { ""month"": ""March"", ""day"": 3 } }
				] }
			]
		}";

        ParsedNotableDateDocument document = NotableDateRuleJsonParser.ParseDocument(json);

        Assert.AreEqual(3, document.LocalRules.Length);
        Assert.AreEqual("First", document.LocalRules[0].Name);
        Assert.AreEqual("Second", document.LocalRules[1].Name);
        Assert.AreEqual("Third", document.LocalRules[2].Name);
    }

    /// <summary>
    /// Verifies that a single notable date containing multiple <c>rules</c> entries expands to one
    /// <see cref="NotableDateRule" /> per rule, that every expanded rule inherits the parent's canonical name, and
    /// that the rules are returned in source order.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenNotableDateContainsMultipleRules_ShouldAssignParentNameAndPreserveRuleOrder()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""Easter"", ""rules"": [
					{ ""name"": ""Easter Sunday Rule"", ""category"": ""Religious"", ""algorithm"": { ""key"": ""easter-sunday"" } },
					{ ""name"": ""Easter Monday Rule"", ""category"": ""Holiday"", ""offsetFromAnchor"": { ""name"": ""Easter Sunday"", ""offset"": 1 } },
					{ ""name"": ""Good Friday Rule"", ""category"": ""Holiday"", ""offsetFromAnchor"": { ""name"": ""Easter Sunday"", ""offset"": -2 } }
				] }
			]
		}";

        ParsedNotableDateDocument document = NotableDateRuleJsonParser.ParseDocument(json);

        Assert.AreEqual(3, document.LocalRules.Length);
        CollectionAssert.AreEqual(
            new[] { "Easter", "Easter", "Easter" },
            document.LocalRules.Select(r => r.Name).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Easter Sunday Rule", "Easter Monday Rule", "Good Friday Rule" },
            document.LocalRules.Select(r => r.RuleName).ToArray());
    }

    /// <summary>
    /// Verifies that a document containing rules of every supported resolution strategy maps each strategy onto the
    /// corresponding <see cref="DateResolutionStrategy" /> and populates its strategy-specific fields. Exercises
    /// the <c>DetectRuleStrategy</c> and <c>ApplyStrategySpecifics</c> paths in a single public call.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenMultipleRulesUseDifferentStrategies_ShouldMapEachStrategyCorrectly()
    {
        const string json = @"{
			""notableDates"": [
				{ ""name"": ""New Year"", ""rules"": [
					{ ""name"": ""New Year Rule"", ""category"": ""Holiday"", ""fixed"": { ""month"": ""January"", ""day"": 1 } }
				] },
				{ ""name"": ""Thanksgiving"", ""rules"": [
					{ ""name"": ""Thanksgiving Rule"", ""category"": ""Holiday"",
					  ""dayOfWeekInMonth"": { ""month"": ""November"", ""dayOfWeek"": ""Thursday"", ""weekOrdinal"": ""Fourth"" } }
				] },
				{ ""name"": ""Good Friday"", ""rules"": [
					{ ""name"": ""Good Friday Rule"", ""category"": ""Holiday"",
					  ""offsetFromAnchor"": { ""name"": ""Easter Sunday"", ""offset"": -2 } }
				] },
				{ ""name"": ""Easter Sunday"", ""rules"": [
					{ ""name"": ""Easter Sunday Rule"", ""category"": ""Religious"",
					  ""algorithm"": { ""key"": ""easter-sunday"" } }
				] }
			]
		}";

        System.Collections.Generic.List<NotableDateRule> rules = NotableDateRuleJsonParser.ParseJson(json);

        Assert.AreEqual(4, rules.Count);
        Assert.AreEqual(DateResolutionStrategy.Fixed, rules[0].Strategy);
        Assert.AreEqual(1, rules[0].Month);
        Assert.AreEqual(1, rules[0].Day);

        Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, rules[1].Strategy);
        Assert.AreEqual(11, rules[1].Month);
        Assert.AreEqual(DayOfWeek.Thursday, rules[1].DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.Fourth, rules[1].WeekOrdinal);

        Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, rules[2].Strategy);
        Assert.AreEqual("Easter Sunday", rules[2].AnchorRuleName);
        Assert.AreEqual(-2, rules[2].OffsetDays);

        Assert.AreEqual(DateResolutionStrategy.Algorithm, rules[3].Strategy);
        Assert.AreEqual("easter-sunday", rules[3].AlgorithmKey);
    }
}
