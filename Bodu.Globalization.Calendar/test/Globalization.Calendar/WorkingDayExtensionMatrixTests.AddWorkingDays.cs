// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayExtensionMatrixTests.AddWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class WorkingDayExtensionMatrixTests
{
    /// <summary>
    /// Verifies that signed-day arithmetic produces the expected working day across positive, negative, zero and
    /// cross-week values.
    /// </summary>
    /// <param name="year">The input year.</param>
    /// <param name="month">The input month.</param>
    /// <param name="day">The input day.</param>
    /// <param name="days">The signed number of working days to add.</param>
    /// <param name="expectedYear">The expected result year.</param>
    /// <param name="expectedMonth">The expected result month.</param>
    /// <param name="expectedDay">The expected result day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(AddWorkingDaysSignedRows))]
    public void AddWorkingDays_WhenSignedDaysSupplied_ShouldReturnExpectedWorkingDay(int year, int month, int day, int days, int expectedYear, int expectedMonth, int expectedDay)
    {
        DateOnly actual = new DateOnly(year, month, day).AddWorkingDays(days, Service, "XX");

        Assert.AreEqual(new DateOnly(expectedYear, expectedMonth, expectedDay), actual);
    }

    /// <summary>
    /// Verifies that adding zero working days returns the input unchanged even when the input is a non-working holiday.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenZeroDaysOnHoliday_ShouldReturnInputUnchanged()
    {
        DateOnly holiday = new(2026, 1, 1);

        Assert.AreEqual(holiday, holiday.AddWorkingDays(0, Service, "XX"));
    }

    /// <summary>
    /// Verifies that adding working days forward skips the fixture holiday and the weekend. From Wednesday 31 December 2025,
    /// three working days forward are 2 January (Friday), 5 January (Monday) and 6 January (Tuesday).
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenCrossingHolidayAndWeekendForward_ShouldSkipNonWorkingDays()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 6), new DateOnly(2025, 12, 31).AddWorkingDays(3, Service, "XX"));
    }

    /// <summary>
    /// Verifies that subtracting working days skips the fixture holiday and the weekend. From Tuesday 6 January 2026, three
    /// working days back are 5 January (Monday), 2 January (Friday) and 31 December 2025 (Wednesday).
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenCrossingHolidayAndWeekendBackward_ShouldSkipNonWorkingDays()
    {
        Assert.AreEqual(new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 6).AddWorkingDays(-3, Service, "XX"));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> service throws <see cref="ArgumentNullException" /> from
    /// <see cref="NotableDateOnlyExtensions.AddWorkingDays" />.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenServiceIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).AddWorkingDays(1, null!, "XX");
        });
    }
}
