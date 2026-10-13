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

    /// <summary>
    /// The well-formed value of every integer-, Boolean- and month-valued attribute the XML scalar test document holds,
    /// keyed by the placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_xmlScalarFieldValues = new(StringComparer.Ordinal)
    {
        ["policy.priority"] = "5",
        ["scope.fromYear"] = "2000",
        ["scope.toYear"] = "2100",
        ["scope.onlyYears"] = "2026",
        ["scope.exceptYears"] = "2027",
        ["trigger.month"] = "June",
        ["trigger.day"] = "15",
        ["action.days"] = "1",
        ["action.maxSearchDays"] = "7",
        ["action.skipWeekends"] = "true",
        ["action.skipNonWorkingDates"] = "false",
        ["emission.nonWorking"] = "true",
        ["use.nonWorking"] = "true",
        ["notableDate.defaultDurationDays"] = "1",
        ["notableDate.defaultNonWorkingDay"] = "true",
        ["rule.priority"] = "3",
        ["rule.nonWorking"] = "true",
        ["rule.durationDays"] = "2",
        ["applicability.fromYear"] = "2000",
        ["applicability.toYear"] = "2100",
        ["applicability.everyYears"] = "1",
        ["applicability.anchorYear"] = "2000",
        ["applicability.onlyYears"] = "2026",
        ["applicability.exceptYears"] = "2027",
        ["patch.priority"] = "4",
        ["patch.nonWorking"] = "false",
        ["patch.durationDays"] = "1",
    };

    /// <summary>
    /// Verifies that the XML scalar test document reads and builds when every integer-, Boolean- and month-valued
    /// attribute is well-formed, so each rejection the per-attribute tests observe is caused by the one value they
    /// replace.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenEveryScalarValueWellFormed_ShouldReadAndBuild()
    {
        NotableDateResource resource = NotableDateDocumentBuilder.FromXml(ScalarFieldXml(null, null)).Build(BuilderEnumImportResolver);

        Assert.AreEqual("test.scalars", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that an integer-valued XML attribute holding text that is not an integer is rejected with a
    /// <see cref="FormatException" /> naming the value, as the loader rejects it, rather than dropped.
    /// </summary>
    /// <param name="field">The placeholder of the attribute holding the malformed value.</param>
    [TestMethod]
    [DataRow("policy.priority")]
    [DataRow("scope.fromYear")]
    [DataRow("scope.toYear")]
    [DataRow("scope.onlyYears")]
    [DataRow("scope.exceptYears")]
    [DataRow("trigger.day")]
    [DataRow("action.days")]
    [DataRow("action.maxSearchDays")]
    [DataRow("notableDate.defaultDurationDays")]
    [DataRow("rule.priority")]
    [DataRow("rule.durationDays")]
    [DataRow("applicability.fromYear")]
    [DataRow("applicability.toYear")]
    [DataRow("applicability.everyYears")]
    [DataRow("applicability.anchorYear")]
    [DataRow("applicability.onlyYears")]
    [DataRow("applicability.exceptYears")]
    [DataRow("patch.priority")]
    [DataRow("patch.durationDays")]
    public void FromXml_WhenIntegerValueMalformed_ShouldThrowFormatException(string field)
    {
        string xml = ScalarFieldXml(field, "high");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromXml(xml);
        });

        Assert.Contains("high", ex.Message);
    }

    /// <summary>
    /// Verifies that an empty integer-valued XML attribute, which the loader reports as a value that does not parse, is
    /// rejected with a <see cref="FormatException" /> rather than read as absent.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenIntegerValueEmpty_ShouldThrowFormatException()
    {
        string xml = ScalarFieldXml("rule.priority", string.Empty);

        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromXml(xml);
        });
    }

    /// <summary>
    /// Verifies that a Boolean-valued XML attribute holding text that is not a schema Boolean is rejected with a
    /// <see cref="FormatException" /> naming the value, as the loader rejects it, rather than read as
    /// <see langword="false" />.
    /// </summary>
    /// <param name="field">The placeholder of the attribute holding the malformed value.</param>
    [TestMethod]
    [DataRow("action.skipWeekends")]
    [DataRow("action.skipNonWorkingDates")]
    [DataRow("emission.nonWorking")]
    [DataRow("use.nonWorking")]
    [DataRow("notableDate.defaultNonWorkingDay")]
    [DataRow("rule.nonWorking")]
    [DataRow("patch.nonWorking")]
    public void FromXml_WhenBooleanValueMalformed_ShouldThrowFormatException(string field)
    {
        string xml = ScalarFieldXml(field, "maybe");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromXml(xml);
        });

        Assert.Contains("maybe", ex.Message);
    }

    /// <summary>
    /// Verifies that a Boolean-valued XML attribute in a case the schema does not define is rejected with a
    /// <see cref="FormatException" />, as the loader rejects it, rather than read as <see langword="true" />.
    /// </summary>
    /// <param name="value">The Boolean in an undefined case.</param>
    [TestMethod]
    [DataRow("TRUE")]
    [DataRow("True")]
    public void FromXml_WhenBooleanValueCaseDiffers_ShouldThrowFormatException(string value)
    {
        string xml = ScalarFieldXml("rule.nonWorking", value);

        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromXml(xml);
        });
    }

    /// <summary>
    /// Verifies that an empty adjustment trigger <c>month</c>, which the loader reports as a value that does not parse,
    /// is rejected with a <see cref="FormatException" /> rather than read as absent.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenTriggerMonthEmpty_ShouldThrowFormatException()
    {
        string xml = ScalarFieldXml("trigger.month", string.Empty);

        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromXml(xml);
        });
    }

    /// <summary>
    /// Builds an XML document holding every integer-, Boolean- and month-valued attribute the builder reads, each set to
    /// its well-formed value except the one named, which holds the supplied value.
    /// </summary>
    /// <param name="field">The placeholder of the attribute to set, or <see langword="null" /> to set none.</param>
    /// <param name="value">The value the named attribute holds.</param>
    /// <returns>The XML document.</returns>
    private static string ScalarFieldXml(string? field, string? value)
    {
        string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.scalars">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="policy" priority="@policy.priority@">
                  <Scope fromYear="@scope.fromYear@" toYear="@scope.toYear@">
                    <OnlyYear value="@scope.onlyYears@" />
                    <ExceptYear value="@scope.exceptYears@" />
                  </Scope>
                  <Trigger type="IfBeforeFixedDate" month="@trigger.month@" day="@trigger.day@" />
                  <Action type="AddDays" days="@action.days@" maxSearchDays="@action.maxSearchDays@"
                          skipWeekends="@action.skipWeekends@" skipNonWorkingDates="@action.skipNonWorkingDates@" />
                  <Emission mode="ObservedOnly" nonWorking="@emission.nonWorking@" />
                </AdjustmentPolicy>
              </AdjustmentPolicies>
              <Imports>
                <Import resource="lib"><Use notableDateRef="lib-day" nonWorking="@use.nonWorking@" /></Import>
              </Imports>
              <NotableDates>
                <NotableDate id="anchor" displayName="Anchor" category="PublicHoliday"
                             defaultDurationDays="@notableDate.defaultDurationDays@" defaultNonWorkingDay="@notableDate.defaultNonWorkingDay@">
                  <Rules>
                    <Rule id="r" priority="@rule.priority@" nonWorking="@rule.nonWorking@" durationDays="@rule.durationDays@">
                      <Applicability fromYear="@applicability.fromYear@" toYear="@applicability.toYear@"
                                     everyYears="@applicability.everyYears@" anchorYear="@applicability.anchorYear@">
                        <OnlyYear value="@applicability.onlyYears@" />
                        <ExceptYear value="@applicability.exceptYears@" />
                      </Applicability>
                      <Strategy><Fixed month="March" day="1" /></Strategy>
                      <Adjustments><Adjustment policyRef="policy" /></Adjustments>
                    </Rule>
                  </Rules>
                </NotableDate>
                <NotableDate id="other" displayName="Other" category="Observance">
                  <Rules><Rule id="r"><Strategy><Fixed month="April" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
              <Overrides>
                <PatchRule notableDateRef="other" ruleRef="r" priority="@patch.priority@" nonWorking="@patch.nonWorking@" durationDays="@patch.durationDays@" />
              </Overrides>
            </NotableDateResource>
            """;

        foreach ((string key, string wellFormed) in s_xmlScalarFieldValues)
            xml = xml.Replace("@" + key + "@", key == field ? value : wellFormed, StringComparison.Ordinal);

        return xml;
    }
}
