// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingWeekResolutionTests.MoveToNextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingWeekResolutionTests
{
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

        Assert.HasCount(1, observed);
        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 5)),
            (observed[0].IsObserved, observed[0].ActualDate, observed[0].Date));
    }
}
