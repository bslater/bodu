// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.ParseJson.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that parsing a Fixed rule populates the canonical name from the parent notable date entry.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateName()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual("Fixed Rule Test", rule.Name);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the resolution strategy as <see cref="DateResolutionStrategy.Fixed" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateStrategy()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the explicit category attribute.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateCategory()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(NotableDateCategory.Holiday, rule.Category);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the inclusive year window.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateYearWindow()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(2000, rule.FirstYear);
        Assert.AreEqual(2100, rule.LastYear);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the recurrence cadence.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateOccurrenceYears()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(4, rule.OccurrenceYears);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the multi-day duration with its default of 1 when authored explicitly.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateDurationDays()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(1, rule.DurationDays);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the priority field.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulatePriority()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(5, rule.Priority);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the non-working flag.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateIsNonWorkingDay()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.IsTrue(rule.IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the territory string.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateTerritoryCode()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual("AU-NSW", rule.TerritoryCode);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the calendar type from a fully qualified name.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateCalendarType()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(typeof(System.Globalization.GregorianCalendar), rule.CalendarType);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the freeform comment.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateComment()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual("Fixed rule comment.", rule.Comment);
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule extracts every tag entry into the immutable tag set.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateTags()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(2, rule.Tags.Count);
        Assert.IsTrue(rule.Tags.Contains("Public"));
        Assert.IsTrue(rule.Tags.Contains("Civic"));
    }

    /// <summary>
    /// Verifies that parsing a Fixed rule populates the day and month fields from the <c>fixed</c> strategy element.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenFixedRule_ShouldPopulateDayAndMonth()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(1, rule.Day);
        Assert.AreEqual(1, rule.Month);
    }

    /// <summary>
    /// Verifies that parsing a DayOfWeekInMonth rule populates the strategy and weekday/ordinal fields.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenDayOfWeekInMonthRule_ShouldPopulateStrategyFields()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(DayOfWeekInMonthRuleJson).Single();

        Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, rule.Strategy);
        Assert.AreEqual(5, rule.Month);
        Assert.AreEqual(DayOfWeek.Sunday, rule.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.Second, rule.WeekOrdinal);
    }

    /// <summary>
    /// Verifies that parsing an Algorithm rule populates the strategy and lookup key.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAlgorithmRule_ShouldPopulateAlgorithmKey()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(AlgorithmRuleJson).Single();

        Assert.AreEqual(DateResolutionStrategy.Algorithm, rule.Strategy);
        Assert.AreEqual("easter-sunday", rule.AlgorithmKey);
    }

    /// <summary>
    /// Verifies that parsing an OffsetFromAnchor rule populates the anchor name and integer offset.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenOffsetFromAnchorRule_ShouldPopulateAnchorAndOffset()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(OffsetFromAnchorRuleJson).Single();

        Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, rule.Strategy);
        Assert.AreEqual("Easter Sunday", rule.AnchorRuleName);
        Assert.AreEqual(-2, rule.OffsetDays);
    }

    /// <summary>
    /// Verifies that parsing a multi-rule document returns one rule per child entry.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenMultiRuleJson_ShouldReturnAllRules()
    {
        var rules = NotableDateRuleJsonParser.ParseJson(MultiRuleJson);

        Assert.AreEqual(4, rules.Count);
        CollectionAssert.AreEquivalent(
            new[] { "New Year's Day", "Easter Sunday", "Good Friday", "Anzac Day" },
            rules.Select(r => r.Name).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleJsonParser.ParseDocument(string)" /> exposes <c>useFrom</c> directives in the
    /// returned <see cref="ParsedNotableDateDocument" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenCherryPickJson_ShouldExposeUseGroups()
    {
        var document = NotableDateRuleJsonParser.ParseDocument(CherryPickJson);

        Assert.AreEqual(2, document.UseGroups.Length);
        Assert.AreEqual("Bodu.Globalization.Calendar.Resources.Common.xml", document.UseGroups[0].SourceResource);
        Assert.AreEqual(2, document.UseGroups[0].Uses.Length);
        Assert.AreEqual("New Year's Day", document.UseGroups[0].Uses[0].SourceRuleName);
        Assert.AreEqual("US", document.UseGroups[0].Uses[0].TerritoryCode);
        Assert.AreEqual("Christmas", document.UseGroups[1].Uses[0].LocalName);
    }

    /// <summary>
    /// Verifies that <c>useAll</c> propagates onto the parsed <see cref="NotableDateRuleUseGroup" /> as
    /// <see cref="NotableDateRuleUseGroup.UseAll" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenUseAllJson_ShouldSetUseAllFlag()
    {
        var document = NotableDateRuleJsonParser.ParseDocument(UseAllJson);

        Assert.AreEqual(1, document.UseGroups.Length);
        Assert.IsTrue(document.UseGroups[0].UseAll);
        Assert.AreEqual(0, document.UseGroups[0].Uses.Length);
    }

    /// <summary>
    /// Verifies that a rule whose <c>category</c> attribute is <c>"Religious"</c> is parsed as
    /// <see cref="NotableDateCategory.Religious" /> rather than silently falling back to <see cref="NotableDateCategory.None" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenCategoryIsReligious_ShouldReturnReligiousCategory()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(ReligiousRuleJson).Single();

        Assert.AreEqual(NotableDateCategory.Religious, rule.Category);
    }

    /// <summary>
    /// Verifies that a rule whose <c>category</c> attribute is <c>"Civic"</c> is parsed as
    /// <see cref="NotableDateCategory.Civic" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenCategoryIsCivic_ShouldReturnCivicCategory()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(CivicRuleJson).Single();

        Assert.AreEqual(NotableDateCategory.Civic, rule.Category);
    }

    /// <summary>
    /// Verifies that an adjustment block populates trigger, action, and priority on the parsed
    /// <see cref="ObservanceAdjustment" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentPresent_ShouldPopulateAdjustmentFields()
    {
        var rule = NotableDateRuleJsonParser.ParseJson(FixedRuleJson).Single();

        Assert.AreEqual(1, rule.Adjustments.Length);
        var adjustment = rule.Adjustments[0];
        Assert.AreEqual("weekend-roll", adjustment.Key);
        Assert.AreEqual(AdjustmentTrigger.IfWeekend, adjustment.Trigger);
        Assert.AreEqual(AdjustmentAction.MoveToNextWeekday, adjustment.Action);
        Assert.AreEqual(10, adjustment.Priority);
    }
}
