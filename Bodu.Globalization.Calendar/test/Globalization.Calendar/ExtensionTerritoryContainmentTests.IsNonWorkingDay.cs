// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionTerritoryContainmentTests.IsNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ExtensionTerritoryContainmentTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.IsNonWorkingDay" /> forwards the query territory and reports
    /// the scoped holiday as non-working only when the rule applies to the query under v2 containment.
    /// </summary>
    /// <param name="ruleTerritory">The territory the rule is scoped to.</param>
    /// <param name="queryTerritory">The territory the query is made for.</param>
    /// <param name="expectedNonWorking">Whether the holiday is expected to apply to the query.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(TerritoryContainmentRows))]
    public void IsNonWorkingDay_WhenTerritoryForwarded_ShouldHonourContainment(string ruleTerritory, string queryTerritory, bool expectedNonWorking)
    {
        INotableDateService service = BuildHolidayService(ruleTerritory);

        bool actual = new DateOnly(2026, 4, 7).IsNonWorkingDay(service, queryTerritory);

        Assert.AreEqual(expectedNonWorking, actual);
    }
}
