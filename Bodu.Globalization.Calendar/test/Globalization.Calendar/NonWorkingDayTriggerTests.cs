// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonWorkingDayTriggerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="AdjustmentTrigger.IfNonWorkingDay" /> and <see cref="AdjustmentTrigger.IfWorkingDay" />
/// triggers, which extend the weekend triggers by also accounting for a day already claimed by another non-working
/// occurrence. The fixture anchors each example on a distinct day in January 2026 (1 Jan Thu, 3 Jan Sat, 8 Jan Thu,
/// 10 Jan Sat, 15 Jan Thu) so the cases never interfere.
/// </summary>
[TestClass]
public sealed partial class NonWorkingDayTriggerTests
{
    private const string Territory = "ZZ";

    /// <summary>
    /// A fixture exercising every non-working-day and working-day trigger branch under the default Monday-to-Friday
    /// working week.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.nonworkingday">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="non-working-next-working-day" priority="100">
          <Trigger type="IfNonWorkingDay" />
          <Action type="MoveToNextWorkingDay" maxSearchDays="7" skipWeekends="true" skipNonWorkingDates="true" />
          <Emission mode="ObservedOnly" reason="Observed next working day" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="working-day-shift-two" priority="100">
          <Trigger type="IfWorkingDay" />
          <Action type="AddDays" days="2" />
          <Emission mode="ObservedOnly" reason="Shifted on working day" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="collide-a" displayName="Collide A" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-1"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="1" /></Strategy></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="collide-b" displayName="Collide B" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-1"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="1" /></Strategy>
              <Adjustments><Adjustment policyRef="non-working-next-working-day" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="weekend-holiday" displayName="Weekend Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-3"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="3" /></Strategy>
              <Adjustments><Adjustment policyRef="non-working-next-working-day" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="lone-working" displayName="Lone Working" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-8"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="8" /></Strategy>
              <Adjustments><Adjustment policyRef="non-working-next-working-day" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="working-shift" displayName="Working Shift" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-15"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="15" /></Strategy>
              <Adjustments><Adjustment policyRef="working-day-shift-two" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="weekend-no-shift" displayName="Weekend No Shift" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-10"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="10" /></Strategy>
              <Adjustments><Adjustment policyRef="working-day-shift-two" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Builds a service over the fixture.
    /// </summary>
    /// <returns>A service over the fixture.</returns>
    private static INotableDateService Build() =>
        new NotableDateService(NotableDateResourceLoader.Load(Xml));

}
