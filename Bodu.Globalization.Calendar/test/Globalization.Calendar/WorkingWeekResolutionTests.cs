// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingWeekResolutionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

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
public sealed partial class WorkingWeekResolutionTests
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

}
