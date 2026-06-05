// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Exhaustive truth tables for every <see cref="AdjustmentTrigger" /> firing condition, resolved end to end through the
/// <see cref="NotableDateService" />. Each policy pairs the trigger under test with an observed-only
/// <see cref="AdjustmentAction.AddDays" /> shift so that "the policy fired" is observable as an emitted occurrence whose
/// <see cref="NotableDate.AdjustmentPolicyId" /> is set and whose <see cref="NotableDate.IsObserved" /> flag is
/// <see langword="true" />, while "the policy did not fire" surfaces as the unchanged actual occurrence with a
/// <see langword="null" /> policy id. Ported from the v1 <c>NotableDateAdjusterTests</c> trigger truth tables and the
/// <c>NotableDateRangePipelineScenarioTests.AdjustmentMatrix</c> trigger-activation matrix.
/// </summary>
/// <remarks>
/// Anchor weekdays referenced in the rows: 1 Jan 2022 is a Saturday, 1 Jan 2023 a Sunday, 1 Jan 2026 a Thursday;
/// dates in June/July/December 2026 are annotated inline.
/// </remarks>
[TestClass]
public sealed class AdjustmentTriggerMatrixTests
{
    private const string Territory = "XX";

    /// <summary>
    /// A resource exposing one holiday per non-parameterized trigger, each firing an observed-only one-day shift so a
    /// fired trigger is visible as a populated <see cref="NotableDate.AdjustmentPolicyId" />. Every holiday uses a fixed
    /// 1 January strategy so the anchor weekday is controlled purely by the resolved year.
    /// </summary>
    private const string TriggerXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.trigger-matrix">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="always" priority="100">
          <Trigger type="Always" />
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="Always" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-weekend" priority="100">
          <Trigger type="IfWeekend" />
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If weekend" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-weekday" priority="100">
          <Trigger type="IfWeekday" />
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If weekday" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-leap-year" priority="100">
          <Trigger type="IfLeapYear" />
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If leap year" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-sunday" priority="100">
          <Trigger type="IfDayOfWeek"><Weekday value="Sunday" /></Trigger>
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If Sunday" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-sat-or-sun" priority="100">
          <Trigger type="IfDayOfWeek">
            <Weekday value="Saturday" />
            <Weekday value="Sunday" />
          </Trigger>
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If Saturday or Sunday" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="always-h" displayName="Always" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="always" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="weekend-h" displayName="Weekend" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-weekend" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="weekday-h" displayName="Weekday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-weekday" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="leap-h" displayName="Leap" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-leap-year" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="sunday-h" displayName="Sunday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-sunday" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="sat-sun-h" displayName="Sat or Sun" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-sat-or-sun" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Gets a shared service over the trigger-matrix fixture.
    /// </summary>
    private static INotableDateService TriggerService =>
        new NotableDateService(NotableDateResourceLoader.Load(TriggerXml));

    /// <summary>
    /// Resolves the single occurrence with the supplied id for the year of the anchor date and asserts whether the
    /// trigger fired. A fired trigger is identified by a populated <see cref="NotableDate.AdjustmentPolicyId" />.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="id">The notable-date id whose single occurrence is inspected.</param>
    /// <param name="anchorYear">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedPolicyId">The policy id expected when the trigger fires, or <see langword="null" />.</param>
    private static void AssertActivation(INotableDateService service, string id, int anchorYear, string? expectedPolicyId)
    {
        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(anchorYear, 1, 1), new DateOnly(anchorYear, 1, 7)), Territory)
            .Single(r => r.NotableDateId == id);

        if (expectedPolicyId is null)
        {
            Assert.IsFalse(match.IsObserved, $"Expected {id} to be unadjusted for {anchorYear}.");
            Assert.IsNull(match.AdjustmentPolicyId);
            Assert.AreEqual(new DateOnly(anchorYear, 1, 1), match.Date);
        }
        else
        {
            Assert.IsTrue(match.IsObserved, $"Expected {id} to be adjusted for {anchorYear}.");
            Assert.AreEqual(expectedPolicyId, match.AdjustmentPolicyId);
            Assert.AreEqual(new DateOnly(anchorYear, 1, 1), match.ActualDate);
            Assert.AreEqual(new DateOnly(anchorYear, 1, 2), match.Date);
        }
    }

    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.Always" /> trigger fires regardless of the anchor weekday or year.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2022)] // Saturday
    [DataRow(2023)] // Sunday
    [DataRow(2024)] // Monday
    [DataRow(2026)] // Thursday
    public void Always_WhenAnyAnchor_ShouldAlwaysFire(int year) =>
        AssertActivation(TriggerService, "always-h", year, "always");

    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfWeekend" /> trigger fires only when the anchor falls on a
    /// Saturday or Sunday.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2021, false)] // Friday
    [DataRow(2022, true)]  // Saturday
    [DataRow(2023, true)]  // Sunday
    [DataRow(2024, false)] // Monday
    [DataRow(2025, false)] // Wednesday
    [DataRow(2026, false)] // Thursday
    public void IfWeekend_WhenAnchorWeekday_ShouldFireOnlyOnSaturdayOrSunday(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "weekend-h", year, expectedFire ? "if-weekend" : null);

    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfWeekday" /> trigger fires only on Monday through Friday.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2021, true)]  // Friday
    [DataRow(2022, false)] // Saturday
    [DataRow(2023, false)] // Sunday
    [DataRow(2024, true)]  // Monday
    [DataRow(2025, true)]  // Wednesday
    [DataRow(2026, true)]  // Thursday
    public void IfWeekday_WhenAnchorWeekday_ShouldFireOnlyOnMondayThroughFriday(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "weekday-h", year, expectedFire ? "if-weekday" : null);

    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfLeapYear" /> trigger fires only in Gregorian leap years,
    /// including the century rule (divisible by 100 is common unless also divisible by 400).
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, true)]  // Leap
    [DataRow(2025, false)] // Common
    [DataRow(2026, false)] // Common
    [DataRow(2027, false)] // Common
    [DataRow(2028, true)]  // Leap
    [DataRow(2000, true)]  // Divisible by 400 → leap
    [DataRow(2100, false)] // Divisible by 100 but not 400 → common
    [DataRow(2400, true)]  // Divisible by 400 → leap
    public void IfLeapYear_WhenAnchorYear_ShouldFireOnlyInLeapYears(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "leap-h", year, expectedFire ? "if-leap-year" : null);

    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfDayOfWeek" /> trigger configured with a single Sunday weekday
    /// fires only when the anchor is a Sunday.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2021, false)] // Friday
    [DataRow(2022, false)] // Saturday
    [DataRow(2023, true)]  // Sunday
    [DataRow(2024, false)] // Monday
    [DataRow(2026, false)] // Thursday
    public void IfDayOfWeek_WhenConfiguredSunday_ShouldFireOnlyOnSundays(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "sunday-h", year, expectedFire ? "if-sunday" : null);

    /// <summary>
    /// Verifies that the <see cref="AdjustmentTrigger.IfDayOfWeek" /> trigger configured with both weekend weekdays
    /// fires on either Saturday or Sunday, matching the canonical mondayisation weekday set.
    /// </summary>
    /// <param name="year">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2021, false)] // Friday
    [DataRow(2022, true)]  // Saturday
    [DataRow(2023, true)]  // Sunday
    [DataRow(2024, false)] // Monday
    public void IfDayOfWeek_WhenConfiguredSaturdayAndSunday_ShouldFireOnEitherWeekendDay(int year, bool expectedFire) =>
        AssertActivation(TriggerService, "sat-sun-h", year, expectedFire ? "if-sat-or-sun" : null);

    // -----------------------------------------------------------------------------------------------------------------
    // Fixed-date triggers — comparison month/day projected onto the occurrence year. A per-row fixture lets the
    // occurrence date vary so both the strictly-before and strictly-after boundaries are exercised.
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a single-holiday service whose fixed-date holiday (bound to 2026) fires the supplied fixed-date trigger
    /// against the supplied comparison month and day, shifting one day forward on activation.
    /// </summary>
    /// <param name="triggerType">The fixed-date trigger type (<c>IfBeforeFixedDate</c> or <c>IfAfterFixedDate</c>).</param>
    /// <param name="month">The English comparison month name.</param>
    /// <param name="day">The comparison day of month.</param>
    /// <param name="strategyMonth">The English month of the holiday's fixed strategy.</param>
    /// <param name="strategyDay">The day of the holiday's fixed strategy.</param>
    /// <returns>A service over the generated fixture.</returns>
    private static INotableDateService FixedDateService(string triggerType, string month, int day, string strategyMonth, int strategyDay)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.fixed-trigger">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="fixed" priority="100">
              <Trigger type="{triggerType}" month="{month}" day="{day}" />
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Fixed-date trigger" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability fromYear="2026" toYear="2026" /><Strategy><Fixed month="{strategyMonth}" day="{strategyDay}" /></Strategy><Adjustments><Adjustment policyRef="fixed" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> fires only when the occurrence falls strictly
    /// before the comparison month and day projected onto the occurrence year. The comparison anchor is 4 April.
    /// </summary>
    /// <param name="strategyMonth">The English month of the holiday's fixed strategy.</param>
    /// <param name="strategyDay">The day of the holiday's fixed strategy.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("January", 1, true)]    // 1 Jan < 4 Apr → fire
    [DataRow("April", 3, true)]      // 3 Apr < 4 Apr → fire
    [DataRow("April", 4, false)]     // 4 Apr is not strictly before 4 Apr → no fire
    [DataRow("April", 5, false)]     // 5 Apr > 4 Apr → no fire
    [DataRow("December", 31, false)] // late in year → no fire
    public void IfBeforeFixedDate_WhenOccurrenceVsComparison_ShouldFireOnlyWhenStrictlyEarlier(string strategyMonth, int strategyDay, bool expectedFire)
    {
        INotableDateService service = FixedDateService("IfBeforeFixedDate", "April", 4, strategyMonth, strategyDay);

        // The window extends into early 2027 so a late-December occurrence shifted across the year boundary is captured;
        // the 2026 applicability bound guarantees a single occurrence regardless of window width.
        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 31)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(expectedFire, match.IsObserved, $"{strategyMonth} {strategyDay}");
        Assert.AreEqual(expectedFire ? "fixed" : null, match.AdjustmentPolicyId);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfAfterFixedDate" /> fires only when the occurrence falls strictly
    /// after the comparison month and day projected onto the occurrence year. The comparison anchor is 4 April.
    /// </summary>
    /// <param name="strategyMonth">The English month of the holiday's fixed strategy.</param>
    /// <param name="strategyDay">The day of the holiday's fixed strategy.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("January", 1, false)]  // before comparison → no fire
    [DataRow("April", 3, false)]    // 3 Apr < 4 Apr → no fire
    [DataRow("April", 4, false)]    // 4 Apr is not strictly after 4 Apr → no fire
    [DataRow("April", 5, true)]     // 5 Apr > 4 Apr → fire
    [DataRow("December", 31, true)] // late in year → fire
    public void IfAfterFixedDate_WhenOccurrenceVsComparison_ShouldFireOnlyWhenStrictlyLater(string strategyMonth, int strategyDay, bool expectedFire)
    {
        INotableDateService service = FixedDateService("IfAfterFixedDate", "April", 4, strategyMonth, strategyDay);

        // The window extends into early 2027 so a late-December occurrence shifted across the year boundary is captured;
        // the 2026 applicability bound guarantees a single occurrence regardless of window width.
        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 31)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(expectedFire, match.IsObserved, $"{strategyMonth} {strategyDay}");
        Assert.AreEqual(expectedFire ? "fixed" : null, match.AdjustmentPolicyId);
    }

    /// <summary>
    /// Verifies that a 29 February comparison day is clamped to the last valid day of February when projected onto a
    /// non-leap occurrence year, so <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> evaluates without overflow: an
    /// occurrence on 28 February 2026 is not strictly before the clamped 28 February pivot. 2026 is not a leap year.
    /// </summary>
    [TestMethod]
    public void IfBeforeFixedDate_WhenComparisonIsFeb29InNonLeapYear_ShouldClampToFeb28()
    {
        // Occurrence on 15 Feb 2026 → strictly before the clamped 28 Feb pivot → fires.
        Assert.IsTrue(FixedDateService("IfBeforeFixedDate", "February", 29, "February", 15)
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), Territory)
            .Single(r => r.NotableDateId == "probe").IsObserved);

        // Occurrence on 28 Feb 2026 → equals the clamped pivot, not strictly before → does not fire.
        Assert.IsFalse(FixedDateService("IfBeforeFixedDate", "February", 29, "February", 28)
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), Territory)
            .Single(r => r.NotableDateId == "probe").IsObserved);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Nth-occurrence-in-month trigger — fires when the occurrence weekday matches the configured weekday AND its
    // day-of-month falls in the seven-day block the ordinal identifies. June 2026 days 1, 8, 15, 22, 29 are Mondays,
    // letting a single weekday cover every ordinal block.
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a single-holiday service whose June fixed-date holiday fires an
    /// <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> trigger for the supplied weekday and ordinal.
    /// </summary>
    /// <param name="weekday">The English weekday the trigger reacts to.</param>
    /// <param name="ordinal">The week ordinal the trigger reacts to.</param>
    /// <param name="strategyDay">The day of June the holiday's fixed strategy resolves to.</param>
    /// <returns>A service over the generated fixture.</returns>
    private static INotableDateService NthOccurrenceService(string weekday, string ordinal, int strategyDay)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.nth-trigger">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="nth" priority="100">
              <Trigger type="IfNthOccurrenceInMonth" weekOrdinal="{ordinal}"><Weekday value="{weekday}" /></Trigger>
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Nth occurrence" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability fromYear="2026" toYear="2026" /><Strategy><Fixed month="June" day="{strategyDay}" /></Strategy><Adjustments><Adjustment policyRef="nth" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> fires only when the occurrence falls in the
    /// seven-day block the ordinal selects (days 1-7 = First, 8-14 = Second, …) and shares the configured weekday. Each
    /// row resolves a Monday in June 2026 (days 1, 8, 15, 22, 29 are Mondays) against a single weekday set to Monday.
    /// </summary>
    /// <param name="ordinal">The configured week ordinal.</param>
    /// <param name="strategyDay">The Monday day-of-June the holiday resolves to.</param>
    /// <param name="expectedFire">Whether the trigger is expected to fire.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("First", 1, true)]    // 1 Jun → First block
    [DataRow("First", 8, false)]   // 8 Jun → Second block
    [DataRow("Second", 8, true)]   // 8 Jun → Second block
    [DataRow("Second", 1, false)]  // 1 Jun → First block
    [DataRow("Third", 15, true)]   // 15 Jun → Third block
    [DataRow("Fourth", 22, true)]  // 22 Jun → Fourth block
    [DataRow("Fifth", 29, true)]   // 29 Jun → Fifth block
    [DataRow("Fifth", 1, false)]   // 1 Jun → First block
    [DataRow("Last", 29, true)]    // 29 Jun is the last Monday of June 2026
    [DataRow("Last", 22, false)]   // 22 Jun is not the last Monday
    public void IfNthOccurrenceInMonth_WhenOrdinalBlockMatches_ShouldFireOnlyForMatchingBlock(string ordinal, int strategyDay, bool expectedFire)
    {
        NotableDate match = NthOccurrenceService("Monday", ordinal, strategyDay)
            .Resolve(new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(expectedFire, match.IsObserved, $"{ordinal} block, day {strategyDay}");
        Assert.AreEqual(expectedFire ? "nth" : null, match.AdjustmentPolicyId);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> does not fire when the occurrence falls in
    /// the correct ordinal block but on a different weekday than the one configured. 1 June 2026 is a Monday, so a
    /// trigger configured for the first Sunday does not fire even though the day is in the first-week block.
    /// </summary>
    [TestMethod]
    public void IfNthOccurrenceInMonth_WhenWeekdayDiffers_ShouldNotFire()
    {
        NotableDate match = NthOccurrenceService("Sunday", "First", 1)
            .Resolve(new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.IsFalse(match.IsObserved);
        Assert.IsNull(match.AdjustmentPolicyId);
    }
}
