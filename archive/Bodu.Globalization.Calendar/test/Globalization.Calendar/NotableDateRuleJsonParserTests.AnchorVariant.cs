// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.AnchorVariant.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the JSON parser reads the variant-disambiguating anchor and replacement-target fields so a cookbook
/// author can bind an offset rule or <see cref="AdjustmentAction.ReplaceWithNamedDate" /> adjustment to a specific rule
/// variant rather than the first same-name candidate.
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that the JSON parser maps the optional <c>anchorRuleVariant</c>, <c>anchorTerritory</c>, and
    /// <c>anchorCalendarType</c> properties of an <c>offsetFromAnchor</c> strategy onto the rule.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenOffsetAnchorDeclaresVariant_ShouldMapAnchorReferenceFields()
    {
        var json = @"{ ""notableDates"": [ { ""name"": ""Good Friday"", ""rules"": [ {
            ""name"": ""Good Friday"", ""ruleName"": ""western"", ""category"": ""Holiday"",
            ""offsetFromAnchor"": { ""name"": ""Easter Sunday"", ""offset"": -2, ""anchorRuleVariant"": ""western"",
                ""anchorTerritory"": ""US"", ""anchorCalendarType"": ""System.Globalization.HijriCalendar"" }
        } ] } ] }";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual("Easter Sunday", rule.AnchorRuleName);
        Assert.AreEqual(-2, rule.OffsetDays);
        Assert.AreEqual("western", rule.AnchorRuleVariant);
        Assert.AreEqual("US", rule.AnchorTerritoryCode);
        Assert.AreEqual(typeof(HijriCalendar), rule.AnchorCalendarType);
    }

    /// <summary>
    /// Verifies that the JSON parser leaves the anchor reference fields <see langword="null" /> when only the required
    /// <c>name</c> and <c>offset</c> properties are authored, preserving backward compatibility.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenOffsetAnchorOmitsVariant_ShouldLeaveAnchorReferenceFieldsNull()
    {
        var json = @"{ ""notableDates"": [ { ""name"": ""Good Friday"", ""rules"": [ {
            ""name"": ""Good Friday"", ""category"": ""Holiday"",
            ""offsetFromAnchor"": { ""name"": ""Easter Sunday"", ""offset"": -2 }
        } ] } ] }";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual("Easter Sunday", rule.AnchorRuleName);
        Assert.IsNull(rule.AnchorRuleVariant);
        Assert.IsNull(rule.AnchorTerritoryCode);
        Assert.IsNull(rule.AnchorCalendarType);
    }

    /// <summary>
    /// Verifies that the JSON parser maps the optional <c>targetRuleVariant</c> property of a <c>ReplaceWithNamedDate</c>
    /// adjustment onto <see cref="ObservanceAdjustment.TargetRuleVariant" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenReplacementAdjustmentDeclaresTargetVariant_ShouldMapTargetRuleVariant()
    {
        var json = @"{ ""notableDates"": [ { ""name"": ""Easter Monday"", ""rules"": [ {
            ""name"": ""Easter Monday"", ""category"": ""Holiday"",
            ""fixed"": { ""month"": ""January"", ""day"": 1 },
            ""adjustments"": [ { ""key"": ""sub"", ""when"": ""Always"", ""action"": ""ReplaceWithNamedDate"",
                ""target"": ""Easter Monday"", ""targetRuleVariant"": ""western"" } ]
        } ] } ] }";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual("Easter Monday", rule.Adjustments.Single().TargetRuleName);
        Assert.AreEqual("western", rule.Adjustments.Single().TargetRuleVariant);
    }
}
