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
public sealed partial class AdjustmentEmissionMatrixTests
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
        string xml = $"""
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
        string xml = $"""
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
}
