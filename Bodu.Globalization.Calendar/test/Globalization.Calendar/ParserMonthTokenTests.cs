// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserMonthTokenTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

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
        (FixedDateStrategy)resource.NotableDates.Single().Rules.Single().Strategy!;

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

        Assert.AreEqual(
            (7, 4, (string?)null),
            (xml.Month, xml.Day, xml.MonthAlias),
            "XML month/day/alias");
        Assert.AreEqual(
            (7, (string?)null),
            (json.Month, json.MonthAlias),
            "JSON month/alias");
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

        Assert.AreEqual(
            (13, (string?)null),
            (strategy.Month, strategy.MonthAlias),
            "month/alias");
    }

    /// <summary>
    /// Verifies that month <c>13</c> on a Gregorian <c>Fixed</c> rule is rejected with a
    /// <see cref="NotableDateValidationException" /> carrying the <c>BODU-CAL-MONTH</c> diagnostic, since 13 is only
    /// valid under a non-Gregorian calendar.
    /// </summary>
    [TestMethod]
    public void Load_WhenGregorianFixedMonthIsThirteen_ShouldThrowMonthDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(GregorianFixedXml("13"));
        });

        Assert.Contains(
            d => d.Code == "BODU-CAL-MONTH" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics,
            $"Expected BODU-CAL-MONTH. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }

    /// <summary>
    /// Verifies that an out-of-range numeric month token on a Gregorian <c>Fixed</c> rule is rejected with a
    /// <see cref="NotableDateValidationException" /> carrying the <c>BODU-CAL-MONTH</c> diagnostic.
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

        Assert.Contains(
            d => d.Code == "BODU-CAL-MONTH" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics,
            $"Expected BODU-CAL-MONTH. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }

    /// <summary>
    /// Verifies that an unrecognized month name on a Gregorian <c>Fixed</c> rule is rejected with the
    /// <c>BODU-CAL-MONTH</c> diagnostic when loaded from XML.
    /// </summary>
    [TestMethod]
    public void Load_WhenGregorianFixedMonthIsUnknownName_ForXml_ShouldThrowMonthDiagnostic()
    {
        NotableDateValidationException xmlEx = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(GregorianFixedXml("Smarch"));
        });

        Assert.Contains(d => d.Code == "BODU-CAL-MONTH", xmlEx.Diagnostics, "XML BODU-CAL-MONTH");
    }

    /// <summary>
    /// Verifies that an unrecognized month name on a Gregorian <c>Fixed</c> rule is rejected with the
    /// <c>BODU-CAL-MONTH</c> diagnostic when loaded from JSON.
    /// </summary>
    [TestMethod]
    public void Load_WhenGregorianFixedMonthIsUnknownName_ForJson_ShouldThrowMonthDiagnostic()
    {
        NotableDateValidationException jsonEx = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(
                """
                { "schemaVersion": "1.0", "resourceId": "test.month",
                  "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                    "rules": [ { "id": "r", "strategy": { "fixed": { "month": "Smarch", "day": 1 } } } ] } ] }
                """);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-MONTH", jsonEx.Diagnostics, "JSON BODU-CAL-MONTH");
    }

    /// <summary>
    /// Verifies that an unrecognized Hebrew month token on a Hebrew <c>Fixed</c> rule is rejected with the
    /// <c>BODU-CAL-MONTH</c> diagnostic. In v1 an unknown alias deferred to a null occurrence at resolution time; the
    /// v2 parser reports it during load.
    /// </summary>
    [TestMethod]
    public void Load_WhenHebrewFixedMonthIsUnknownToken_ShouldThrowMonthDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(HebrewFixedXml("Smarchadar"));
        });

        Assert.Contains(
            d => d.Code == "BODU-CAL-MONTH" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics,
            $"Expected BODU-CAL-MONTH. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }

    /// <summary>
    /// Verifies that an English month name on a <c>Fixed</c> rule in a calendar other than Gregorian, loaded from XML,
    /// is reported with a <c>BODU-CAL-MONTH</c> message naming what that calendar accepts, the integers of its months
    /// and, for the Hebrew calendar alone, its month names, rather than the Gregorian forms the value already has.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="months">The number of months the calendar's year can have.</param>
    [TestMethod]
    [DataRow("Hebrew", 13)]
    [DataRow("Hijri", 12)]
    [DataRow("UmmAlQura", 12)]
    [DataRow("Persian", 12)]
    [DataRow("ChineseLunisolar", 13)]
    public void Load_WhenCalendarFixedMonthIsEnglishName_ForXml_ShouldNameTheMonthsTheCalendarAccepts(string calendar, int months)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(CalendarFixedXml(calendar, "July"));
        });

        AssertCalendarMonthMessage(ex, calendar, "July", months);
    }

    /// <summary>
    /// Verifies that an English month name on a <c>Fixed</c> rule in a calendar other than Gregorian, loaded from JSON,
    /// is reported with a <c>BODU-CAL-MONTH</c> message naming what that calendar accepts, the integers of its months
    /// and, for the Hebrew calendar alone, its month names, rather than the Gregorian forms the value already has.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="months">The number of months the calendar's year can have.</param>
    [TestMethod]
    [DataRow("Hebrew", 13)]
    [DataRow("Hijri", 12)]
    [DataRow("UmmAlQura", 12)]
    [DataRow("Persian", 12)]
    [DataRow("ChineseLunisolar", 13)]
    public void Load_WhenCalendarFixedMonthIsEnglishName_ForJson_ShouldNameTheMonthsTheCalendarAccepts(string calendar, int months)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(CalendarFixedJson(calendar, "\"July\""));
        });

        AssertCalendarMonthMessage(ex, calendar, "July", months);
    }

    /// <summary>
    /// Verifies that a Hebrew month name on a <c>Fixed</c> rule in a calendar other than the Hebrew, loaded from XML, is
    /// reported with <c>BODU-CAL-MONTH</c> rather than read as the position the name has in the Hebrew year.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="months">The number of months the calendar's year can have.</param>
    [TestMethod]
    [DataRow("Hijri", 12)]
    [DataRow("UmmAlQura", 12)]
    [DataRow("Persian", 12)]
    [DataRow("ChineseLunisolar", 13)]
    public void Load_WhenFixedMonthIsHebrewNameInAnotherCalendar_ForXml_ShouldThrowMonthDiagnostic(string calendar, int months)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(CalendarFixedXml(calendar, "Nisan"));
        });

        AssertCalendarMonthMessage(ex, calendar, "Nisan", months);
    }

    /// <summary>
    /// Verifies that a Hebrew month name on a <c>Fixed</c> rule in a calendar other than the Hebrew, loaded from JSON,
    /// is reported with <c>BODU-CAL-MONTH</c> rather than read as the position the name has in the Hebrew year.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="months">The number of months the calendar's year can have.</param>
    [TestMethod]
    [DataRow("Hijri", 12)]
    [DataRow("UmmAlQura", 12)]
    [DataRow("Persian", 12)]
    [DataRow("ChineseLunisolar", 13)]
    public void Load_WhenFixedMonthIsHebrewNameInAnotherCalendar_ForJson_ShouldThrowMonthDiagnostic(string calendar, int months)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(CalendarFixedJson(calendar, "\"Nisan\""));
        });

        AssertCalendarMonthMessage(ex, calendar, "Nisan", months);
    }

    /// <summary>
    /// Verifies that a numeric month on a <c>Fixed</c> rule beyond the months its calendar's year can have, loaded from
    /// XML, is reported with <c>BODU-CAL-MONTH</c> rather than loaded as a month that never occurs: 13 in the twelve
    /// month calendars, and in the Chinese lunisolar calendar when the month skips the leap month, and 14 in the
    /// thirteen month calendars.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="month">The month.</param>
    /// <param name="skipLeapMonth">A value indicating whether the month skips a Chinese leap month.</param>
    /// <param name="months">The number of months the rule's month can name.</param>
    [TestMethod]
    [DataRow("Hijri", "13", false, 12)]
    [DataRow("UmmAlQura", "13", false, 12)]
    [DataRow("Persian", "13", false, 12)]
    [DataRow("ChineseLunisolar", "13", true, 12)]
    [DataRow("ChineseLunisolar", "14", false, 13)]
    [DataRow("Hebrew", "14", false, 13)]
    public void Load_WhenFixedMonthExceedsCalendarMonths_ForXml_ShouldThrowMonthDiagnostic(string calendar, string month, bool skipLeapMonth, int months)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(CalendarFixedXml(calendar, month, skipLeapMonth));
        });

        AssertCalendarMonthMessage(ex, calendar, month, months);
    }

    /// <summary>
    /// Verifies that a numeric month on a <c>Fixed</c> rule beyond the months its calendar's year can have, loaded from
    /// JSON, is reported with <c>BODU-CAL-MONTH</c> rather than loaded as a month that never occurs, whether the
    /// <c>skipLeapMonth</c> flag precedes or follows the month.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="month">The month, as a JSON literal.</param>
    /// <param name="skipLeapMonth">A value indicating whether the month skips a Chinese leap month.</param>
    /// <param name="months">The number of months the rule's month can name.</param>
    [TestMethod]
    [DataRow("Hijri", "13", false, 12)]
    [DataRow("UmmAlQura", "13", false, 12)]
    [DataRow("Persian", "13", false, 12)]
    [DataRow("ChineseLunisolar", "13", true, 12)]
    [DataRow("ChineseLunisolar", "14", false, 13)]
    [DataRow("Hebrew", "14", false, 13)]
    public void Load_WhenFixedMonthExceedsCalendarMonths_ForJson_ShouldThrowMonthDiagnostic(string calendar, string month, bool skipLeapMonth, int months)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(CalendarFixedJson(calendar, month, skipLeapMonth));
        });

        AssertCalendarMonthMessage(ex, calendar, month, months);
    }

    /// <summary>
    /// Verifies that the last month a <c>Fixed</c> rule's calendar can name loads as that month: 12 in the twelve month
    /// calendars and in the Chinese lunisolar calendar when the month skips the leap month, and 13 in the Hebrew and
    /// the Chinese lunisolar calendars otherwise.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="month">The month.</param>
    /// <param name="skipLeapMonth">A value indicating whether the month skips a Chinese leap month.</param>
    [TestMethod]
    [DataRow("Hijri", 12, false)]
    [DataRow("UmmAlQura", 12, false)]
    [DataRow("Persian", 12, false)]
    [DataRow("ChineseLunisolar", 12, true)]
    [DataRow("ChineseLunisolar", 13, false)]
    [DataRow("Hebrew", 13, false)]
    public void Load_WhenFixedMonthIsCalendarsLastMonth_ShouldResolveNumericMonth(string calendar, int month, bool skipLeapMonth)
    {
        FixedDateStrategy strategy = Strategy(NotableDateResourceLoader.Load(CalendarFixedXml(calendar, month.ToString(CultureInfo.InvariantCulture), skipLeapMonth)));

        Assert.AreEqual((month, (string?)null), (strategy.Month, strategy.MonthAlias));
    }

    /// <summary>
    /// Verifies that a Hebrew month name on a Hebrew <c>Fixed</c> rule, loaded from XML in another case, is matched
    /// ignoring case, as a Gregorian month name and an enumeration value are, and read as the month it names: a
    /// fixed-position name as its number, and a leap-dependent name as the alias in its canonical spelling.
    /// </summary>
    /// <param name="token">The month name as written.</param>
    /// <param name="month">The month the name reads as, or <c>0</c> when an alias supplies it.</param>
    /// <param name="alias">The canonical alias the name reads as, or <see langword="null" />.</param>
    [TestMethod]
    [DataRow("tishri", 1, null)]
    [DataRow("TISHRI", 1, null)]
    [DataRow("adari", 6, null)]
    [DataRow("nisan", 0, "Nisan")]
    [DataRow("NISAN", 0, "Nisan")]
    [DataRow("lastadar", 0, "LastAdar")]
    [DataRow("ElUl", 0, "Elul")]
    public void Load_WhenHebrewFixedMonthNameDiffersInCase_ForXml_ShouldReadTheNamedMonth(string token, int month, string? alias)
    {
        FixedDateStrategy strategy = Strategy(NotableDateResourceLoader.Load(CalendarFixedXml("Hebrew", token)));

        Assert.AreEqual((month, alias), (strategy.Month, strategy.MonthAlias));
    }

    /// <summary>
    /// Verifies that a Hebrew month name on a Hebrew <c>Fixed</c> rule, loaded from JSON in another case, is matched
    /// ignoring case and read as the month it names, a leap-dependent name as the alias in its canonical spelling.
    /// </summary>
    /// <param name="token">The month name as written.</param>
    /// <param name="month">The month the name reads as, or <c>0</c> when an alias supplies it.</param>
    /// <param name="alias">The canonical alias the name reads as, or <see langword="null" />.</param>
    [TestMethod]
    [DataRow("tishri", 1, null)]
    [DataRow("SHEVAT", 5, null)]
    [DataRow("nisan", 0, "Nisan")]
    [DataRow("adarii", 0, "AdarII")]
    [DataRow("TAMMUZ", 0, "Tammuz")]
    public void Load_WhenHebrewFixedMonthNameDiffersInCase_ForJson_ShouldReadTheNamedMonth(string token, int month, string? alias)
    {
        FixedDateStrategy strategy = Strategy(NotableDateResourceLoader.LoadJson(CalendarFixedJson("Hebrew", "\"" + token + "\"")));

        Assert.AreEqual((month, alias), (strategy.Month, strategy.MonthAlias));
    }

    /// <summary>
    /// Builds a one-concept XML document with a <c>Fixed</c> rule in the supplied calendar whose month token is the
    /// supplied value.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="month">The month token to author.</param>
    /// <param name="skipLeapMonth">A value indicating whether the month skips a Chinese leap month.</param>
    /// <returns>The XML document content.</returns>
    private static string CalendarFixedXml(string calendar, string month, bool skipLeapMonth = false) =>
        $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.month">
          <NotableDates>
            <NotableDate id="x" displayName="X" category="Religious">
              <Rules><Rule id="r"><Applicability calendar="{calendar}" /><Strategy><Fixed month="{month}" day="15" skipLeapMonth="{(skipLeapMonth ? "true" : "false")}" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a one-concept JSON document with a <c>Fixed</c> rule in the supplied calendar whose month is the supplied
    /// JSON literal, its <c>skipLeapMonth</c> flag written before the month and its day after it.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="month">The month, as a JSON literal.</param>
    /// <param name="skipLeapMonth">A value indicating whether the month skips a Chinese leap month.</param>
    /// <returns>The JSON document content.</returns>
    private static string CalendarFixedJson(string calendar, string month, bool skipLeapMonth = false) =>
        $$"""
        { "schemaVersion": "1.0", "resourceId": "test.month",
          "notableDates": [ { "id": "x", "displayName": "X", "category": "Religious",
            "rules": [ { "id": "r", "applicability": { "calendar": "{{calendar}}" },
              "strategy": { "fixed": { "skipLeapMonth": {{(skipLeapMonth ? "true" : "false")}}, "month": {{month}}, "day": 15 } } } ] } ] }
        """;

    /// <summary>
    /// Asserts that a load failed with the one <c>BODU-CAL-MONTH</c> error a non-Gregorian rule's month draws when its
    /// calendar does not take it, worded for that calendar: the integers of its months and, for the Hebrew calendar
    /// alone, its month names.
    /// </summary>
    /// <param name="ex">The exception the load threw.</param>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="month">The month the rule names.</param>
    /// <param name="months">The number of months the rule's month can name.</param>
    private static void AssertCalendarMonthMessage(NotableDateValidationException ex, string calendar, string month, int months)
    {
        string expected = calendar == "Hebrew"
            ? $"Notable date 'x', rule 'r': '{month}' is not a valid month value in the Hebrew calendar. Expected an integer " +
              "between 1 and 13 or a Hebrew month name: Tishri, Heshvan, Kislev, Tevet, Shevat, AdarI, AdarII, LastAdar, " +
              "Nisan, Iyar, Sivan, Tammuz, Av or Elul."
            : $"Notable date 'x', rule 'r': '{month}' is not a valid month value in the {calendar} calendar. Expected an " +
              $"integer between 1 and {months}.";

        string[] messages = [.. ex.Diagnostics.Where(d => d.Code == "BODU-CAL-MONTH").Select(d => d.Message)];

        Assert.HasCount(1, messages, string.Join(" | ", ex.Diagnostics.Select(d => d.Code + ": " + d.Message)));
        Assert.AreEqual(expected, messages[0]);
    }

    /// <summary>
    /// Verifies that a numeric month day token outside the schema's 1..31 range on a <c>Fixed</c> strategy is rejected
    /// before semantic validation with the <c>BODU-CAL-SCHEMA</c> diagnostic (<c>day</c> is constrained by the XSD).
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

        Assert.Contains(
            d => d.Code == "BODU-CAL-SCHEMA" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics,
            $"Expected BODU-CAL-SCHEMA. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
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

        var strategy = (DayOfWeekInMonthStrategy)NotableDateResourceLoader.Load(xml).NotableDates.Single().Rules.Single().Strategy!;

        Assert.AreEqual(10, strategy.Month, "month");
    }
}
