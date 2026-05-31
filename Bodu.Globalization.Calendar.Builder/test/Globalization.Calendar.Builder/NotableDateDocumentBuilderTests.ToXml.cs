// // ---------------------------------------------------------------------------------------------------------------
// // <copyright file="NotableDateDocumentBuilderTests.ToXml.cs" company="Bodu Pty. Ltd.">
// //     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// // </copyright>
// // ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    private static readonly XNamespace s_calendarNs = XNamespace.Get("urn:bodu:globalization:calendar");

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToXml" /> produces XML that is accepted by
    /// <see cref="NotableDateRuleParser.ParseXml(string)" /> and round-trips the rule properties correctly.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenFixedRule_ShouldProduceSchemaValidXmlRoundTrip()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .Fixed(12, 25)
                    .AddAdjustment("weekend-roll", adj => adj
                        .When(AdjustmentTrigger.IfWeekend)
                        .Action(AdjustmentAction.MoveToNextWeekday)
                        .NonWorking())));

        var xml = builder.ToXml();
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.HasCount(1, parsed);
        NotableDateRule rule = parsed[0];
        Assert.AreEqual("Christmas Day", rule.Name);
        Assert.AreEqual(DateResolutionStrategy.Fixed, rule.Strategy);
        Assert.AreEqual(NotableDateCategory.Holiday, rule.Category);
        Assert.IsTrue(rule.IsNonWorkingDay);
        Assert.AreEqual(12, rule.Month);
        Assert.AreEqual(25, rule.Day);
        Assert.HasCount(1, rule.Adjustments);
        Assert.AreEqual("weekend-roll", rule.Adjustments[0].Key);
        Assert.AreEqual(AdjustmentTrigger.IfWeekend, rule.Adjustments[0].Trigger);
        Assert.AreEqual(AdjustmentAction.MoveToNextWeekday, rule.Adjustments[0].Action);
    }

    /// <summary>
    /// Verifies that the <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> strategy serialises and parses
    /// back without data loss.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenDayOfWeekInMonthStrategy_ShouldRoundTripCorrectly()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Thanksgiving", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Territory("US")
                    .DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOfMonthOrdinal.Fourth)));

        var xml = builder.ToXml();
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.HasCount(1, parsed);
        NotableDateRule rule = parsed[0];
        Assert.AreEqual(DateResolutionStrategy.DayOfWeekInMonth, rule.Strategy);
        Assert.AreEqual(11, rule.Month);
        Assert.AreEqual(DayOfWeek.Thursday, rule.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.Fourth, rule.WeekOrdinal);
        Assert.AreEqual("US", rule.TerritoryCode);
    }

    /// <summary>
    /// Verifies that the <see cref="DateResolutionStrategy.OffsetFromAnchor" /> strategy serialises and parses
    /// back without data loss.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenOffsetFromAnchorStrategy_ShouldRoundTripCorrectly()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Easter Monday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .OffsetFromAnchor("Easter Sunday", 1)));

        var xml = builder.ToXml();
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.HasCount(1, parsed);
        NotableDateRule rule = parsed[0];
        Assert.AreEqual(DateResolutionStrategy.OffsetFromAnchor, rule.Strategy);
        Assert.AreEqual("Easter Sunday", rule.AnchorRuleName);
        Assert.AreEqual(1, rule.OffsetDays);
    }

    /// <summary>
    /// Verifies that the <see cref="DateResolutionStrategy.Algorithm" /> strategy serialises and parses back
    /// without data loss when only an algorithm key is supplied.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenAlgorithmStrategy_ShouldRoundTripCorrectly()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Easter Sunday", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Algorithm(key: "easter-gregorian")));

        var xml = builder.ToXml();
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.HasCount(1, parsed);
        Assert.AreEqual(DateResolutionStrategy.Algorithm, parsed[0].Strategy);
        Assert.AreEqual("easter-gregorian", parsed[0].AlgorithmKey);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToXml" /> emits one <c>&lt;NotableDate&gt;</c> element per
    /// added date entry, each with the correct <c>name</c> attribute.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenMultipleDates_ShouldProduceAllNotableDateElements()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(12, 25)))
            .AddDate("Boxing Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(12, 26)));

        var doc = builder.ToXDocument();
        var notableDates = doc.Descendants(s_calendarNs + "NotableDate").ToList();

        Assert.HasCount(2, notableDates);
        Assert.AreEqual("Christmas Day", notableDates[0].Attribute("name")?.Value);
        Assert.AreEqual("Boxing Day", notableDates[1].Attribute("name")?.Value);
    }

    /// <summary>
    /// Verifies that tags added via <see cref="NotableDateRuleBuilder.AddTag" /> are serialised as <c>&lt;Tag&gt;</c>
    /// child elements and round-trip through the parser.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenRuleHasTags_ShouldIncludeTagElements()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Religious)
                    .Fixed(12, 25)
                    .AddTag("Christian")
                    .AddTag("Public")));

        var xml = builder.ToXml();
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.HasCount(1, parsed);
        Assert.Contains("Christian", parsed[0].Tags);
        Assert.Contains("Public", parsed[0].Tags);
    }

    /// <summary>
    /// Verifies that optional scalar rule properties (firstYear, lastYear, durationDays, priority, territory,
    /// comment) are serialised as XML attributes and round-trip through the parser.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenRuleHasOptionalScalars_ShouldIncludeAllOptionalAttributes()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Observance)
                    .Territory("AU")
                    .FirstYear(2000)
                    .LastYear(2099)
                    .Duration(3)
                    .Priority(50)
                    .Comment("Test remark")
                    .Fixed(7, 4)));

        var xml = builder.ToXml();
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.HasCount(1, parsed);
        NotableDateRule rule = parsed[0];
        Assert.AreEqual("AU", rule.TerritoryCode);
        Assert.AreEqual(2000, rule.FirstYear);
        Assert.AreEqual(2099, rule.LastYear);
        Assert.AreEqual(3, rule.DurationDays);
        Assert.AreEqual(50, rule.Priority);
        Assert.AreEqual("Test remark", rule.Comment);
    }

    /// <summary>
    /// Verifies that a non-zero <see cref="ObservanceAdjustmentBuilder.OffsetDays" /> value is serialised as the
    /// <c>days</c> attribute and round-trips through the parser.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenAdjustmentHasOffsetDays_ShouldIncludeDaysAttribute()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("shift", adj => adj
                        .When(AdjustmentTrigger.Always)
                        .Action(AdjustmentAction.AddDays)
                        .OffsetDays(-1))));

        var xml = builder.ToXml();
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.HasCount(1, parsed);
        Assert.AreEqual(-1, parsed[0].Adjustments[0].OffsetDays);
    }

    /// <summary>
    /// Verifies that <c>skipLeapMonth="true"</c> is serialised and round-trips correctly.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenFixedRuleWithSkipLeapMonth_ShouldRoundTripSkipLeapMonthTrue()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Dragon Boat Festival", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Cultural)
                    .Fixed(5, 5, skipLeapMonth: true)));

        var xml = builder.ToXml();
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.HasCount(1, parsed);
        Assert.IsTrue(parsed[0].SkipLeapMonth);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToXDocument" /> returns an <see cref="XDocument" /> whose root
    /// element is in the <c>urn:bodu:globalization:calendar</c> namespace.
    /// </summary>
    [TestMethod]
    public void ToXDocument_WhenValid_ShouldReturnDocumentWithCalendarNamespace()
    {
        var doc = NotableDateDocumentBuilder.Create()
            .AddDate("Test", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(1, 1)))
            .ToXDocument();

        Assert.IsNotNull(doc.Root);
        Assert.AreEqual("urn:bodu:globalization:calendar", doc.Root.Name.NamespaceName);
        Assert.AreEqual("NotableDates", doc.Root.Name.LocalName);
    }

    /// <summary>
    /// Verifies that values supplied via <see cref="ObservanceAdjustmentBuilder.AddHandlerParameter" /> and
    /// <see cref="ObservanceAdjustmentBuilder.MaxAdjustmentReachDays" /> are omitted from the serialised XML
    /// (they are programmatic-only properties not defined by the schema).
    /// </summary>
    [TestMethod]
    public void ToXml_WhenAdjustmentHasProgrammaticOnlyFields_ShouldOmitFromXml()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .Fixed(1, 1)
                    .AddAdjustment("custom", adj => adj
                        .When(AdjustmentTrigger.Custom)
                        .Action(AdjustmentAction.Custom)
                        .HandlerKey("my-handler")
                        .AddHandlerParameter("enabled", "true")
                        .MaxAdjustmentReachDays(45))));

        var doc = builder.ToXDocument();
        XElement adjElement = doc.Descendants(s_calendarNs + "Adjustment").Single();

        Assert.IsNull(adjElement.Attribute("handlerParameters"));
        Assert.IsNull(adjElement.Attribute("maxAdjustmentReachDays"));

        // The XML must still round-trip cleanly through the parser.
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(builder.ToXml());
        ObservanceAdjustment adj = parsed[0].Adjustments[0];
        Assert.AreEqual("my-handler", adj.HandlerKey);
        Assert.IsNull(adj.HandlerParameters);
        Assert.IsNull(adj.MaxAdjustmentReachDays);
    }

    /// <summary>
    /// Verifies that a comparison date set via <see cref="ObservanceAdjustmentBuilder.ComparisonDate" /> is serialised
    /// as <c>comparisonMonth</c> and <c>comparisonDay</c> attributes and round-trips through the parser.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenAdjustmentHasComparisonDate_ShouldSerialiseComparisonMonthAndDay()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Test", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Seasonal)
                    .Fixed(3, 1)
                    .AddAdjustment("pre-equinox", adj => adj
                        .When(AdjustmentTrigger.IfBeforeFixedDate)
                        .Action(AdjustmentAction.AddDays)
                        .ComparisonDate(3, 20)
                        .OffsetDays(1))));

        var xml = builder.ToXml();
        List<NotableDateRule> parsed = NotableDateRuleParser.ParseXml(xml);

        Assert.HasCount(1, parsed);
        ObservanceAdjustment adj = parsed[0].Adjustments[0];
        Assert.IsNotNull(adj.ComparisonDate);
        Assert.AreEqual(3, adj.ComparisonDate!.Value.Month);
        Assert.AreEqual(20, adj.ComparisonDate!.Value.Day);
    }
}
