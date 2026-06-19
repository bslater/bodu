// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentEmissionMatrixTests.Selection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentEmissionMatrixTests
{
    /// <summary>
    /// Verifies that when two referenced policies both activate, the one with the lower priority value is selected
    /// (ascending-priority, first-active-wins). 25 December 2026 is a Friday, so both weekday policies fire; the
    /// priority-10 (+1 day) policy wins over the priority-20 (+3 day) policy and the result is independent of authored
    /// order.
    /// </summary>
    [TestMethod]
    public void Selection_WhenBothPoliciesFire_ShouldKeepLowestPriorityValue()
    {
        NotableDate match = TwoWeekdayPolicyService(firstPriority: 10, secondPriority: 20)
            .Resolve(new DateRange(new DateOnly(2026, 12, 25), new DateOnly(2026, 12, 31)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(
            (new DateOnly(2026, 12, 26), (string?)"shift-one"),
            (match.Date, match.AdjustmentPolicyId));
    }

    /// <summary>
    /// Verifies that the ascending-priority selection ignores the authored element order: swapping the priorities so the
    /// +3 day policy is the lower value makes it win, even though the +1 day policy is referenced first.
    /// </summary>
    [TestMethod]
    public void Selection_WhenAuthoredOrderReversedAgainstPriority_ShouldFollowPriorityNotOrder()
    {
        NotableDate match = TwoWeekdayPolicyService(firstPriority: 20, secondPriority: 10)
            .Resolve(new DateRange(new DateOnly(2026, 12, 25), new DateOnly(2026, 12, 31)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(
            (new DateOnly(2026, 12, 28), (string?)"shift-three"),
            (match.Date, match.AdjustmentPolicyId));
    }

    /// <summary>
    /// Verifies that when only one of two referenced policies activates, that single activation is selected regardless of
    /// its priority. 26 December 2026 is a Saturday: the weekday policy does not fire, so the weekend policy is selected
    /// even though it carries the higher priority value.
    /// </summary>
    [TestMethod]
    public void Selection_WhenOnlyOnePolicyFires_ShouldSelectTheActiveOne()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.one-active">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="weekday-only" priority="10">
              <Trigger type="IfWeekday" />
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Weekday" />
            </AdjustmentPolicy>
            <AdjustmentPolicy id="weekend-only" priority="20">
              <Trigger type="IfWeekend" />
              <Action type="AddDays" days="2" />
              <Emission mode="ObservedOnly" reason="Weekend" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="x">
                  <Strategy><Fixed month="December" day="26" /></Strategy>
                  <Adjustments>
                    <Adjustment policyRef="weekday-only" />
                    <Adjustment policyRef="weekend-only" />
                  </Adjustments>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(xml));

        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2026, 12, 26), new DateOnly(2026, 12, 31)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(
            (new DateOnly(2026, 12, 28), (string?)"weekend-only"),
            (match.Date, match.AdjustmentPolicyId));
    }

    /// <summary>
    /// Verifies that when no referenced policy activates, the occurrence is emitted as its unchanged actual date with no
    /// policy id. 26 December 2026 is a Saturday, so a single weekday-gated policy does not fire.
    /// </summary>
    [TestMethod]
    public void Selection_WhenNoPolicyFires_ShouldEmitUnchangedActual()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.none-active">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="weekday-only" priority="10">
              <Trigger type="IfWeekday" />
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Weekday" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="December" day="26" /></Strategy><Adjustments><Adjustment policyRef="weekday-only" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(xml));

        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2026, 12, 26), new DateOnly(2026, 12, 31)), Territory)
            .Single(r => r.NotableDateId == "probe");

        Assert.AreEqual(
            (new DateOnly(2026, 12, 26), false, (string?)null),
            (match.Date, match.IsObserved, match.AdjustmentPolicyId));
    }
}
