// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CustomAdjustmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="AdjustmentAction.ReplaceWithRule" /> reference action and the
/// <see cref="AdjustmentAction.Custom" /> handler action, including their validation and runtime behaviour.
/// </summary>
[TestClass]
public sealed partial class CustomAdjustmentTests
{
    /// <summary>
    /// A resource whose moved holiday replaces its date with the occurrence of an anchor concept.
    /// </summary>
    private const string ReplaceXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.replace">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="replace-with-anchor" priority="100">
          <Trigger type="Always" />
          <Action type="ReplaceWithRule" notableDateRef="anchor-day" />
          <Emission mode="ObservedOnly" reason="Moved to anchor" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="anchor-day" displayName="Anchor Day" category="PublicHoliday" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="10" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="moved-holiday" displayName="Moved Holiday" category="PublicHoliday" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="replace-with-anchor" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A resource whose holiday defers its observed date to a custom handler.
    /// </summary>
    private const string CustomXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.custom-action">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="shift-ten" priority="100">
          <Trigger type="Always" />
          <Action type="Custom" handlerKey="shift-ten" />
          <Emission mode="ObservedOnly" reason="Custom shift" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="custom-holiday" displayName="Custom Holiday" category="PublicHoliday" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="March" day="1" /></Strategy><Adjustments><Adjustment policyRef="shift-ten" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A custom handler that shifts an occurrence forward by ten days.
    /// </summary>
    private sealed class ShiftTenHandler
        : IAdjustmentHandler
    {
        /// <inheritdoc />
        public DateOnly? Adjust(AdjustmentHandlerContext context) =>
            context.BaseDate.AddDays(10);
    }

    /// <summary>
    /// Resolves the single occurrence with the supplied id for a year over the supplied service.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="id">The notable-date id.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(INotableDateService service, int year, string id) =>
        service.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX").Single(r => r.NotableDateId == id);

}
