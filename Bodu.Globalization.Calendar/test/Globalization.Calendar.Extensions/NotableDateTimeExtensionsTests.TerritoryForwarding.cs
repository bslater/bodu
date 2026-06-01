// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.TerritoryForwarding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateTimeExtensions.IsNonWorkingDay(DateTime, INotableDateService, string?, Type?)" /> forwards
    /// the supplied <c>territoryCode</c> to the service and observes bidirectional territory containment.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(TerritoryForwardingTestData))]
    public void IsNonWorkingDay_WhenTerritoryForwarded_ShouldHonourBidirectionalContainment(string ruleTerritory, string? queryTerritory, bool expected)
    {
        NotableDateService service = BuildHolidayService(ruleTerritory);

        var actual = new DateTime(2026, 4, 7).IsNonWorkingDay(service, territoryCode: queryTerritory);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeExtensions.NextWorkingDay(DateTime, INotableDateService, int, string?, Type?)" />
    /// skips the holiday only when the query territory is in scope; otherwise the holiday is treated as a working day.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(TerritoryForwardingTestData))]
    public void NextWorkingDay_WhenTerritoryForwarded_ShouldHonourBidirectionalContainment(string ruleTerritory, string? queryTerritory, bool expectedSkipsHoliday)
    {
        NotableDateService service = BuildHolidayService(ruleTerritory);

        // 2026-04-06 is Monday. NextWorkingDay should land on Tuesday 2026-04-07 unless a non-working rule applies in scope.
        DateTime actual = new DateTime(2026, 4, 6).NextWorkingDay(service, count: 1, territoryCode: queryTerritory);

        DateTime expected = expectedSkipsHoliday ? new DateTime(2026, 4, 8) : new DateTime(2026, 4, 7);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateTimeExtensions.WorkingDaysBetween(DateTime, DateTime, INotableDateService, string?, Type?)" />
    /// forwards the territory and excludes the holiday only when the query is in scope.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(TerritoryForwardingTestData))]
    public void WorkingDaysBetween_WhenTerritoryForwarded_ShouldHonourBidirectionalContainment(string ruleTerritory, string? queryTerritory, bool expectedExcludesHoliday)
    {
        NotableDateService service = BuildHolidayService(ruleTerritory);

        // 2026-04-06..2026-04-10 (Mon..Fri) is normally five working days; the holiday on Tuesday removes one when in scope.
        var actual = new DateTime(2026, 4, 6).WorkingDaysBetween(new DateTime(2026, 4, 10), service, territoryCode: queryTerritory);

        var expected = expectedExcludesHoliday ? 4 : 5;
        Assert.AreEqual(expected, actual);
    }
}
