// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LeapYearScenarioTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Ports the v1 leap-year scenarios to the v2 <see cref="NotableDateService.Resolve(DateRange, string)" /> surface:
/// 29 February fixed anchors that materialise only in leap years, offset rules projecting from a leap-day anchor,
/// last-Monday-of-February resolution across the leap cycle, multi-day spans straddling 29 February, the
/// <see cref="AdjustmentTrigger.IfLeapYear" /> trigger, the <see cref="AdjustmentTrigger.IfBeforeFixedDate" />
/// comparison-date clamp at 29 February, and weekend roll-forward on a Saturday 29 February.
/// </summary>
/// <remarks>
/// <para>
/// In v2 a Gregorian fixed-date strategy returns no occurrence when the day exceeds the month's length, so a 29
/// February rule naturally yields nothing in a non-leap year. Day-of-week pairings in the assertions are real
/// Gregorian dates verifiable with any almanac.
/// </para>
/// </remarks>
[TestClass]
public sealed class LeapYearScenarioTests
{
    /// <summary>The territory used by the global-rule documents, which apply to every territory.</summary>
    private const string Territory = "AU";

    // =====================================================================================================================
    // Fixed 29 February across the leap cycle
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a fixed-date rule on 29 February emits only in leap years and yields nothing in non-leap years,
    /// including the non-leap century 2100 and the divisible-by-400 leap century 2400.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="isLeapYear">Whether the year is a Gregorian leap year.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2020, true)]   // Divisible by 4 -> leap
    [DataRow(2021, false)]
    [DataRow(2022, false)]
    [DataRow(2023, false)]
    [DataRow(2024, true)]   // Leap
    [DataRow(2025, false)]
    [DataRow(2026, false)]
    [DataRow(2027, false)]
    [DataRow(2028, true)]   // Leap
    [DataRow(2100, false)]  // Century not divisible by 400 -> non-leap
    [DataRow(2400, true)]   // Century divisible by 400 -> leap
    public void Resolve_WhenFixedDateIsLeapDay_ShouldEmitOnlyInLeapYears(int year, bool isLeapYear)
    {
        IReadOnlyList<NotableDate> results = LeapDayFixedService()
            .Resolve(new DateRange(new DateOnly(year, 2, 1), new DateOnly(year, 3, 31)), Territory);

        if (isLeapYear)
        {
            NotableDate match = Single(results, "leap-day");
            Assert.AreEqual(new DateOnly(year, 2, 29), match.Date);
        }
        else
        {
            Assert.DoesNotContain(n => n.NotableDateId == "leap-day", results, $"Leap Day should not emit in non-leap year {year}.");
        }
    }

    // =====================================================================================================================
    // Offset rule anchored on the leap day
    // =====================================================================================================================

    /// <summary>
    /// Verifies that an offset rule whose anchor resolves only in leap years (day after 29 February) emits in leap years
    /// and yields nothing in non-leap years because the anchor itself does not resolve.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="isLeapYear">Whether the year is a Gregorian leap year.</param>
    [TestMethod]
    [DataRow(2020, true)]
    [DataRow(2023, false)]
    [DataRow(2024, true)]
    [DataRow(2025, false)]
    public void Resolve_WhenOffsetRuleAnchoredOnLeapDay_ShouldEmitOnlyInLeapYears(int year, bool isLeapYear)
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.leap-offset">
          <NotableDates>
            <NotableDate id="leap-day" displayName="Leap Day" category="Observance" defaultNonWorkingDay="false">
              <Rules><Rule id="feb-29"><Applicability calendar="Gregorian" /><Strategy><Fixed month="February" day="29" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="day-after-leap-day" displayName="Day After Leap Day" category="Observance" defaultNonWorkingDay="false">
              <Rules><Rule id="feb-29-plus-1"><Applicability calendar="Gregorian" />
                <Strategy><OffsetFromRule notableDateRef="leap-day" ruleRef="feb-29" offsetDays="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(year, 2, 1), new DateOnly(year, 3, 31)), Territory);

        if (isLeapYear)
        {
            Assert.AreEqual(new DateOnly(year, 3, 1), Single(results, "day-after-leap-day").Date);
        }
        else
        {
            Assert.DoesNotContain(n => n.NotableDateId == "day-after-leap-day", results);
        }
    }

    // =====================================================================================================================
    // DayOfWeekInMonth in February
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a last-Monday-of-February rule resolves to the actual last Monday across leap and non-leap years.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedDay">The expected day of the last Monday in February.</param>
    [TestMethod]
    [DataRow(2024, 26)] // Leap. Feb 29 = Thu; Mondays: 5, 12, 19, 26.
    [DataRow(2025, 24)] // Non-leap. Feb 28 = Fri; Mondays: 3, 10, 17, 24.
    [DataRow(2026, 23)] // Non-leap. Feb 28 = Sat; Mondays: 2, 9, 16, 23.
    [DataRow(2028, 28)] // Leap. Feb 28 = Mon, Feb 29 = Tue -> last Mon = 28.
    public void Resolve_WhenLastMondayOfFebruary_ShouldResolveCorrectlyAcrossLeapAndNonLeap(int year, int expectedDay)
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.last-mon-feb">
          <NotableDates>
            <NotableDate id="last-monday-of-february" displayName="Last Monday of February" category="Observance" defaultNonWorkingDay="false">
              <Rules><Rule id="last-mon"><Applicability calendar="Gregorian" />
                <Strategy><DayOfWeekInMonth month="February" dayOfWeek="Monday" weekOrdinal="Last" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(year, 2, 1), new DateOnly(year, 2, 28)), Territory);

        Assert.AreEqual(new DateOnly(year, 2, expectedDay), Single(results, "last-monday-of-february").Date);
    }

    // =====================================================================================================================
    // Multi-day spans crossing the leap day
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a four-day span anchored on 27 February includes 29 February inside a leap year and skips it in a
    /// non-leap year. The end date is computed inclusively from <c>Date + (DurationDays - 1)</c>.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedEndMonth">The expected inclusive end month.</param>
    /// <param name="expectedEndDay">The expected inclusive end day.</param>
    [TestMethod]
    [DataRow(2024, 3, 1)] // Leap. Span Feb 27, 28, 29, Mar 1.
    [DataRow(2025, 3, 2)] // Non-leap. Span Feb 27, 28, Mar 1, Mar 2.
    public void Resolve_WhenMultiDaySpanFromFeb27_ShouldStraddleLeapDayCorrectly(int year, int expectedEndMonth, int expectedEndDay)
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.feb-span">
          <NotableDates>
            <NotableDate id="feb-end-festival" displayName="Feb-End Festival" category="Cultural" defaultNonWorkingDay="false">
              <Rules><Rule id="feb-27" durationDays="4"><Applicability calendar="Gregorian" />
                <Strategy><Fixed month="February" day="27" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(year, 2, 1), new DateOnly(year, 3, 31)), Territory);

        NotableDate festival = Single(results, "feb-end-festival");
        Assert.AreEqual(new DateOnly(year, 2, 27), festival.Date);
        Assert.AreEqual(4, festival.DurationDays);
        Assert.AreEqual(new DateOnly(year, expectedEndMonth, expectedEndDay), festival.EndDate);
    }

    // =====================================================================================================================
    // IfLeapYear adjustment fires only in leap years
    // =====================================================================================================================

    /// <summary>
    /// Verifies that an <see cref="AdjustmentTrigger.IfLeapYear" /> + <see cref="AdjustmentAction.AddDays" />(+1) policy
    /// shifts the occurrence only in leap years and emits the unshifted anchor in non-leap years.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedFire">Whether the leap-year trigger fires.</param>
    [TestMethod]
    [DataRow(2024, true)]   // Leap
    [DataRow(2025, false)]
    [DataRow(2026, false)]
    [DataRow(2027, false)]
    [DataRow(2028, true)]   // Leap
    public void Resolve_WhenAdjustmentIsIfLeapYear_ShouldFireOnlyInLeapYears(int year, bool expectedFire)
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.if-leap">
          <AdjustmentPolicies>
            <AdjustmentPolicy id="leap-year-shift" priority="100">
              <Trigger type="IfLeapYear" />
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Leap-year shift" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="leap-aware-holiday" displayName="Leap-Aware Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="jul-1"><Applicability calendar="Gregorian" /><Strategy><Fixed month="July" day="1" /></Strategy>
                <Adjustments><Adjustment policyRef="leap-year-shift" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(year, 6, 1), new DateOnly(year, 7, 31)), Territory);

        NotableDate match = Single(results, "leap-aware-holiday");

        if (expectedFire)
        {
            Assert.AreEqual(new DateOnly(year, 7, 2), match.Date);
            Assert.IsTrue(match.IsObserved);
            Assert.AreEqual(new DateOnly(year, 7, 1), match.ActualDate);
        }
        else
        {
            Assert.AreEqual(new DateOnly(year, 7, 1), match.Date);
            Assert.IsFalse(match.IsObserved);
        }
    }

    // =====================================================================================================================
    // IfBeforeFixedDate comparison date = 29 February clamps to 28 February in non-leap years
    // =====================================================================================================================

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> with a 29 February comparison date clamps to 28
    /// February in non-leap years, firing strictly before the (clamped) pivot.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="month">The probe rule's fixed month.</param>
    /// <param name="day">The probe rule's fixed day.</param>
    /// <param name="expectedFire">Whether the trigger fires for the anchor.</param>
    [TestMethod]
    [DataRow(2025, 2, 27, true)]    // Non-leap. Pivot clamps to 28 Feb 2025; 27 < 28 -> fires.
    [DataRow(2025, 2, 28, false)]   // Non-leap. 28 < 28 is false -> no fire.
    [DataRow(2024, 2, 28, true)]    // Leap. Pivot = 29 Feb 2024; 28 < 29 -> fires.
    [DataRow(2024, 2, 29, false)]   // Leap. 29 < 29 is false -> no fire.
    public void Resolve_WhenComparisonDateIsFeb29_ShouldClampToFeb28InNonLeapYear(int year, int month, int day, bool expectedFire)
    {
        var monthName = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);

        var xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.before-feb-29">
          <AdjustmentPolicies>
            <AdjustmentPolicy id="before-feb-29" priority="100">
              <Trigger type="IfBeforeFixedDate" month="February" day="29" />
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Before 29 February" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="anchor"><Applicability calendar="Gregorian" fromYear="{year}" toYear="{year}" />
                <Strategy><Fixed month="{monthName}" day="{day}" /></Strategy>
                <Adjustments><Adjustment policyRef="before-feb-29" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(year, 2, 1), new DateOnly(year, 3, 31)), Territory);

        NotableDate match = Single(results, "probe");

        Assert.AreEqual(expectedFire, match.IsObserved);
        DateOnly expected = expectedFire
            ? new DateOnly(year, month, day).AddDays(1)
            : new DateOnly(year, month, day);
        Assert.AreEqual(expected, match.Date);
    }

    // =====================================================================================================================
    // Algorithmic Easter across the leap cycle
    // =====================================================================================================================

    /// <summary>
    /// Verifies that algorithmic Western Easter Sunday resolves to its known date in both leap and non-leap years.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedMonth">The expected Easter month.</param>
    /// <param name="expectedDay">The expected Easter day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, 4, 9)]   // Non-leap
    [DataRow(2024, 3, 31)]  // Leap
    [DataRow(2025, 4, 20)]
    [DataRow(2026, 4, 5)]
    [DataRow(2027, 3, 28)]
    [DataRow(2028, 4, 16)]  // Leap
    public void Resolve_WhenAlgorithmicEaster_ShouldComputeKnownDatesAcrossLeapAndNonLeap(int year, int expectedMonth, int expectedDay)
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.leap-easter">
          <NotableDates>
            <NotableDate id="easter-sunday" displayName="Easter Sunday" category="Religious" defaultNonWorkingDay="false">
              <Rules><Rule id="western"><Applicability calendar="Gregorian" /><Strategy><Algorithm key="western-easter" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), Territory);

        Assert.AreEqual(new DateOnly(year, expectedMonth, expectedDay), Single(results, "easter-sunday").Date);
    }

    // =====================================================================================================================
    // Weekend roll-forward on a Saturday 29 February
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a 29 February holiday whose policy rolls weekend anchors forward emits the Monday substitute when 29
    /// February falls on a Saturday. 29 Feb 2020 = Saturday; the substitute lands on Monday 2 March 2020.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenLeapDaySaturdayRollsForward_ShouldEmitMondaySubstitute()
    {
        IReadOnlyList<NotableDate> results = LeapDayWeekendRollService()
            .Resolve(new DateRange(new DateOnly(2020, 2, 1), new DateOnly(2020, 3, 31)), Territory);

        NotableDate substitute = Single(results, "leap-day-holiday");
        Assert.AreEqual(new DateOnly(2020, 3, 2), substitute.Date);
        Assert.IsTrue(substitute.IsObserved);
        Assert.AreEqual(new DateOnly(2020, 2, 29), substitute.ActualDate);
    }

    /// <summary>
    /// Verifies that the same 29 February holiday emits its unshifted anchor when 29 February falls on a weekday. 29 Feb
    /// 2024 = Thursday; no roll fires.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenLeapDayThursdayDoesNotRoll_ShouldEmitBaseAnchor()
    {
        IReadOnlyList<NotableDate> results = LeapDayWeekendRollService()
            .Resolve(new DateRange(new DateOnly(2024, 2, 1), new DateOnly(2024, 3, 31)), Territory);

        NotableDate match = Single(results, "leap-day-holiday");
        Assert.AreEqual(new DateOnly(2024, 2, 29), match.Date);
        Assert.IsFalse(match.IsObserved);
    }

    // =====================================================================================================================
    // Helpers
    // =====================================================================================================================

    /// <summary>
    /// Builds a resolver over an inline document holding a single global 29 February fixed-date rule with no adjustment.
    /// </summary>
    /// <returns>A resolver for the inline document.</returns>
    private static NotableDateService LeapDayFixedService()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.leap-day">
          <NotableDates>
            <NotableDate id="leap-day" displayName="Leap Day" category="Observance" defaultNonWorkingDay="false">
              <Rules><Rule id="feb-29"><Applicability calendar="Gregorian" /><Strategy><Fixed month="February" day="29" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Builds a resolver over an inline document holding a 29 February holiday whose weekend-substitute policy moves the
    /// occurrence to the next working day.
    /// </summary>
    /// <returns>A resolver for the inline document.</returns>
    private static NotableDateService LeapDayWeekendRollService()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.leap-weekend">
          <AdjustmentPolicies>
            <AdjustmentPolicy id="weekend-substitute" priority="100">
              <Trigger type="IfWeekend" />
              <Action type="MoveToNextWorkingDay" skipWeekends="true" maxSearchDays="7" />
              <Emission mode="ObservedOnly" reason="Substitute public holiday" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="leap-day-holiday" displayName="Leap Day Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="feb-29"><Applicability calendar="Gregorian" /><Strategy><Fixed month="February" day="29" /></Strategy>
                <Adjustments><Adjustment policyRef="weekend-substitute" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Returns the single resolved occurrence with the supplied notable-date id, asserting exactly one match.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(IReadOnlyList<NotableDate> results, string notableDateId)
    {
        var matches = results.Where(r => r.NotableDateId == notableDateId).ToList();
        Assert.HasCount(1, matches, $"expected exactly one '{notableDateId}'");
        return matches[0];
    }
}
