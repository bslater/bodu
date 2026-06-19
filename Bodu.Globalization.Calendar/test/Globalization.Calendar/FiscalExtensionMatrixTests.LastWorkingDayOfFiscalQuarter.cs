// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalExtensionMatrixTests.LastWorkingDayOfFiscalQuarter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FiscalExtensionMatrixTests
{
    /// <summary>
    /// Verifies the last working day of the fiscal quarter that contains the date across a sweep of start months,
    /// including a weekend end that snaps backward.
    /// </summary>
    /// <param name="year">The input year.</param>
    /// <param name="month">The input month.</param>
    /// <param name="day">The input day.</param>
    /// <param name="startMonth">The fiscal-year start month.</param>
    /// <param name="firstYear">Ignored.</param>
    /// <param name="firstMonth">Ignored.</param>
    /// <param name="firstDay">Ignored.</param>
    /// <param name="lastYear">The expected last-working-day year.</param>
    /// <param name="lastMonth">The expected last-working-day month.</param>
    /// <param name="lastDay">The expected last-working-day day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(FiscalQuarterBoundaryRows))]
    public void LastWorkingDayOfFiscalQuarter_WhenSweepingStartMonths_ShouldReturnExpectedDate(
        int year, int month, int day, int startMonth,
        int firstYear, int firstMonth, int firstDay,
        int lastYear, int lastMonth, int lastDay)
    {
        DateOnly result = new DateOnly(year, month, day).LastWorkingDayOfFiscalQuarter(startMonth, Service, "XX");

        Assert.AreEqual(new DateOnly(lastYear, lastMonth, lastDay), result);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> territory throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void LastWorkingDayOfFiscalQuarter_WhenTerritoryIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2025, 9, 15).LastWorkingDayOfFiscalQuarter(7, Service, null!);
        });
    }
}
