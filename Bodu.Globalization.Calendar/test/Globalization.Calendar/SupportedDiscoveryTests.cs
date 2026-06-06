// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SupportedDiscoveryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the resource-introspection APIs <see cref="INotableDateService.GetSupportedTerritories" /> and
/// <see cref="INotableDateService.GetSupportedCalendars" />, restoring the v1 discovery surface.
/// </summary>
[TestClass]
public sealed class SupportedDiscoveryTests
{
    /// <summary>
    /// A resource mixing a global rule, three territory-scoped rules, and two non-Gregorian-calendar rules.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.discovery">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="global" displayName="Global" category="Observance">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="au" displayName="AU" category="PublicHoliday">
          <Rules><Rule id="x"><Applicability><Territory code="US" /></Applicability><Strategy><Fixed month="July" day="4" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="nsw" displayName="NSW" category="PublicHoliday">
          <Rules><Rule id="x"><Applicability><Territory code="AU-NSW" /></Applicability><Strategy><Fixed month="August" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="auday" displayName="AU Day" category="PublicHoliday">
          <Rules><Rule id="x"><Applicability><Territory code="AU" /></Applicability><Strategy><Fixed month="January" day="26" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="hijri" displayName="Hijri" category="Religious">
          <Rules><Rule id="x"><Applicability calendar="Hijri" /><Strategy><Fixed month="9" day="1" sweepCalendarYears="true" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="hebrew" displayName="Hebrew" category="Religious">
          <Rules><Rule id="x"><Applicability calendar="Hebrew" /><Strategy><Fixed month="Tishri" day="1" sweepCalendarYears="true" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Builds a service over the supplied resource XML.
    /// </summary>
    /// <param name="xml">The resource XML.</param>
    /// <returns>A service over the resource.</returns>
    private static INotableDateService Build(string xml) =>
        new NotableDateService(NotableDateResourceLoader.Load(xml));

    /// <summary>
    /// Verifies that the supported territories are the distinct scoped codes in case-insensitive ascending order, and
    /// that global rules contribute nothing.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_ShouldReturnScopedCodesSorted()
    {
        CollectionAssert.AreEqual(new[] { "AU", "AU-NSW", "US" }, Build(Xml).GetSupportedTerritories().ToArray());
    }

    /// <summary>
    /// Verifies that the supported calendars are the distinct calendar systems in ascending order, including Gregorian.
    /// </summary>
    [TestMethod]
    public void GetSupportedCalendars_ShouldReturnDistinctSorted()
    {
        CollectionAssert.AreEqual(
            new[] { CalendarSystem.Gregorian, CalendarSystem.Hijri, CalendarSystem.Hebrew },
            Build(Xml).GetSupportedCalendars().ToArray());
    }

    /// <summary>
    /// Verifies that a resource whose rules are all global reports no supported territories.
    /// </summary>
    [TestMethod]
    public void GetSupportedTerritories_WhenAllRulesGlobal_ShouldReturnEmpty()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.discovery-global">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="n" displayName="N" category="Observance">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        Assert.IsEmpty(Build(xml).GetSupportedTerritories());
    }
}
