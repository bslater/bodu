// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScopedRemovalTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that an occurrence can be removed for a scoped subset of years or territories through the override system:
/// year-scoped removal via a <c>PatchRule</c> that excludes a year, and territory-scoped removal via an <c>AddRule</c>
/// that injects a more-specific shadowing rule whose policy suppresses emission.
/// </summary>
[TestClass]
public sealed partial class ScopedRemovalTests
{
    /// <summary>
    /// A holiday on 1 January whose <c>PatchRule</c> override excludes the year 2025.
    /// </summary>
    private const string YearScopedXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.scoped-year">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="holiday" displayName="Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
      <Overrides>
        <PatchRule notableDateRef="holiday" ruleRef="x">
          <Applicability><ExceptYear value="2025" /></Applicability>
        </PatchRule>
      </Overrides>
    </NotableDateResource>
    """;

    /// <summary>
    /// A national (AU) holiday on 26 January whose <c>AddRule</c> override injects an AU-NSW rule that suppresses it.
    /// </summary>
    private const string TerritoryScopedXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.scoped-territory">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="suppress-all" priority="100">
          <Trigger type="Always" />
          <Action type="None" />
          <Emission mode="Suppress" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="holiday" displayName="Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="national"><Applicability><Territory code="AU" /></Applicability><Strategy><Fixed month="January" day="26" /></Strategy></Rule>
          </Rules>
        </NotableDate>
      </NotableDates>
      <Overrides>
        <AddRule notableDateRef="holiday">
          <Rule id="nsw-suppress">
            <Applicability><Territory code="AU-NSW" /></Applicability>
            <Strategy><Fixed month="January" day="26" /></Strategy>
            <Adjustments><Adjustment policyRef="suppress-all" /></Adjustments>
          </Rule>
        </AddRule>
      </Overrides>
    </NotableDateResource>
    """;

    /// <summary>
    /// Counts the occurrences of the holiday emitted on a date for a territory.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="date">The date to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>The number of holiday occurrences emitted.</returns>
    private static int Count(INotableDateService service, DateOnly date, string territory) =>
        service.Resolve(date, territory).Count(r => r.NotableDateId == "holiday");

}
