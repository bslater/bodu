// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.UseDirectives.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies <see cref="NotableDateRuleJsonParser" /> behaviour for <c>useFrom</c> / <c>use</c> directives —
/// directive-level scalar overrides, ordering preservation across multiple groups and directives, and the
/// <c>useAll</c> happy path. Complements the <c>UseFrom</c> resource / name validation tests in
/// <see cref="NotableDateRuleJsonParserTests" /> that focus on schema rejection paths.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that a single <c>useFrom</c> group containing a <c>useAll: true</c> directive sets
    /// <see cref="NotableDateRuleUseGroup.UseAll" /> and produces no per-rule directives.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseFromContainsUseAllDirective_ShouldSetUseAllAndResource()
    {
        const string json = @"{
			""useFrom"": [
				{ ""resource"": ""everything.json"", ""useAll"": true }
			]
		}";

        ParsedNotableDateDocument document = NotableDateRuleJsonParser.ParseDocument(json);

        Assert.AreEqual(1, document.UseGroups.Length);
        Assert.AreEqual("everything.json", document.UseGroups[0].SourceResource);
        Assert.IsTrue(document.UseGroups[0].UseAll);
        Assert.AreEqual(0, document.UseGroups[0].Uses.Length);
    }

    /// <summary>
    /// Verifies that a <c>useFrom</c> group containing several <c>use</c> directives preserves their source order,
    /// exercising the iterator path in <c>MapUseGroup</c>.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseFromContainsMultipleUseDirectives_ShouldPreserveDirectiveOrder()
    {
        const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [
					{ ""name"": ""Alpha"" },
					{ ""name"": ""Bravo"" },
					{ ""name"": ""Charlie"" }
				]
			} ]
		}";

        ParsedNotableDateDocument document = NotableDateRuleJsonParser.ParseDocument(json);
        var directiveNames = document.UseGroups.Single().Uses.Select(u => u.SourceRuleName).ToArray();

        CollectionAssert.AreEqual(new[] { "Alpha", "Bravo", "Charlie" }, directiveNames);
    }

    /// <summary>
    /// Verifies that a <c>use</c> directive's flat scalar overrides — <c>as</c>, <c>category</c>, <c>territory</c>,
    /// <c>nonWorking</c>, year window, occurrence cadence, duration, priority, and comment — propagate onto the
    /// resulting <see cref="NotableDateRuleUseDirective" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseDirectiveSuppliesFullScalarSurface_ShouldMapAllDirectiveScalars()
    {
        const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Christmas Day"",
					""as"": ""Christmas"",
					""category"": ""Holiday"",
					""territory"": ""US"",
					""nonWorking"": true,
					""firstYear"": 1990,
					""lastYear"": 2099,
					""occurrenceYears"": 1,
					""durationDays"": 2,
					""priority"": 5,
					""comment"": ""US Christmas observance""
				} ]
			} ]
		}";

        NotableDateRuleUseDirective directive = NotableDateRuleJsonParser
            .ParseDocument(json)
            .UseGroups.Single()
            .Uses.Single();

        Assert.AreEqual("Christmas Day", directive.SourceRuleName);
        Assert.AreEqual("Christmas", directive.LocalName);
        Assert.AreEqual(NotableDateCategory.Holiday, directive.Category);
        Assert.AreEqual("US", directive.TerritoryCode);
        Assert.AreEqual(true, directive.IsNonWorkingDay);
        Assert.AreEqual(1990, directive.FirstYear);
        Assert.AreEqual(2099, directive.LastYear);
        Assert.AreEqual(1, directive.OccurrenceYears);
        Assert.AreEqual(2, directive.DurationDays);
        Assert.AreEqual(5, directive.Priority);
        Assert.AreEqual("US Christmas observance", directive.Comment);
    }

    /// <summary>
    /// Verifies that a <c>use</c> directive carrying both the directive-level <c>clearTags</c> /
    /// <c>clearAdjustments</c> / <c>clearInherited</c> flags and a nested <c>rule</c> override body populates the
    /// directive's clear flags and the override body together.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseDirectiveSetsClearFlagsAndOverrideBody_ShouldMapBoth()
    {
        const string json = @"{
			""useFrom"": [ {
				""resource"": ""shared.json"",
				""uses"": [ {
					""name"": ""Boxing Day"",
					""clearTags"": true,
					""clearAdjustments"": true,
					""clearInherited"": true,
					""rule"": {
						""name"": ""Boxing Day Rule"",
						""category"": ""Holiday"",
						""fixed"": { ""month"": ""December"", ""day"": 26 }
					}
				} ]
			} ]
		}";

        NotableDateRuleUseDirective directive = NotableDateRuleJsonParser
            .ParseDocument(json)
            .UseGroups.Single()
            .Uses.Single();

        Assert.IsTrue(directive.ClearTags);
        Assert.IsTrue(directive.ClearAdjustments);
        Assert.IsTrue(directive.ClearInherited);
        Assert.IsNotNull(directive.OverrideBody);
        Assert.AreEqual(DateResolutionStrategy.Fixed, directive.OverrideBody!.Strategy);
        Assert.AreEqual(12, directive.OverrideBody.Month);
        Assert.AreEqual(26, directive.OverrideBody.Day);
    }
}
