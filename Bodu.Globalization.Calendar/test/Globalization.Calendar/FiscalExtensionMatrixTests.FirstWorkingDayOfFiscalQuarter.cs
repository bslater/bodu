// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalExtensionMatrixTests.FirstWorkingDayOfFiscalQuarter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FiscalExtensionMatrixTests
{
    /// <summary>
    /// Verifies the first working day of the fiscal quarter that contains the date across a sweep of start months,
    /// including a weekend start that snaps forward.
    /// </summary>
    /// <param name="year">The input year.</param>
    /// <param name="month">The input month.</param>
    /// <param name="day">The input day.</param>
    /// <param name="startMonth">The fiscal-year start month.</param>
    /// <param name="firstYear">The expected first-working-day year.</param>
    /// <param name="firstMonth">The expected first-working-day month.</param>
    /// <param name="firstDay">The expected first-working-day day.</param>
    /// <param name="lastYear">Ignored.</param>
    /// <param name="lastMonth">Ignored.</param>
    /// <param name="lastDay">Ignored.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(FiscalQuarterBoundaryRows))]
    public void FirstWorkingDayOfFiscalQuarter_WhenSweepingStartMonths_ShouldReturnExpectedDate(
        int year, int month, int day, int startMonth,
        int firstYear, int firstMonth, int firstDay,
        int lastYear, int lastMonth, int lastDay)
    {
        DateOnly result = new DateOnly(year, month, day).FirstWorkingDayOfFiscalQuarter(startMonth, Service, "XX");

        Assert.AreEqual(new DateOnly(firstYear, firstMonth, firstDay), result);
    }

    /// <summary>
    /// Verifies that a fiscal-year start month below 1 is rejected. This is the v2 equivalent of the v1 quarter-zero
    /// guard, which has no direct port because v2 has no explicit quarter index.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalQuarter_WhenStartMonthIsZero_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateOnly(2025, 9, 15).FirstWorkingDayOfFiscalQuarter(0, Service, "XX");
        });
    }
}
