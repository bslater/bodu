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
public sealed class ScopedRemovalTests
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

    /// <summary>
    /// Verifies that a PatchRule override excluding 2025 removes the 1 January occurrence for that year alone while
    /// leaving neighbouring years intact.
    /// </summary>
    /// <param name="year">The civil year under test.</param>
    /// <param name="expectedCount">The expected number of emitted occurrences on 1 January of that year.</param>
    [TestMethod]
    [DataRow(2024, 1)]  // neighbouring year intact
    [DataRow(2025, 0)]  // excluded year removed
    [DataRow(2026, 1)]  // neighbouring year intact
    public void PatchRuleExceptYear_WhenYearExcluded_ShouldRemoveOccurrenceForThatYearOnly(int year, int expectedCount)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(YearScopedXml));

        Assert.AreEqual(expectedCount, Count(service, new DateOnly(year, 1, 1), "XX"));
    }

    /// <summary>
    /// Verifies that an AddRule override injecting a more-specific suppressing rule removes the 26 January 2025
    /// occurrence for the shadowed subterritory alone while leaving the parent and sibling subterritories intact.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="expectedCount">The expected number of emitted occurrences for that territory.</param>
    [TestMethod]
    [DataRow("AU", 1)]       // national parent intact
    [DataRow("AU-NSW", 0)]   // shadowed subterritory removed
    [DataRow("AU-VIC", 1)]   // sibling subterritory intact
    public void AddRuleSuppressShadow_WhenSubterritoryShadowed_ShouldRemoveOccurrenceForThatTerritoryOnly(string territory, int expectedCount)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(TerritoryScopedXml));

        Assert.AreEqual(expectedCount, Count(service, new DateOnly(2025, 1, 26), territory));
    }
}
