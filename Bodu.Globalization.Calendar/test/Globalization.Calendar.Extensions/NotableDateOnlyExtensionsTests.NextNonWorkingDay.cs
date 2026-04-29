// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.NextNonWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the next non-working day after a Tuesday with no rules lands on the immediately following Saturday.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenWeekdayWithNoRules_ShouldReturnFollowingSaturday()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 6).NextNonWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 10), result);
    }

    /// <summary>
    /// Verifies that a non-working rule on a weekday is preferred over the upcoming weekend when it occurs first.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenWeekdayHolidayPrecedesWeekend_ShouldReturnHoliday()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateOnly result = new DateOnly(2026, 1, 6).NextNonWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 7), result);
    }

    /// <summary>
    /// Verifies that requesting a count greater than one advances through that many non-working days.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenCountIsTwo_ShouldAdvanceTwoNonWorkingDays()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 6).NextNonWorkingDay(service, count: 2);

        Assert.AreEqual(new DateOnly(2026, 1, 11), result);
    }

    /// <summary>
    /// Verifies that requesting a count of zero returns a value equal to the input.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenCountIsZero_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        DateOnly input = new DateOnly(2026, 1, 6);

        DateOnly result = input.NextNonWorkingDay(service, count: 0);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            DateOnly result = new DateOnly(2026, 1, 6).NextNonWorkingDay();

            Assert.AreEqual(new DateOnly(2026, 1, 10), result);
        }
        finally
        {
            NotableDateContext.Reset();
        }
    }

    /// <summary>
    /// Verifies that supplying a <see langword="null" /> service throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).NextNonWorkingDay(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a negative count throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).NextNonWorkingDay(service, count: -1);
        });
    }

    /// <summary>
    /// Verifies that advancing past <see cref="DateOnly.MaxValue" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenAdvancePastMaxValue_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnly.MaxValue.NextNonWorkingDay(service);
        });
    }
}
