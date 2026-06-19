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
public sealed partial class AdjustmentActionMatrixTests
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
}
