// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.TerritoryForwarding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.IsNonWorkingDay(DateOnly, INotableDateService, string?, Type?)" /> forwards
    /// the supplied <c>territoryCode</c> to the service and observes bidirectional territory containment.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(TerritoryForwardingTestData), DynamicDataSourceType.Method)]
    public void IsNonWorkingDay_WhenTerritoryForwarded_ShouldHonourBidirectionalContainment(string ruleTerritory, string? queryTerritory, bool expected)
    {
        NotableDateService service = BuildHolidayService(ruleTerritory);

        bool actual = new DateOnly(2026, 4, 7).IsNonWorkingDay(service, territoryCode: queryTerritory);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.NextWorkingDay(DateOnly, INotableDateService, int, string?, Type?)" />
    /// skips the holiday only when the query territory is in scope; otherwise the holiday is treated as a working day.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(TerritoryForwardingTestData), DynamicDataSourceType.Method)]
    public void NextWorkingDay_WhenTerritoryForwarded_ShouldHonourBidirectionalContainment(string ruleTerritory, string? queryTerritory, bool expectedSkipsHoliday)
    {
        NotableDateService service = BuildHolidayService(ruleTerritory);

        DateOnly actual = new DateOnly(2026, 4, 6).NextWorkingDay(service, count: 1, territoryCode: queryTerritory);

        DateOnly expected = expectedSkipsHoliday ? new DateOnly(2026, 4, 8) : new DateOnly(2026, 4, 7);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.WorkingDaysBetween(DateOnly, DateOnly, INotableDateService, string?, Type?)" />
    /// forwards the territory and excludes the holiday only when the query is in scope.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(TerritoryForwardingTestData), DynamicDataSourceType.Method)]
    public void WorkingDaysBetween_WhenTerritoryForwarded_ShouldHonourBidirectionalContainment(string ruleTerritory, string? queryTerritory, bool expectedExcludesHoliday)
    {
        NotableDateService service = BuildHolidayService(ruleTerritory);

        int actual = new DateOnly(2026, 4, 6).WorkingDaysBetween(new DateOnly(2026, 4, 10), service, territoryCode: queryTerritory);

        int expected = expectedExcludesHoliday ? 4 : 5;
        Assert.AreEqual(expected, actual);
    }
}
