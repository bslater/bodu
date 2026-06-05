// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionTerritoryContainmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Ports the v1 territory-forwarding matrix to the v2 explicit-service surface, verifying that the working-day
/// extension methods forward the requested territory to the service and observe v2 territory containment when deciding
/// whether a scoped holiday applies.
/// </summary>
/// <remarks>
/// <para>
/// v2 territory containment is one-directional, unlike the v1 bidirectional model: a rule scoped to a parent
/// (<c>AU</c>) matches a subnational query (<c>AU-NSW</c>), but a rule scoped to a subnational territory
/// (<c>AU-NSW</c>) does <i>not</i> match a national query (<c>AU</c>). The truth table below is therefore expressed in
/// v2 terms. The v1 <c>("AU-NSW", "AU", true)</c> row becomes <see langword="false" />, and the v1 null-query row is
/// not portable because v2 throws <see cref="ArgumentNullException" /> for a null territory.
/// </para>
/// <para>
/// The single scoped rule fires on Tuesday 7 April 2026 (a weekday). When the rule applies to the query territory the
/// day is non-working; otherwise it is an ordinary working day.
/// </para>
/// </remarks>
[TestClass]
public sealed class ExtensionTerritoryContainmentTests
{
    /// <summary>
    /// Builds a service whose only rule is a single-day non-working holiday on 7 April 2026 scoped to the supplied
    /// territory.
    /// </summary>
    /// <param name="ruleTerritory">The territory the holiday rule is scoped to.</param>
    /// <returns>A service over the scoped-holiday fixture.</returns>
    private static INotableDateService BuildHolidayService(string ruleTerritory)
    {
        string xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.territory-fwd">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="holiday" displayName="Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="x"><Applicability><Territory code="{ruleTerritory}" /></Applicability><Strategy><Fixed month="April" day="7" /></Strategy></Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Provides the v2 parent-to-child containment truth table: a rule applies to a query when the rule's territory is
    /// the query itself or a parent of it.
    /// </summary>
    /// <returns>A sequence of <c>(string ruleTerritory, string queryTerritory, bool applies)</c> rows.</returns>
    public static IEnumerable<object[]> TerritoryContainmentRows()
    {
        yield return new object[] { "AU", "AU", true };          // exact national match
        yield return new object[] { "AU", "AU-NSW", true };      // parent rule matches subnational query
        yield return new object[] { "AU-NSW", "AU", false };     // subnational rule does NOT match national query (v2: one-directional)
        yield return new object[] { "AU-NSW", "AU-NSW", true };  // exact subnational match
        yield return new object[] { "AU-NSW", "AU-VIC", false }; // sibling subdivisions do not match
        yield return new object[] { "AU", "NZ", false };         // unrelated territory does not match
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.IsNonWorkingDay" /> forwards the query territory and reports
    /// the scoped holiday as non-working only when the rule applies to the query under v2 containment.
    /// </summary>
    /// <param name="ruleTerritory">The territory the rule is scoped to.</param>
    /// <param name="queryTerritory">The territory the query is made for.</param>
    /// <param name="expectedNonWorking">Whether the holiday is expected to apply to the query.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(TerritoryContainmentRows), DynamicDataSourceType.Method)]
    public void IsNonWorkingDay_WhenTerritoryForwarded_ShouldHonourContainment(string ruleTerritory, string queryTerritory, bool expectedNonWorking)
    {
        INotableDateService service = BuildHolidayService(ruleTerritory);

        bool actual = new DateOnly(2026, 4, 7).IsNonWorkingDay(service, queryTerritory);

        Assert.AreEqual(expectedNonWorking, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.NextWorkingDay" /> skips the scoped holiday only when the rule
    /// applies to the query territory; otherwise the holiday is treated as a working day. From Monday 6 April 2026 the
    /// next working day is Wednesday 8 April when the holiday applies, or Tuesday 7 April when it does not.
    /// </summary>
    /// <param name="ruleTerritory">The territory the rule is scoped to.</param>
    /// <param name="queryTerritory">The territory the query is made for.</param>
    /// <param name="expectedSkipsHoliday">Whether the holiday is expected to be skipped.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(TerritoryContainmentRows), DynamicDataSourceType.Method)]
    public void NextWorkingDay_WhenTerritoryForwarded_ShouldHonourContainment(string ruleTerritory, string queryTerritory, bool expectedSkipsHoliday)
    {
        INotableDateService service = BuildHolidayService(ruleTerritory);

        DateOnly actual = new DateOnly(2026, 4, 6).NextWorkingDay(service, queryTerritory);

        DateOnly expected = expectedSkipsHoliday ? new DateOnly(2026, 4, 8) : new DateOnly(2026, 4, 7);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.WorkingDaysBetween" /> forwards the query territory and
    /// excludes the scoped holiday only when the rule applies. The inclusive range 6 to 10 April 2026 is all weekdays,
    /// so the count is four when the Tuesday holiday applies and five when it does not.
    /// </summary>
    /// <param name="ruleTerritory">The territory the rule is scoped to.</param>
    /// <param name="queryTerritory">The territory the query is made for.</param>
    /// <param name="expectedExcludesHoliday">Whether the holiday is expected to be excluded from the count.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(TerritoryContainmentRows), DynamicDataSourceType.Method)]
    public void WorkingDaysBetween_WhenTerritoryForwarded_ShouldHonourContainment(string ruleTerritory, string queryTerritory, bool expectedExcludesHoliday)
    {
        INotableDateService service = BuildHolidayService(ruleTerritory);

        int actual = new DateOnly(2026, 4, 6).WorkingDaysBetween(new DateOnly(2026, 4, 10), service, queryTerritory);

        int expected = expectedExcludesHoliday ? 4 : 5;
        Assert.AreEqual(expected, actual);
    }
}
