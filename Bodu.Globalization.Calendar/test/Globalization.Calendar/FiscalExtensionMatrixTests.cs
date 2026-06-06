// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalExtensionMatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Ports the v1 fiscal boundary working-day coverage to the v2 explicit-service surface, sweeping the configurable
/// fiscal-year start month and verifying the first and last working day of the fiscal year and the fiscal quarter that
/// contains a date, including the weekend-snap behaviour at a boundary and the start-month range guard.
/// </summary>
/// <remarks>
/// <para>
/// The v2 fiscal API differs from v1: it takes a fiscal-year start month (1 to 12) and derives the quarter from the
/// date, rather than v1's <c>FiscalWeekQuarterProvider</c> plus an explicit quarter index (1 to 4). The v1
/// quarter-zero guard is therefore not portable; the equivalent v2 guard rejects a start month outside 1 to 12.
/// </para>
/// <para>
/// The fixture declares no notable dates, so working days are governed only by the default Monday-to-Friday working
/// week. Expected boundary dates were derived from the 2025/2026 calendar layout (for example a June fiscal year starts
/// Sunday 1 June 2025, snapping its first working day forward to Monday 2 June, and ends Sunday 31 May 2026, snapping
/// its last working day backward to Friday 29 May).
/// </para>
/// </remarks>
[TestClass]
public sealed class FiscalExtensionMatrixTests
{
    /// <summary>
    /// A resource declaring no notable dates, so working days are governed only by the working week.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.fiscal-matrix">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
    </NotableDateResource>
    """;

    /// <summary>
    /// Gets a service over the fixture.
    /// </summary>
    private static INotableDateService Service =>
        new NotableDateService(NotableDateResourceLoader.Load(Xml));

    /// <summary>
    /// Provides fiscal-year boundary cases as <c>(date, startMonth)</c> with the expected first and last working day.
    /// </summary>
    /// <returns>
    /// A sequence of <c>(int y, int m, int d, int startMonth, int fy, int fm, int fd, int ly, int lm, int ld)</c> rows,
    /// where the first triple is the input date, then the first-working-day triple, then the last-working-day triple.
    /// </returns>
    public static IEnumerable<object[]> FiscalYearBoundaryRows()
    {
        // January (calendar) fiscal year containing 2025-09-15: 2025-01-01 (Wed) .. 2025-12-31 (Wed).
        yield return new object[] { 2025, 9, 15, 1, 2025, 1, 1, 2025, 12, 31 };

        // July fiscal year containing 2025-09-15: 2025-07-01 (Tue) .. 2026-06-30 (Tue).
        yield return new object[] { 2025, 9, 15, 7, 2025, 7, 1, 2026, 6, 30 };

        // April fiscal year containing 2025-09-15: 2025-04-01 (Tue) .. 2026-03-31 (Tue).
        yield return new object[] { 2025, 9, 15, 4, 2025, 4, 1, 2026, 3, 31 };

        // October fiscal year containing 2026-01-15: 2025-10-01 (Wed) .. 2026-09-30 (Wed).
        yield return new object[] { 2026, 1, 15, 10, 2025, 10, 1, 2026, 9, 30 };

        // June fiscal year containing 2025-08-15: starts Sun 2025-06-01 -> Mon 2025-06-02; ends Sun 2026-05-31 -> Fri 2026-05-29.
        yield return new object[] { 2025, 8, 15, 6, 2025, 6, 2, 2026, 5, 29 };
    }

    /// <summary>
    /// Provides fiscal-quarter boundary cases as <c>(date, startMonth)</c> with the expected first and last working day
    /// of the quarter that contains the date.
    /// </summary>
    /// <returns>
    /// A sequence of <c>(int y, int m, int d, int startMonth, int fy, int fm, int fd, int ly, int lm, int ld)</c> rows.
    /// </returns>
    public static IEnumerable<object[]> FiscalQuarterBoundaryRows()
    {
        // July fiscal year, 2025-08-15 -> Q1 (Jul-Sep): 2025-07-01 (Tue) .. 2025-09-30 (Tue).
        yield return new object[] { 2025, 8, 15, 7, 2025, 7, 1, 2025, 9, 30 };

        // January (calendar) fiscal year, 2025-09-15 -> Q3 (Jul-Sep): 2025-07-01 (Tue) .. 2025-09-30 (Tue).
        yield return new object[] { 2025, 9, 15, 1, 2025, 7, 1, 2025, 9, 30 };

        // October fiscal year, 2026-01-15 -> Q2 (Jan-Mar): 2026-01-01 (Thu) .. 2026-03-31 (Tue).
        yield return new object[] { 2026, 1, 15, 10, 2026, 1, 1, 2026, 3, 31 };

        // June fiscal year, 2025-08-15 -> Q1 (Jun-Aug): starts Sun 2025-06-01 -> Mon 2025-06-02; ends Sun 2025-08-31 -> Fri 2025-08-29.
        yield return new object[] { 2025, 8, 15, 6, 2025, 6, 2, 2025, 8, 29 };
    }

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
    /// Verifies the last working day of the fiscal year that contains the date across a sweep of start months, including
    /// a weekend end that snaps backward.
    /// </summary>
    /// <param name="year">The input year.</param>
    /// <param name="month">The input month.</param>
    /// <param name="day">The input day.</param>
    /// <param name="startMonth">The fiscal-year start month.</param>
    /// <param name="firstYear">Ignored; the first-working-day triple is asserted in a sibling test.</param>
    /// <param name="firstMonth">Ignored.</param>
    /// <param name="firstDay">Ignored.</param>
    /// <param name="lastYear">The expected last-working-day year.</param>
    /// <param name="lastMonth">The expected last-working-day month.</param>
    /// <param name="lastDay">The expected last-working-day day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(FiscalYearBoundaryRows))]
    public void LastWorkingDayOfFiscalYear_WhenSweepingStartMonths_ShouldReturnExpectedDate(
        int year, int month, int day, int startMonth,
        int firstYear, int firstMonth, int firstDay,
        int lastYear, int lastMonth, int lastDay)
    {
        DateOnly result = new DateOnly(year, month, day).LastWorkingDayOfFiscalYear(startMonth, Service, "XX");

        Assert.AreEqual(new DateOnly(lastYear, lastMonth, lastDay), result);
    }

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
