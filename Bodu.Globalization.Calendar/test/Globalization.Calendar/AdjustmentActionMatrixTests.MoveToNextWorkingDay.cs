// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentActionMatrixTests.MoveToNextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentActionMatrixTests
{
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
}
