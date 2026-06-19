// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionTerritoryContainmentTests.NextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ExtensionTerritoryContainmentTests
{
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
    [DynamicData(nameof(TerritoryContainmentRows))]
    public void NextWorkingDay_WhenTerritoryForwarded_ShouldHonourContainment(string ruleTerritory, string queryTerritory, bool expectedSkipsHoliday)
    {
        INotableDateService service = BuildHolidayService(ruleTerritory);

        DateOnly actual = new DateOnly(2026, 4, 6).NextWorkingDay(service, queryTerritory);

        DateOnly expected = expectedSkipsHoliday ? new DateOnly(2026, 4, 8) : new DateOnly(2026, 4, 7);
        Assert.AreEqual(expected, actual);
    }
}
