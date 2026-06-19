// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerMatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Exhaustive truth tables for every <see cref="AdjustmentTrigger" /> firing condition, resolved end to end through the
/// <see cref="NotableDateService" />. Each policy pairs the trigger under test with an observed-only
/// <see cref="AdjustmentAction.AddDays" /> shift so that "the policy fired" is observable as an emitted occurrence whose
/// <see cref="NotableDate.AdjustmentPolicyId" /> is set and whose <see cref="NotableDate.IsObserved" /> flag is
/// <see langword="true" />, while "the policy did not fire" surfaces as the unchanged actual occurrence with a
/// <see langword="null" /> policy id. Ported from the v1 <c>NotableDateAdjusterTests</c> trigger truth tables and the
/// <c>NotableDateRangePipelineScenarioTests.AdjustmentMatrix</c> trigger-activation matrix.
/// </summary>
/// <remarks>
/// Anchor weekdays referenced in the rows: 1 Jan 2022 is a Saturday, 1 Jan 2023 a Sunday, 1 Jan 2026 a Thursday;
/// dates in June/July/December 2026 are annotated inline.
/// </remarks>
[TestClass]
public sealed partial class AdjustmentTriggerMatrixTests
{
    private const string Territory = "XX";

    /// <summary>
    /// A resource exposing one holiday per non-parameterized trigger, each firing an observed-only one-day shift so a
    /// fired trigger is visible as a populated <see cref="NotableDate.AdjustmentPolicyId" />. Every holiday uses a fixed
    /// 1 January strategy so the anchor weekday is controlled purely by the resolved year.
    /// </summary>
    private const string TriggerXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.trigger-matrix">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="always" priority="100">
          <Trigger type="Always" />
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="Always" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-weekend" priority="100">
          <Trigger type="IfWeekend" />
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If weekend" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-weekday" priority="100">
          <Trigger type="IfWeekday" />
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If weekday" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-leap-year" priority="100">
          <Trigger type="IfLeapYear" />
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If leap year" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-sunday" priority="100">
          <Trigger type="IfDayOfWeek"><Weekday value="Sunday" /></Trigger>
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If Sunday" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="if-sat-or-sun" priority="100">
          <Trigger type="IfDayOfWeek">
            <Weekday value="Saturday" />
            <Weekday value="Sunday" />
          </Trigger>
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="If Saturday or Sunday" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="always-h" displayName="Always" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="always" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="weekend-h" displayName="Weekend" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-weekend" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="weekday-h" displayName="Weekday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-weekday" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="leap-h" displayName="Leap" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-leap-year" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="sunday-h" displayName="Sunday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-sunday" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="sat-sun-h" displayName="Sat or Sun" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="if-sat-or-sun" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Gets a shared service over the trigger-matrix fixture.
    /// </summary>
    private static INotableDateService TriggerService =>
        new NotableDateService(NotableDateResourceLoader.Load(TriggerXml));

    /// <summary>
    /// Resolves the single occurrence with the supplied id for the year of the anchor date and asserts whether the
    /// trigger fired. A fired trigger is identified by a populated <see cref="NotableDate.AdjustmentPolicyId" />.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="id">The notable-date id whose single occurrence is inspected.</param>
    /// <param name="anchorYear">The Gregorian year whose 1 January anchor is resolved.</param>
    /// <param name="expectedPolicyId">The policy id expected when the trigger fires, or <see langword="null" />.</param>
    private static void AssertActivation(INotableDateService service, string id, int anchorYear, string? expectedPolicyId)
    {
        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(anchorYear, 1, 1), new DateOnly(anchorYear, 1, 7)), Territory)
            .Single(r => r.NotableDateId == id);

        if (expectedPolicyId is null)
        {
            Assert.IsFalse(match.IsObserved, $"Expected {id} to be unadjusted for {anchorYear}.");
            Assert.IsNull(match.AdjustmentPolicyId);
            Assert.AreEqual(new DateOnly(anchorYear, 1, 1), match.Date);
        }
        else
        {
            Assert.IsTrue(match.IsObserved, $"Expected {id} to be adjusted for {anchorYear}.");
            Assert.AreEqual(expectedPolicyId, match.AdjustmentPolicyId);
            Assert.AreEqual(new DateOnly(anchorYear, 1, 1), match.ActualDate);
            Assert.AreEqual(new DateOnly(anchorYear, 1, 2), match.Date);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Fixed-date triggers — comparison month/day projected onto the occurrence year. A per-row fixture lets the
    // occurrence date vary so both the strictly-before and strictly-after boundaries are exercised.
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a single-holiday service whose fixed-date holiday (bound to 2026) fires the supplied fixed-date trigger
    /// against the supplied comparison month and day, shifting one day forward on activation.
    /// </summary>
    /// <param name="triggerType">The fixed-date trigger type (<c>IfBeforeFixedDate</c> or <c>IfAfterFixedDate</c>).</param>
    /// <param name="month">The English comparison month name.</param>
    /// <param name="day">The comparison day of month.</param>
    /// <param name="strategyMonth">The English month of the holiday's fixed strategy.</param>
    /// <param name="strategyDay">The day of the holiday's fixed strategy.</param>
    /// <returns>A service over the generated fixture.</returns>
    private static INotableDateService FixedDateService(string triggerType, string month, int day, string strategyMonth, int strategyDay)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.fixed-trigger">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="fixed" priority="100">
              <Trigger type="{triggerType}" month="{month}" day="{day}" />
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Fixed-date trigger" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability fromYear="2026" toYear="2026" /><Strategy><Fixed month="{strategyMonth}" day="{strategyDay}" /></Strategy><Adjustments><Adjustment policyRef="fixed" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Nth-occurrence-in-month trigger — fires when the occurrence weekday matches the configured weekday AND its
    // day-of-month falls in the seven-day block the ordinal identifies. June 2026 days 1, 8, 15, 22, 29 are Mondays,
    // letting a single weekday cover every ordinal block.
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Builds a single-holiday service whose June fixed-date holiday fires an
    /// <see cref="AdjustmentTrigger.IfNthOccurrenceInMonth" /> trigger for the supplied weekday and ordinal.
    /// </summary>
    /// <param name="weekday">The English weekday the trigger reacts to.</param>
    /// <param name="ordinal">The week ordinal the trigger reacts to.</param>
    /// <param name="strategyDay">The day of June the holiday's fixed strategy resolves to.</param>
    /// <returns>A service over the generated fixture.</returns>
    private static INotableDateService NthOccurrenceService(string weekday, string ordinal, int strategyDay)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.nth-trigger">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="nth" priority="100">
              <Trigger type="IfNthOccurrenceInMonth" weekOrdinal="{ordinal}"><Weekday value="{weekday}" /></Trigger>
              <Action type="AddDays" days="1" />
              <Emission mode="ObservedOnly" reason="Nth occurrence" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability fromYear="2026" toYear="2026" /><Strategy><Fixed month="June" day="{strategyDay}" /></Strategy><Adjustments><Adjustment policyRef="nth" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }
}
