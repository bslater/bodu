// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.FromXml.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// The recognized value of every enumeration-valued attribute the XML enumeration test document holds, keyed by the
    /// placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_xmlEnumFieldValues = new(StringComparer.Ordinal)
    {
        ["duplicatePolicy"] = "Error",
        ["sameDayCollisionPolicy"] = "KeepAll",
        ["spanCollisionPolicy"] = "KeepAll",
        ["priorityDirection"] = "HigherWins",
        ["observedDateRangePolicy"] = "ObservedOccurrenceControlsInclusion",
        ["categoryPrecedence"] = "PublicHoliday",
        ["scope.calendars"] = "Gregorian",
        ["scope.categories"] = "PublicHoliday",
        ["trigger.type"] = "IfDayOfWeek",
        ["trigger.weekdays"] = "Saturday",
        ["trigger.weekOrdinal"] = "First",
        ["action.type"] = "MoveToNextWeekday",
        ["action.dayOfWeek"] = "Monday",
        ["emission.mode"] = "ObservedOnly",
        ["use.category"] = "Cultural",
        ["notableDate.category"] = "PublicHoliday",
        ["rule.category"] = "Observance",
        ["applicability.calendar"] = "Gregorian",
        ["untilDate.startBoundary"] = "Inclusive",
        ["untilDate.endBoundary"] = "Exclusive",
        ["untilDate.selection"] = "FirstAfterStart",
        ["patch.category"] = "Civic",
    };

    /// <summary>
    /// Verifies that the XML enumeration test document reads and builds when every enumeration-valued attribute holds a
    /// recognized value, so each rejection the per-attribute test observes is caused by the one value it replaces.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenEveryEnumValueRecognized_ShouldReadAndBuild()
    {
        NotableDateResource resource = NotableDateDocumentBuilder.FromXml(EnumFieldXml(null, null)).Build(BuilderEnumImportResolver);

        Assert.AreEqual("test.enums", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that an enumeration-valued XML attribute holding a value that names no member is rejected with a
    /// <see cref="FormatException" /> naming the value, as the loader rejects it, rather than read as the enumeration's
    /// default or dropped.
    /// </summary>
    /// <param name="field">The placeholder of the attribute holding the unrecognized value.</param>
    [TestMethod]
    [DataRow("duplicatePolicy")]
    [DataRow("sameDayCollisionPolicy")]
    [DataRow("spanCollisionPolicy")]
    [DataRow("priorityDirection")]
    [DataRow("observedDateRangePolicy")]
    [DataRow("categoryPrecedence")]
    [DataRow("scope.calendars")]
    [DataRow("scope.categories")]
    [DataRow("trigger.type")]
    [DataRow("trigger.weekdays")]
    [DataRow("trigger.weekOrdinal")]
    [DataRow("action.type")]
    [DataRow("action.dayOfWeek")]
    [DataRow("emission.mode")]
    [DataRow("use.category")]
    [DataRow("notableDate.category")]
    [DataRow("rule.category")]
    [DataRow("applicability.calendar")]
    [DataRow("untilDate.startBoundary")]
    [DataRow("untilDate.endBoundary")]
    [DataRow("untilDate.selection")]
    [DataRow("patch.category")]
    public void FromXml_WhenEnumValueUnrecognized_ShouldThrowFormatException(string field)
    {
        string xml = EnumFieldXml(field, "Bogus");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromXml(xml);
        });

        Assert.Contains("Bogus", ex.Message);
    }

    /// <summary>
    /// Verifies that an enumeration-valued XML attribute holding a number, whether or not it is a member's value, is
    /// rejected with a <see cref="FormatException" />, as the schema and the loader accept only member names.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    [TestMethod]
    [DataRow("1")]
    [DataRow("99")]
    public void FromXml_WhenEnumValueIsNumeric_ShouldThrowFormatException(string value)
    {
        string xml = EnumFieldXml("notableDate.category", value);

        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromXml(xml);
        });
    }

    /// <summary>
    /// Verifies that an enumeration-valued XML attribute naming a member in a different case reads as that member, as
    /// the loader reads member names ignoring case.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenEnumValueDiffersInCase_ShouldReadTheNamedMember()
    {
        string xml = EnumFieldXml("sameDayCollisionPolicy", "highestpriorityonly");

        NotableDateResource resource = NotableDateDocumentBuilder.FromXml(xml).Build(BuilderEnumImportResolver);

        Assert.AreEqual(CollisionPolicy.HighestPriorityOnly, resource.ResolutionPolicy.SameDayCollisionPolicy);
    }

    /// <summary>
    /// Verifies that a resolution policy whose <c>workingDays</c> is a letter mask, which <see cref="DayOfWeekSet" />
    /// reads but the document format does not define, is rejected with a <see cref="FormatException" />, as the loader
    /// rejects it, rather than read as that working week.
    /// </summary>
    /// <param name="workingDays">The letter mask.</param>
    [TestMethod]
    [DataRow("_MTWTF_")]
    [DataRow("MTWTF__")]
    [DataRow("-MTWTF-")]
    public void FromXml_WhenWorkingDaysIsLetterMask_ShouldThrowFormatException(string workingDays)
    {
        string xml = WorkingDaysXml(workingDays);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromXml(xml);
        });

        Assert.Contains(workingDays, ex.Message);
    }

    /// <summary>
    /// Verifies that a resolution policy whose <c>workingDays</c> is not a seven-character working-week pattern is
    /// rejected with a <see cref="FormatException" />, as the loader rejects it, rather than dropped in favour of the
    /// default working week.
    /// </summary>
    /// <param name="workingDays">The malformed working-week pattern.</param>
    [TestMethod]
    [DataRow("MTWTF")]
    [DataRow("01111100")]
    [DataRow("0111112")]
    public void FromXml_WhenWorkingDaysMalformed_ShouldThrowFormatException(string workingDays)
    {
        string xml = WorkingDaysXml(workingDays);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromXml(xml);
        });

        Assert.Contains(workingDays, ex.Message);
    }

    /// <summary>
    /// Verifies that a resolution policy whose <c>workingDays</c> is the documented Sunday-first binary pattern reads as
    /// that working week.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenWorkingDaysIsBinary_ShouldReadTheWorkingWeek()
    {
        NotableDateResource resource = NotableDateDocumentBuilder.FromXml(WorkingDaysXml("1111100")).Build();

        Assert.AreEqual(DayOfWeekSet.SundayToThursday, resource.ResolutionPolicy.WorkingWeek);
    }

    /// <summary>
    /// Resolves the one resource the enumeration test documents import.
    /// </summary>
    /// <param name="name">The imported resource's name.</param>
    /// <returns>The imported resource's JSON, or <see langword="null" /> for any other name.</returns>
    private static string? BuilderEnumImportResolver(string name) =>
        name == "lib"
            ? """
              { "schemaVersion": "1.0", "resourceId": "test.lib",
                "notableDates": [ { "id": "lib-day", "displayName": "Library Day", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "fixed": { "month": 8, "day": 8 } } } ] } ] }
              """
            : null;

    /// <summary>
    /// Builds an XML document holding every enumeration-valued attribute the builder reads, each set to its recognized
    /// value except the one named, which holds the supplied value.
    /// </summary>
    /// <param name="field">The placeholder of the attribute to set, or <see langword="null" /> to set none.</param>
    /// <param name="value">The value the named attribute holds.</param>
    /// <returns>The XML document.</returns>
    private static string EnumFieldXml(string? field, string? value)
    {
        string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.enums">
              <ResolutionPolicy duplicatePolicy="@duplicatePolicy@" sameDayCollisionPolicy="@sameDayCollisionPolicy@"
                                spanCollisionPolicy="@spanCollisionPolicy@" priorityDirection="@priorityDirection@"
                                observedDateRangePolicy="@observedDateRangePolicy@">
                <CategoryPrecedence><Category value="@categoryPrecedence@" /></CategoryPrecedence>
              </ResolutionPolicy>
              <AdjustmentPolicies>
                <AdjustmentPolicy id="policy">
                  <Scope><Calendar name="@scope.calendars@" /><Category value="@scope.categories@" /></Scope>
                  <Trigger type="@trigger.type@" weekOrdinal="@trigger.weekOrdinal@"><Weekday value="@trigger.weekdays@" /></Trigger>
                  <Action type="@action.type@" dayOfWeek="@action.dayOfWeek@" />
                  <Emission mode="@emission.mode@" />
                </AdjustmentPolicy>
              </AdjustmentPolicies>
              <Imports>
                <Import resource="lib"><Use notableDateRef="lib-day" category="@use.category@" /></Import>
              </Imports>
              <NotableDates>
                <NotableDate id="anchor" displayName="Anchor" category="@notableDate.category@">
                  <Rules>
                    <Rule id="r" category="@rule.category@">
                      <Applicability calendar="@applicability.calendar@" />
                      <Strategy><Fixed month="March" day="1" /></Strategy>
                      <Duration>
                        <UntilDate startBoundary="@untilDate.startBoundary@" endBoundary="@untilDate.endBoundary@" selection="@untilDate.selection@">
                          <Strategy><Fixed month="March" day="5" /></Strategy>
                        </UntilDate>
                      </Duration>
                      <Adjustments><Adjustment policyRef="policy" /></Adjustments>
                    </Rule>
                  </Rules>
                </NotableDate>
              </NotableDates>
              <Overrides>
                <PatchRule notableDateRef="anchor" ruleRef="r" category="@patch.category@" />
              </Overrides>
            </NotableDateResource>
            """;

        foreach ((string key, string recognized) in s_xmlEnumFieldValues)
            xml = xml.Replace("@" + key + "@", key == field ? value : recognized, StringComparison.Ordinal);

        return xml;
    }

    /// <summary>
    /// Builds an XML document with one fixed concept whose resolution policy declares the supplied working week.
    /// </summary>
    /// <param name="workingDays">The <c>workingDays</c> value.</param>
    /// <returns>The XML document.</returns>
    private static string WorkingDaysXml(string workingDays) =>
        """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.working-days">
          <ResolutionPolicy workingDays="@workingDays@" />
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="March" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """.Replace("@workingDays@", workingDays, StringComparison.Ordinal);
}
