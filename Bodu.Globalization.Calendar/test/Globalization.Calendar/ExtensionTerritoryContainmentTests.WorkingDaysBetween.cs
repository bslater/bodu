// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionTerritoryContainmentTests.WorkingDaysBetween.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ExtensionTerritoryContainmentTests
{
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
    [DynamicData(nameof(TerritoryContainmentRows))]
    public void WorkingDaysBetween_WhenTerritoryForwarded_ShouldHonourContainment(string ruleTerritory, string queryTerritory, bool expectedExcludesHoliday)
    {
        INotableDateService service = BuildHolidayService(ruleTerritory);

        int actual = new DateOnly(2026, 4, 6).WorkingDaysBetween(new DateOnly(2026, 4, 10), service, queryTerritory);

        int expected = expectedExcludesHoliday ? 4 : 5;
        Assert.AreEqual(expected, actual);
    }
}
