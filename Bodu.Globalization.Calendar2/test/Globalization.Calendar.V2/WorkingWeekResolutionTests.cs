// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingWeekResolutionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies that a resource's configured working week drives weekend-sensitive resolution: the
/// <see cref="AdjustmentTrigger.IfWeekend" /> trigger and the working-day search both treat the days outside the
/// configured working week as the weekend, rather than a hard-coded Saturday and Sunday.
/// </summary>
/// <remarks>
/// The fixture anchors a single holiday on Friday 2 January 2026. Under a Sunday-to-Thursday working week that Friday
/// is a weekend day; under the default Monday-to-Friday working week it is a working day.
/// </remarks>
[TestClass]
public sealed class WorkingWeekResolutionTests
{
    private const string Territory = "ZZ";

    /// <summary>
    /// Builds a service over a fixture whose single holiday falls on Friday 2 January 2026 and moves to the next
    /// working day when it falls on a weekend.
    /// </summary>
    /// <param name="resolutionPolicyAttributes">Additional attributes to splice onto the <c>ResolutionPolicy</c> element.</param>
    /// <returns>A service over the constructed resource.</returns>
    private static INotableDateService Build(string resolutionPolicyAttributes) =>
        new NotableDateService(NotableDateResourceLoader.Load($$"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.workingweek">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" {{resolutionPolicyAttributes}} />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="weekend-next-working-day" priority="100">
              <Trigger type="IfWeekend" />
              <Action type="MoveToNextWorkingDay" maxSearchDays="7" skipWeekends="true" skipNonWorkingDates="true" />
              <Emission mode="ObservedOnly" reason="Observed next working day" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="friday-holiday" displayName="Friday Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="jan-2"><Applicability><Territory code="ZZ" /></Applicability>
                  <Strategy><Fixed month="January" day="2" /></Strategy>
                  <Adjustments><Adjustment policyRef="weekend-next-working-day" /></Adjustments></Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """));

    /// <summary>
    /// Verifies that under a Sunday-to-Thursday working week a Friday holiday is treated as a weekend occurrence and is
    /// observed on the following Sunday, skipping the configured Friday and Saturday weekend.
    /// </summary>
    [TestMethod]
    public void IfWeekend_WhenWorkingWeekIsSundayToThursday_ForFridayHoliday_ShouldObserveOnFollowingSunday()
    {
        INotableDateService service = Build("workingDays=\"1111100\"");

        Assert.AreEqual(0, service.Resolve(new DateOnly(2026, 1, 2), Territory).Count, "Friday actual suppressed");

        IReadOnlyList<NotableDate> observed = service.Resolve(new DateOnly(2026, 1, 4), Territory);
        Assert.AreEqual(1, observed.Count);
        Assert.IsTrue(observed[0].IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 2), observed[0].ActualDate);
        Assert.AreEqual(new DateOnly(2026, 1, 4), observed[0].Date);
    }

    /// <summary>
    /// Verifies that under the default Monday-to-Friday working week a Friday holiday is a working-day occurrence, so
    /// the weekend trigger does not fire and the holiday is emitted on its actual Friday date.
    /// </summary>
    [TestMethod]
    public void IfWeekend_WhenDefaultWorkingWeek_ForFridayHoliday_ShouldEmitActualDate()
    {
        INotableDateService service = Build(string.Empty);

        IReadOnlyList<NotableDate> actual = service.Resolve(new DateOnly(2026, 1, 2), Territory);
        Assert.AreEqual(1, actual.Count);
        Assert.IsFalse(actual[0].IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 2), actual[0].Date);

        Assert.AreEqual(0, service.Resolve(new DateOnly(2026, 1, 4), Territory).Count, "no observed occurrence emitted");
    }

    /// <summary>
    /// Verifies that the configured weekend leaves the default behavior intact: under the default Monday-to-Friday
    /// working week the move-to-next-working-day search still skips Saturday and Sunday.
    /// </summary>
    [TestMethod]
    public void MoveToNextWorkingDay_WhenDefaultWorkingWeek_ShouldSkipSaturdayAndSunday()
    {
        // The base fixture holiday is on a Friday; under the default working week the weekend trigger never fires, so
        // build a parallel fixture whose holiday lands on Saturday 3 January 2026 to exercise the working-day search.
        INotableDateService service = new NotableDateService(NotableDateResourceLoader.Load("""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.workingweek.saturday">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="weekend-next-working-day" priority="100">
              <Trigger type="IfWeekend" />
              <Action type="MoveToNextWorkingDay" maxSearchDays="7" skipWeekends="true" skipNonWorkingDates="true" />
              <Emission mode="ObservedOnly" reason="Observed next working day" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="saturday-holiday" displayName="Saturday Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="jan-3"><Applicability><Territory code="ZZ" /></Applicability>
                  <Strategy><Fixed month="January" day="3" /></Strategy>
                  <Adjustments><Adjustment policyRef="weekend-next-working-day" /></Adjustments></Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """));

        IReadOnlyList<NotableDate> observed = service.Resolve(new DateOnly(2026, 1, 5), Territory);
        Assert.AreEqual(1, observed.Count);
        Assert.IsTrue(observed[0].IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 3), observed[0].ActualDate);
        Assert.AreEqual(new DateOnly(2026, 1, 5), observed[0].Date);
    }
}
