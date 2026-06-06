// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentEmissionMatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Truth tables for the full <see cref="EmissionMode" /> set and for ascending-priority, first-active-wins adjustment
/// selection, resolved end to end through the <see cref="NotableDateService" />. The emission rows mondayise New Year's
/// Day, which is a Saturday in 2022 (the trigger fires) and a Thursday in 2026 (the trigger does not fire), so each mode
/// is exercised on both a fired and a non-fired occurrence. Ported from the v1 emission-mode coverage and the
/// <c>NotableDateRangePipelineScenarioTests.AdjustmentMatrix</c> multiple-adjustment scenarios.
/// </summary>
[TestClass]
public sealed class AdjustmentEmissionMatrixTests
{
    private const string Territory = "XX";

    /// <summary>
    /// Builds a single-holiday service whose New Year's Day mondayisation policy uses the supplied emission mode and a
    /// move-to-next-Monday action gated on the weekend.
    /// </summary>
    /// <param name="emissionMode">The emission mode the policy applies when it fires.</param>
    /// <returns>A service over the generated fixture.</returns>
    private static INotableDateService EmissionService(string emissionMode)
    {
        var xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.emission-matrix">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="mondayise" priority="100">
              <Trigger type="IfWeekend" />
              <Action type="MoveToNextWeekday" dayOfWeek="Monday" />
              <Emission mode="{emissionMode}" reason="New Year substitute" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="new-year" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="mondayise" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Resolves the New Year occurrences in a year's opening fortnight.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="year">The year to resolve.</param>
    /// <returns>The matching occurrences, date-ordered.</returns>
    private static IReadOnlyList<NotableDate> ResolveNewYear(INotableDateService service, int year) =>
        service.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 1, 14)), Territory)
            .Where(r => r.NotableDateId == "new-year")
            .ToList();

    /// <summary>
    /// Verifies that <see cref="EmissionMode.ActualOnly" /> emits only the calculated weekend date with no observed
    /// occurrence, even when the trigger fires. New Year's Day 2022 is a Saturday.
    /// </summary>
    [TestMethod]
    public void Emission_ActualOnly_WhenWeekend_ShouldEmitOnlyActual()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("ActualOnly"), 2022);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(new DateOnly(2022, 1, 1), results[0].Date);
        Assert.IsFalse(results[0].IsObserved);
        Assert.IsNull(results[0].AdjustmentPolicyId);
    }

    /// <summary>
    /// Verifies that <see cref="EmissionMode.ObservedOnly" /> suppresses the calculated weekend date and emits only the
    /// observed Monday substitute. New Year's Day 2022 (Saturday) is observed on Monday 3 January.
    /// </summary>
    [TestMethod]
    public void Emission_ObservedOnly_WhenWeekend_ShouldEmitOnlyObserved()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("ObservedOnly"), 2022);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(new DateOnly(2022, 1, 3), results[0].Date);
        Assert.AreEqual(new DateOnly(2022, 1, 1), results[0].ActualDate);
        Assert.IsTrue(results[0].IsObserved);
        Assert.AreEqual("mondayise", results[0].AdjustmentPolicyId);
    }

    /// <summary>
    /// Verifies that <see cref="EmissionMode.ActualAndObserved" /> emits both the calculated weekend date and the
    /// observed Monday substitute when the trigger fires.
    /// </summary>
    [TestMethod]
    public void Emission_ActualAndObserved_WhenWeekend_ShouldEmitBoth()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("ActualAndObserved"), 2022);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(r => r.Date == new DateOnly(2022, 1, 1) && !r.IsObserved), "actual occurrence");
        Assert.IsTrue(results.Any(r => r.Date == new DateOnly(2022, 1, 3) && r.IsObserved), "observed occurrence");
    }

    /// <summary>
    /// Verifies that <see cref="EmissionMode.ObservedAsAdditional" /> emits the calculated weekend date plus an
    /// additional observed Monday occurrence when the trigger fires.
    /// </summary>
    [TestMethod]
    public void Emission_ObservedAsAdditional_WhenWeekend_ShouldEmitActualPlusObserved()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("ObservedAsAdditional"), 2022);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(r => r.Date == new DateOnly(2022, 1, 1) && !r.IsObserved), "actual occurrence");
        Assert.IsTrue(results.Any(r => r.Date == new DateOnly(2022, 1, 3) && r.IsObserved), "additional observed occurrence");
    }

    /// <summary>
    /// Verifies that <see cref="EmissionMode.Suppress" /> removes the occurrence entirely when the trigger fires on a
    /// weekend, emitting neither the actual nor any observed date.
    /// </summary>
    [TestMethod]
    public void Emission_Suppress_WhenWeekend_ShouldEmitNothing()
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService("Suppress"), 2022);

        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// Verifies that every emission mode leaves a non-firing weekday occurrence as the single unchanged actual date,
    /// because the trigger does not fire and so no emission rule is engaged. New Year's Day 2026 is a Thursday.
    /// </summary>
    /// <param name="emissionMode">The emission mode under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("ActualOnly")]
    [DataRow("ObservedOnly")]
    [DataRow("ActualAndObserved")]
    [DataRow("ObservedAsAdditional")]
    [DataRow("Suppress")]
    public void Emission_WhenWeekdayAndTriggerDoesNotFire_ShouldEmitUnchangedActual(string emissionMode)
    {
        IReadOnlyList<NotableDate> results = ResolveNewYear(EmissionService(emissionMode), 2026);

        Assert.AreEqual(1, results.Count, emissionMode);
        Assert.AreEqual(new DateOnly(2026, 1, 1), results[0].Date);
        Assert.IsFalse(results[0].IsObserved);
        Assert.IsNull(results[0].AdjustmentPolicyId);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Ascending-priority, first-active-wins policy selection across multiple referenced policies.
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a service whose single holiday references two adjustment policies. The policies are authored with the
    /// supplied priorities so the selection order (ascending priority, first active wins) can be asserted independently
    /// of authored order.
    /// </summary>
    /// <param name="firstPriority">The priority of the first referenced policy (a +1 day shift on a weekday).</param>
    /// <param name="secondPriority">The priority of the second referenced policy (a +3 day shift on a weekday).</param>
    /// <returns>A service over the generated fixture.</returns>
    private static INotableDateService TwoWeekdayPolicyService(int firstPriority, int secondPriority)
    {
        var xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.priority">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="shift-one" priority="{firstPriority}">
              <Trigger type="IfWeekday" />
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Shift one" />
            </AdjustmentPolicy>
            <AdjustmentPolicy id="shift-three" priority="{secondPriority}">
              <Trigger type="IfWeekday" />
              <Action type="AddDays" days="3" />
              <Emission mode="ObservedOnly" reason="Shift three" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="x">
                  <Strategy><Fixed month="December" day="25" /></Strategy>
                  <Adjustments>
                    <Adjustment policyRef="shift-one" />
                    <Adjustment policyRef="shift-three" />
                  </Adjustments>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

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

        Assert.AreEqual(new DateOnly(2026, 12, 26), match.Date);
        Assert.AreEqual("shift-one", match.AdjustmentPolicyId);
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

        Assert.AreEqual(new DateOnly(2026, 12, 28), match.Date);
        Assert.AreEqual("shift-three", match.AdjustmentPolicyId);
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

        Assert.AreEqual(new DateOnly(2026, 12, 28), match.Date);
        Assert.AreEqual("weekend-only", match.AdjustmentPolicyId);
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

        Assert.AreEqual(new DateOnly(2026, 12, 26), match.Date);
        Assert.IsFalse(match.IsObserved);
        Assert.IsNull(match.AdjustmentPolicyId);
    }
}
