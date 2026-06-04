// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserMonthTokenTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the month-token parsing surface of <see cref="NotableDateResourceLoader" /> on the <c>Fixed</c> strategy:
/// numeric months, full English month names, Hebrew month names and leap-dependent aliases under a Hebrew-calendar rule,
/// and out-of-range tokens. Ported from the v1 <c>NotableDateRuleParserTests.MonthToken</c> /
/// <c>NotableDateRuleJsonParserTests.MonthToken</c> rows, mapped onto the v2 <see cref="FixedDateStrategy" /> surface
/// (<see cref="FixedDateStrategy.Month" /> / <see cref="FixedDateStrategy.MonthAlias" />).
/// </summary>
[TestClass]
public sealed class ParserMonthTokenTests
{
    /// <summary>
    /// Builds a one-concept XML document with a Gregorian <c>Fixed</c> rule whose month token is the supplied value.
    /// </summary>
    /// <param name="month">The month token to author.</param>
    /// <param name="day">The day to author.</param>
    /// <returns>The XML document content.</returns>
    private static string GregorianFixedXml(string month, int day = 1) =>
        $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.month">
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="{month}" day="{day}" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a one-concept XML document with a Hebrew <c>Fixed</c> rule whose month token is the supplied value.
    /// </summary>
    /// <param name="month">The Hebrew month token to author.</param>
    /// <param name="day">The day to author.</param>
    /// <returns>The XML document content.</returns>
    private static string HebrewFixedXml(string month, int day = 1) =>
        $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.month">
          <NotableDates>
            <NotableDate id="x" displayName="X" category="Religious">
              <Rules><Rule id="r"><Applicability calendar="Hebrew" /><Strategy><Fixed month="{month}" day="{day}" sweepCalendarYears="true" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Returns the single rule's strategy as a <see cref="FixedDateStrategy" />.
    /// </summary>
    /// <param name="resource">The loaded resource.</param>
    /// <returns>The fixed-date strategy.</returns>
    private static FixedDateStrategy Strategy(NotableDateResource resource) =>
        (FixedDateStrategy)resource.NotableDates.Single().Rules.Single().Strategy;

    /// <summary>
    /// Verifies that a Gregorian <c>Fixed</c> rule authored with a numeric month token resolves to the matching integer
    /// month with no alias, in both XML and JSON.
    /// </summary>
    [TestMethod]
    public void Load_WhenGregorianFixedMonthIsNumeric_ShouldResolveNumericMonth()
    {
        FixedDateStrategy xml = Strategy(NotableDateResourceLoader.Load(GregorianFixedXml("7", 4)));
        FixedDateStrategy json = Strategy(NotableDateResourceLoader.LoadJson(
            """
            { "schemaVersion": "1.0", "resourceId": "test.month",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 7, "day": 4 } } } ] } ] }
            """));

        Assert.AreEqual(7, xml.Month, "XML month");
        Assert.AreEqual(4, xml.Day, "XML day");
        Assert.IsNull(xml.MonthAlias, "XML alias");
        Assert.AreEqual(7, json.Month, "JSON month");
        Assert.IsNull(json.MonthAlias, "JSON alias");
    }

    /// <summary>
    /// Verifies that every supported English Gregorian month name on a <c>Fixed</c> strategy resolves to the matching
    /// integer 1..12 with no alias, in both XML and JSON.
    /// </summary>
    /// <param name="token">The English month name.</param>
    /// <param name="expected">The expected one-based month.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("January", 1)]
    [DataRow("February", 2)]
    [DataRow("March", 3)]
    [DataRow("April", 4)]
    [DataRow("May", 5)]
    [DataRow("June", 6)]
    [DataRow("July", 7)]
    [DataRow("August", 8)]
    [DataRow("September", 9)]
    [DataRow("October", 10)]
    [DataRow("November", 11)]
    [DataRow("December", 12)]
    public void Load_WhenGregorianFixedMonthIsEnglishName_ShouldResolveExpectedMonth(string token, int expected)
    {
        FixedDateStrategy xml = Strategy(NotableDateResourceLoader.Load(GregorianFixedXml(token)));
        FixedDateStrategy json = Strategy(NotableDateResourceLoader.LoadJson(
            $$"""
            { "schemaVersion": "1.0", "resourceId": "test.month",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": "{{token}}", "day": 1 } } } ] } ] }
            """));

        Assert.AreEqual(expected, xml.Month, "XML month");
        Assert.IsNull(xml.MonthAlias, "XML alias");
        Assert.AreEqual(expected, json.Month, "JSON month");
        Assert.IsNull(json.MonthAlias, "JSON alias");
    }

    /// <summary>
    /// Verifies that every string-form numeric month token <c>"1"</c> through <c>"12"</c> resolves to the matching
    /// integer in the default Gregorian context.
    /// </summary>
    /// <param name="token">The numeric month token.</param>
    /// <param name="expected">The expected one-based month.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("1", 1)]
    [DataRow("2", 2)]
    [DataRow("3", 3)]
    [DataRow("4", 4)]
    [DataRow("5", 5)]
    [DataRow("6", 6)]
    [DataRow("7", 7)]
    [DataRow("8", 8)]
    [DataRow("9", 9)]
    [DataRow("10", 10)]
    [DataRow("11", 11)]
    [DataRow("12", 12)]
    public void Load_WhenGregorianFixedMonthIsStringNumeric_ShouldResolveExpectedMonth(string token, int expected)
    {
        FixedDateStrategy xml = Strategy(NotableDateResourceLoader.Load(GregorianFixedXml(token)));

        Assert.AreEqual(expected, xml.Month, "XML month");
        Assert.IsNull(xml.MonthAlias, "XML alias");
    }

    /// <summary>
    /// Verifies that every simple Hebrew month name (whose calendar position does not depend on the leap year) resolves
    /// to its canonical integer 1..6 with no alias under a Hebrew-calendar rule. Note that the v2 model treats
    /// <c>AdarII</c> as a leap-dependent alias rather than the fixed integer 7 used by v1.
    /// </summary>
    /// <param name="token">The Hebrew month name.</param>
    /// <param name="expected">The expected one-based month.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("Tishri", 1)]
    [DataRow("Heshvan", 2)]
    [DataRow("Kislev", 3)]
    [DataRow("Tevet", 4)]
    [DataRow("Shevat", 5)]
    [DataRow("AdarI", 6)]
    public void Load_WhenHebrewFixedMonthIsSimpleName_ShouldResolveNumericMonth(string token, int expected)
    {
        FixedDateStrategy xml = Strategy(NotableDateResourceLoader.Load(HebrewFixedXml(token)));
        FixedDateStrategy json = Strategy(NotableDateResourceLoader.LoadJson(
            $$"""
            { "schemaVersion": "1.0", "resourceId": "test.month",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "Religious",
                "rules": [ { "id": "r", "applicability": { "calendar": "Hebrew" }, "strategy": { "fixed": { "month": "{{token}}", "day": 1, "sweepCalendarYears": true } } } ] } ] }
            """));

        Assert.AreEqual(expected, xml.Month, "XML month");
        Assert.IsNull(xml.MonthAlias, "XML alias");
        Assert.AreEqual(expected, json.Month, "JSON month");
        Assert.IsNull(json.MonthAlias, "JSON alias");
    }

    /// <summary>
    /// Verifies that every leap-dependent Hebrew month token populates <see cref="FixedDateStrategy.MonthAlias" /> and
    /// leaves <see cref="FixedDateStrategy.Month" /> at <c>0</c>, so the resolver can pick the runtime-correct slot. The
    /// v2 model adds <c>AdarII</c> to this alias set (v1 mapped it to the fixed integer 7).
    /// </summary>
    /// <param name="token">The Hebrew month alias.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("AdarII")]
    [DataRow("LastAdar")]
    [DataRow("Nisan")]
    [DataRow("Iyar")]
    [DataRow("Sivan")]
    [DataRow("Tammuz")]
    [DataRow("Av")]
    [DataRow("Elul")]
    public void Load_WhenHebrewFixedMonthIsLeapDependentAlias_ShouldPopulateAlias(string token)
    {
        FixedDateStrategy xml = Strategy(NotableDateResourceLoader.Load(HebrewFixedXml(token, 14)));
        FixedDateStrategy json = Strategy(NotableDateResourceLoader.LoadJson(
            $$"""
            { "schemaVersion": "1.0", "resourceId": "test.month",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "Religious",
                "rules": [ { "id": "r", "applicability": { "calendar": "Hebrew" }, "strategy": { "fixed": { "month": "{{token}}", "day": 14, "sweepCalendarYears": true } } } ] } ] }
            """));

        Assert.AreEqual(0, xml.Month, "XML month");
        Assert.AreEqual(token, xml.MonthAlias, "XML alias");
        Assert.AreEqual(0, json.Month, "JSON month");
        Assert.AreEqual(token, json.MonthAlias, "JSON alias");
    }

    /// <summary>
    /// Verifies that a non-Gregorian <c>Fixed</c> rule accepts the maximum numeric month token <c>"13"</c> to support
    /// lunisolar calendars with an intercalary month, resolving it to the integer 13 with no alias.
    /// </summary>
    [TestMethod]
    public void Load_WhenNonGregorianFixedMonthIsThirteen_ShouldResolveNumericMonth()
    {
        const string xml =
            """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.month">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="Cultural">
                  <Rules><Rule id="r"><Applicability calendar="ChineseLunisolar" /><Strategy><Fixed month="13" day="1" sweepCalendarYears="true" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        FixedDateStrategy strategy = Strategy(NotableDateResourceLoader.Load(xml));

        Assert.AreEqual(13, strategy.Month, "month");
        Assert.IsNull(strategy.MonthAlias, "alias");
    }

    /// <summary>
    /// Verifies that month <c>13</c> on a Gregorian <c>Fixed</c> rule is rejected with a
    /// <see cref="NotableDateValidationException" /> carrying the <c>BODU-CAL2-MONTH</c> diagnostic, since 13 is only
    /// valid under a non-Gregorian calendar.
    /// </summary>
    [TestMethod]
    public void Load_WhenGregorianFixedMonthIsThirteen_ShouldThrowMonthDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(GregorianFixedXml("13"));
        });

        Assert.IsTrue(
            ex.Diagnostics.Any(d => d.Code == "BODU-CAL2-MONTH" && d.Severity == NotableDateValidationSeverity.Error),
            $"Expected BODU-CAL2-MONTH. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }

    /// <summary>
    /// Verifies that an out-of-range numeric month token on a Gregorian <c>Fixed</c> rule is rejected with a
    /// <see cref="NotableDateValidationException" /> carrying the <c>BODU-CAL2-MONTH</c> diagnostic.
    /// </summary>
    /// <param name="month">The out-of-range numeric month token.</param>
    [TestMethod]
    [DataRow("0")]
    [DataRow("14")]
    [DataRow("99")]
    public void Load_WhenGregorianFixedMonthIsOutOfRange_ShouldThrowMonthDiagnostic(string month)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(GregorianFixedXml(month));
        });

        Assert.IsTrue(
            ex.Diagnostics.Any(d => d.Code == "BODU-CAL2-MONTH" && d.Severity == NotableDateValidationSeverity.Error),
            $"Expected BODU-CAL2-MONTH. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }

    /// <summary>
    /// Verifies that an unrecognised month name on a Gregorian <c>Fixed</c> rule is rejected with the
    /// <c>BODU-CAL2-MONTH</c> diagnostic, in both XML and JSON.
    /// </summary>
    [TestMethod]
    public void Load_WhenGregorianFixedMonthIsUnknownName_ShouldThrowMonthDiagnostic()
    {
        NotableDateValidationException xmlEx = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(GregorianFixedXml("Smarch"));
        });

        NotableDateValidationException jsonEx = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(
                """
                { "schemaVersion": "1.0", "resourceId": "test.month",
                  "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                    "rules": [ { "id": "r", "strategy": { "fixed": { "month": "Smarch", "day": 1 } } } ] } ] }
                """);
        });

        Assert.IsTrue(xmlEx.Diagnostics.Any(d => d.Code == "BODU-CAL2-MONTH"), "XML BODU-CAL2-MONTH");
        Assert.IsTrue(jsonEx.Diagnostics.Any(d => d.Code == "BODU-CAL2-MONTH"), "JSON BODU-CAL2-MONTH");
    }

    /// <summary>
    /// Verifies that an unrecognised Hebrew month token on a Hebrew <c>Fixed</c> rule is rejected with the
    /// <c>BODU-CAL2-MONTH</c> diagnostic. In v1 an unknown alias deferred to a null occurrence at resolution time; the
    /// v2 parser reports it during load.
    /// </summary>
    [TestMethod]
    public void Load_WhenHebrewFixedMonthIsUnknownToken_ShouldThrowMonthDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(HebrewFixedXml("Smarchadar"));
        });

        Assert.IsTrue(
            ex.Diagnostics.Any(d => d.Code == "BODU-CAL2-MONTH" && d.Severity == NotableDateValidationSeverity.Error),
            $"Expected BODU-CAL2-MONTH. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }

    /// <summary>
    /// Verifies that a numeric month day token outside the schema's 1..31 range on a <c>Fixed</c> strategy is rejected
    /// before semantic validation with the <c>BODU-CAL2-SCHEMA</c> diagnostic (<c>day</c> is constrained by the XSD).
    /// </summary>
    /// <param name="day">The out-of-schema-range day token.</param>
    [TestMethod]
    [DataRow("0")]
    [DataRow("32")]
    public void Load_WhenFixedDayIsOutOfSchemaRange_ShouldThrowSchemaDiagnostic(string day)
    {
        string xml =
            $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.month">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy><Fixed month="January" day="{day}" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.IsTrue(
            ex.Diagnostics.Any(d => d.Code == "BODU-CAL2-SCHEMA" && d.Severity == NotableDateValidationSeverity.Error),
            $"Expected BODU-CAL2-SCHEMA. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }

    /// <summary>
    /// Verifies that a <c>DayOfWeekInMonth</c> strategy authored with a numeric month token resolves to the matching
    /// integer in the v2 model. The v1 schema restricted this field to Gregorian month names only; the v2 parser accepts
    /// numeric months here as it does on the <c>Fixed</c> strategy.
    /// </summary>
    [TestMethod]
    public void Load_WhenDayOfWeekInMonthMonthIsNumeric_ShouldResolveNumericMonth()
    {
        const string xml =
            """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.month">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="Observance">
                  <Rules><Rule id="r"><Strategy><DayOfWeekInMonth month="10" dayOfWeek="Monday" weekOrdinal="Second" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        DayOfWeekInMonthStrategy strategy = (DayOfWeekInMonthStrategy)NotableDateResourceLoader.Load(xml).NotableDates.Single().Rules.Single().Strategy;

        Assert.AreEqual(10, strategy.Month, "month");
    }
}
