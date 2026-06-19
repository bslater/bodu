// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalExtensionMatrixTests.FirstWorkingDayOfFiscalYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FiscalExtensionMatrixTests
{
    /// <summary>
    /// Verifies the first working day of the fiscal year that contains the date across a sweep of start months,
    /// including a weekend start that snaps forward.
    /// </summary>
    /// <param name="year">The input year.</param>
    /// <param name="month">The input month.</param>
    /// <param name="day">The input day.</param>
    /// <param name="startMonth">The fiscal-year start month.</param>
    /// <param name="firstYear">The expected first-working-day year.</param>
    /// <param name="firstMonth">The expected first-working-day month.</param>
    /// <param name="firstDay">The expected first-working-day day.</param>
    /// <param name="lastYear">Ignored; the last-working-day triple is asserted in a sibling test.</param>
    /// <param name="lastMonth">Ignored.</param>
    /// <param name="lastDay">Ignored.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(FiscalYearBoundaryRows))]
    public void FirstWorkingDayOfFiscalYear_WhenSweepingStartMonths_ShouldReturnExpectedDate(
        int year, int month, int day, int startMonth,
        int firstYear, int firstMonth, int firstDay,
        int lastYear, int lastMonth, int lastDay)
    {
        DateOnly result = new DateOnly(year, month, day).FirstWorkingDayOfFiscalYear(startMonth, Service, "XX");

        Assert.AreEqual(new DateOnly(firstYear, firstMonth, firstDay), result);
    }

    /// <summary>
    /// Verifies that a fiscal-year start month above 12 is rejected by the fiscal-year query.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalYear_WhenStartMonthOutOfRange_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateOnly(2025, 9, 15).FirstWorkingDayOfFiscalYear(13, Service, "XX");
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalYear_WhenServiceIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2025, 9, 15).FirstWorkingDayOfFiscalYear(7, null!, "XX");
        });
    }
}
