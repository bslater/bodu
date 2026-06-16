// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentActionMatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Exhaustive shift tables for every date-transforming <see cref="AdjustmentAction" />, resolved end to end through the
/// <see cref="NotableDateService" />. Each policy uses an <see cref="AdjustmentTrigger.Always" /> trigger so the action's
/// transform is always applied, and an observed-only emission so the action's output is the emitted
/// <see cref="NotableDate.Date" />. Ported from the v1 <c>NotableDateAdjusterTests</c> action transforms and the
/// <c>NotableDateRangePipelineScenarioTests.AdjustmentMatrix</c> action-shift matrix, adjusted to the v2 action
/// contract where weekday-seeking actions take an explicit target weekday and working-day searches skip weekends by
/// default.
/// </summary>
/// <remarks>
/// In v2 the weekday-seeking actions are inclusive: <c>MoveToNextWeekday</c> targeting Monday leaves a Monday anchor
/// unchanged and rolls every other weekday forward to the following Monday. Working-day searches start strictly past the
/// anchor, so a working-day anchor still advances one day. Anchor weekdays are annotated inline.
/// </remarks>
[TestClass]
public sealed class AdjustmentActionMatrixTests
{
    private const string Territory = "XX";

    /// <summary>
    /// Resolves the single occurrence with the supplied id from a service, scanning the supplied inclusive window.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="id">The notable-date id whose single occurrence is inspected.</param>
    /// <param name="start">The inclusive window start.</param>
    /// <param name="end">The inclusive window end.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(INotableDateService service, string id, DateOnly start, DateOnly end) =>
        service.Resolve(new DateRange(start, end), Territory).Single(r => r.NotableDateId == id);

    // -----------------------------------------------------------------------------------------------------------------
    // AddDays — signed shift, including offsets that cross month and year boundaries. A single-year applicability bound
    // keeps a cross-year shift from materialising the rule twice over the service's one-year-either-side scan.
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a single-holiday service whose 1 July 2026 holiday shifts by the supplied signed day delta. The holiday is
    /// bound to 2026 so an offset that crosses a year boundary does not produce a second occurrence in a neighbouring
    /// candidate year.
    /// </summary>
    /// <param name="days">The signed day delta applied by the action.</param>
    /// <returns>A service over the generated fixture.</returns>
    private static INotableDateService AddDaysService(int days)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.add-days">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="shift" priority="100">
              <Trigger type="Always" />
              <Action type="AddDays" days="{days}" />
              <Emission mode="ObservedOnly" reason="Shifted" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability fromYear="2026" toYear="2026" /><Strategy><Fixed month="July" day="1" /></Strategy><Adjustments><Adjustment policyRef="shift" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.AddDays" /> shifts the 1 July 2026 anchor by the configured signed
    /// offset, including negative offsets and offsets that cross month and year boundaries.
    /// </summary>
    /// <param name="days">The signed day delta.</param>
    /// <param name="expectedYear">The expected emitted year.</param>
    /// <param name="expectedMonth">The expected emitted month.</param>
    /// <param name="expectedDay">The expected emitted day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(1, 2026, 7, 2)]
    [DataRow(7, 2026, 7, 8)]
    [DataRow(31, 2026, 8, 1)]      // Cross month forward
    [DataRow(180, 2026, 12, 28)]   // Half-year forward
    [DataRow(-1, 2026, 6, 30)]     // Cross month backward
    [DataRow(-7, 2026, 6, 24)]
    [DataRow(-180, 2026, 1, 2)]    // Half-year backward
    [DataRow(365, 2027, 7, 1)]     // Forward into the next year
    [DataRow(-365, 2025, 7, 1)]    // Backward into the prior year
    public void AddDays_WhenAlwaysTrigger_ShouldShiftAnchorBySignedOffset(int days, int expectedYear, int expectedMonth, int expectedDay)
    {
        INotableDateService service = AddDaysService(days);
        DateOnly expected = new(expectedYear, expectedMonth, expectedDay);

        // Scan the full span between the anchor and the shifted date so the emitted occurrence is always visible.
        DateOnly anchor = new(2026, 7, 1);
        DateOnly start = expected < anchor ? expected : anchor;
        DateOnly end = expected > anchor ? expected : anchor;

        NotableDate match = Single(service, "probe", start, end);

        Assert.AreEqual(expected, match.Date);
        Assert.AreEqual(new DateOnly(2026, 7, 1), match.ActualDate);
        Assert.IsTrue(match.IsObserved);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.AddDays" /> with a zero delta leaves the date unchanged while the
    /// observed-only emission still suppresses no occurrence and reports the policy id on the single emitted date.
    /// </summary>
    [TestMethod]
    public void AddDays_WhenZeroOffset_ShouldLeaveDateButReportPolicy()
    {
        NotableDate match = Single(AddDaysService(0), "probe", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 1));

        Assert.AreEqual(
            (new DateOnly(2026, 7, 1), (string?)"shift"),
            (match.Date, match.AdjustmentPolicyId));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Weekday-seeking actions — inclusive OnOrAfter / OnOrBefore against an explicit target weekday.
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a single-holiday service whose holiday resolves to the supplied December 2026 day and always moves to the
    /// supplied target weekday in the supplied direction.
    /// </summary>
    /// <param name="action">The weekday-seeking action (<c>MoveToNextWeekday</c> or <c>MoveToPreviousWeekday</c>).</param>
    /// <param name="targetWeekday">The English target weekday.</param>
    /// <param name="strategyDay">The day of December 2026 the holiday resolves to.</param>
    /// <returns>A service over the generated fixture.</returns>
    private static INotableDateService WeekdayMoveService(string action, string targetWeekday, int strategyDay)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.weekday-move">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="move" priority="100">
              <Trigger type="Always" />
              <Action type="{action}" dayOfWeek="{targetWeekday}" />
              <Emission mode="ObservedOnly" reason="Weekday move" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability fromYear="2026" toYear="2026" /><Strategy><Fixed month="December" day="{strategyDay}" /></Strategy><Adjustments><Adjustment policyRef="move" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToNextWeekday" /> targeting Monday returns the anchor unchanged when
    /// it is already a Monday and otherwise rolls forward to the first following Monday, crossing the year boundary for
    /// anchors after the last Monday of the year.
    /// </summary>
    /// <param name="strategyDay">The day of December 2026 the holiday resolves to.</param>
    /// <param name="expectedYear">The expected emitted year.</param>
    /// <param name="expectedMonth">The expected emitted month.</param>
    /// <param name="expectedDay">The expected emitted day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(25, 2026, 12, 28)] // Fri → Mon 28 Dec
    [DataRow(26, 2026, 12, 28)] // Sat → Mon 28 Dec
    [DataRow(27, 2026, 12, 28)] // Sun → Mon 28 Dec
    [DataRow(28, 2026, 12, 28)] // Mon → unchanged (inclusive)
    [DataRow(29, 2027, 1, 4)]   // Tue → next Mon 4 Jan 2027
    [DataRow(31, 2027, 1, 4)]   // Thu → next Mon 4 Jan 2027
    public void MoveToNextWeekday_WhenTargetingMonday_ShouldRollForwardInclusively(int strategyDay, int expectedYear, int expectedMonth, int expectedDay)
    {
        INotableDateService service = WeekdayMoveService("MoveToNextWeekday", "Monday", strategyDay);
        DateOnly expected = new(expectedYear, expectedMonth, expectedDay);

        NotableDate match = Single(service, "probe", new DateOnly(2026, 12, 1), expected);

        Assert.AreEqual(expected, match.Date);
        Assert.AreEqual(new DateOnly(2026, 12, strategyDay), match.ActualDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToPreviousWeekday" /> targeting Friday returns the anchor unchanged
    /// when it is already a Friday and otherwise rolls back to the preceding Friday.
    /// </summary>
    /// <param name="strategyDay">The day of December 2026 the holiday resolves to.</param>
    /// <param name="expectedDay">The expected emitted day of December 2026.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(25, 25)] // Fri → unchanged (inclusive)
    [DataRow(26, 25)] // Sat → Fri 25 Dec
    [DataRow(27, 25)] // Sun → Fri 25 Dec
    [DataRow(28, 25)] // Mon → Fri 25 Dec
    [DataRow(31, 25)] // Thu → Fri 25 Dec
    public void MoveToPreviousWeekday_WhenTargetingFriday_ShouldRollBackInclusively(int strategyDay, int expectedDay)
    {
        INotableDateService service = WeekdayMoveService("MoveToPreviousWeekday", "Friday", strategyDay);

        NotableDate match = Single(service, "probe", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));

        Assert.AreEqual(new DateOnly(2026, 12, expectedDay), match.Date);
        Assert.AreEqual(new DateOnly(2026, 12, strategyDay), match.ActualDate);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Working-day actions — start strictly past the anchor and skip weekends by default (skipWeekends defaults true).
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a single-holiday service whose holiday resolves to the supplied December 2026 day and always seeks a
    /// working day in the supplied direction with the supplied search bound.
    /// </summary>
    /// <param name="action">The working-day action (<c>MoveToNextWorkingDay</c> or <c>MoveToPreviousWorkingDay</c>).</param>
    /// <param name="strategyDay">The day of December 2026 the holiday resolves to.</param>
    /// <param name="maxSearchDays">The bounded working-day search length.</param>
    /// <returns>A service over the generated fixture.</returns>
    private static INotableDateService WorkingDayService(string action, int strategyDay, int maxSearchDays)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.working-day">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="work" priority="100">
              <Trigger type="Always" />
              <Action type="{action}" maxSearchDays="{maxSearchDays}" />
              <Emission mode="ObservedOnly" reason="Working day" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability fromYear="2026" toYear="2026" /><Strategy><Fixed month="December" day="{strategyDay}" /></Strategy><Adjustments><Adjustment policyRef="work" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToNextWorkingDay" /> advances strictly past the anchor and skips
    /// Saturday and Sunday, so every weekday anchor advances one day and weekend anchors land on the following Monday.
    /// </summary>
    /// <param name="strategyDay">The day of December 2026 the holiday resolves to.</param>
    /// <param name="expectedYear">The expected emitted year.</param>
    /// <param name="expectedMonth">The expected emitted month.</param>
    /// <param name="expectedDay">The expected emitted day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(23, 2026, 12, 24)] // Wed → Thu 24 Dec
    [DataRow(24, 2026, 12, 25)] // Thu → Fri 25 Dec
    [DataRow(25, 2026, 12, 28)] // Fri → Mon 28 Dec (skip Sat/Sun)
    [DataRow(26, 2026, 12, 28)] // Sat → Mon 28 Dec
    [DataRow(27, 2026, 12, 28)] // Sun → Mon 28 Dec
    [DataRow(28, 2026, 12, 29)] // Mon → Tue 29 Dec
    [DataRow(31, 2027, 1, 1)]   // Thu → Fri 1 Jan 2027
    public void MoveToNextWorkingDay_WhenAlwaysTrigger_ShouldAdvancePastWeekends(int strategyDay, int expectedYear, int expectedMonth, int expectedDay)
    {
        INotableDateService service = WorkingDayService("MoveToNextWorkingDay", strategyDay, 7);
        DateOnly expected = new(expectedYear, expectedMonth, expectedDay);

        NotableDate match = Single(service, "probe", new DateOnly(2026, 12, 1), expected);

        Assert.AreEqual(expected, match.Date);
        Assert.AreEqual(new DateOnly(2026, 12, strategyDay), match.ActualDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.MoveToPreviousWorkingDay" /> retreats strictly past the anchor and skips
    /// Saturday and Sunday, so weekend anchors and the following Monday all land on the preceding Friday.
    /// </summary>
    /// <param name="strategyDay">The day of December 2026 the holiday resolves to.</param>
    /// <param name="expectedDay">The expected emitted day of December 2026.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(24, 23)] // Thu → Wed 23 Dec
    [DataRow(25, 24)] // Fri → Thu 24 Dec
    [DataRow(26, 25)] // Sat → Fri 25 Dec
    [DataRow(27, 25)] // Sun → Fri 25 Dec
    [DataRow(28, 25)] // Mon → Fri 25 Dec (skip Sun/Sat)
    [DataRow(29, 28)] // Tue → Mon 28 Dec
    public void MoveToPreviousWorkingDay_WhenAlwaysTrigger_ShouldRetreatPastWeekends(int strategyDay, int expectedDay)
    {
        INotableDateService service = WorkingDayService("MoveToPreviousWorkingDay", strategyDay, 7);

        NotableDate match = Single(service, "probe", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));

        Assert.AreEqual(new DateOnly(2026, 12, expectedDay), match.Date);
        Assert.AreEqual(new DateOnly(2026, 12, strategyDay), match.ActualDate);
    }

    /// <summary>
    /// Verifies that a bounded <see cref="AdjustmentAction.MoveToNextWorkingDay" /> search returns the last scanned day
    /// when the bound is reached before a working day is found. From Friday 25 December 2026 a single-day bound steps to
    /// Saturday, then exhausts its one iteration on Sunday and stops there even though Sunday is still a weekend.
    /// </summary>
    [TestMethod]
    public void MoveToNextWorkingDay_WhenSearchBoundExhausted_ShouldReturnLastScannedDay()
    {
        NotableDate match = Single(WorkingDayService("MoveToNextWorkingDay", 25, 1), "probe", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));

        Assert.AreEqual(new DateOnly(2026, 12, 27), match.Date);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentPolicy.SkipNonWorkingDates" /> advances a substitute past a day already claimed
    /// by another non-working occurrence. An earlier blocker holiday occupies Thursday 24 December 2026, so the probe's
    /// forward working-day search skips both the blocker's day and the weekend.
    /// </summary>
    [TestMethod]
    public void MoveToNextWorkingDay_WhenSkipNonWorkingDates_ShouldStepOverOccupiedDay()
    {
        // The blocker holiday occupies Thu 24 Dec (non-working). The probe on Wed 23 Dec rolls forward: the search steps to
        // 24 Dec, which skipNonWorkingDates treats as blocked, then lands on the free working day Fri 25 Dec.
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.skip-occupied">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="skip-occupied" priority="100">
              <Trigger type="Always" />
              <Action type="MoveToNextWorkingDay" skipWeekends="true" skipNonWorkingDates="true" maxSearchDays="7" />
              <Emission mode="ObservedOnly" reason="Skip occupied" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="blocker" displayName="Blocker" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability fromYear="2026" toYear="2026" /><Strategy><Fixed month="December" day="24" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability fromYear="2026" toYear="2026" /><Strategy><Fixed month="December" day="23" /></Strategy><Adjustments><Adjustment policyRef="skip-occupied" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(xml));

        // Probe anchor Wed 23 Dec → step to Thu 24 Dec (occupied by blocker, skipped) → Fri 25 Dec (free working day).
        NotableDate probe = Single(service, "probe", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));

        Assert.AreEqual(
            (new DateOnly(2026, 12, 25), (DateOnly?)new DateOnly(2026, 12, 23)),
            (probe.Date, probe.ActualDate));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Reference and no-op actions.
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.ReplaceWithRule" /> replaces the occurrence date with the referenced
    /// concept's occurrence for the same year, preserving the calculated date as the actual date.
    /// </summary>
    [TestMethod]
    public void ReplaceWithRule_WhenReferenceResolves_ShouldUseReferencedDate()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.replace-action">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="replace" priority="100">
              <Trigger type="Always" />
              <Action type="ReplaceWithRule" notableDateRef="target" />
              <Emission mode="ObservedOnly" reason="Replaced" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="target" displayName="Target" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="July" day="15" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="replacement" displayName="Replacement" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="July" day="1" /></Strategy><Adjustments><Adjustment policyRef="replace" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(xml));

        NotableDate match = Single(service, "replacement", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));

        Assert.AreEqual(
            (new DateOnly(2026, 7, 15), (DateOnly?)new DateOnly(2026, 7, 1), true),
            (match.Date, match.ActualDate, match.IsObserved));
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.None" /> leaves the occurrence date unchanged even though the trigger
    /// fires; under an actual-only emission the single emitted occurrence is the unchanged actual date with no policy id.
    /// </summary>
    [TestMethod]
    public void None_WhenAlwaysTriggerAndActualOnly_ShouldEmitUnchangedActual()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.none-action">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="noop" priority="100">
              <Trigger type="Always" />
              <Action type="None" />
              <Emission mode="ActualOnly" reason="No-op" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="July" day="1" /></Strategy><Adjustments><Adjustment policyRef="noop" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(xml));

        NotableDate match = Single(service, "probe", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));

        Assert.AreEqual(
            (new DateOnly(2026, 7, 1), false, (string?)null),
            (match.Date, match.IsObserved, match.AdjustmentPolicyId));
    }
}
