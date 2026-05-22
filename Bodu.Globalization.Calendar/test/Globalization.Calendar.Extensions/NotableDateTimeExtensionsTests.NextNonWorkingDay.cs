// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.NextNonWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the next non-working day after a Tuesday with no rules lands on the immediately following Saturday.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenWeekdayWithNoRules_ShouldReturnFollowingSaturday()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 6).NextNonWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 10), result);
    }

    /// <summary>
    /// Verifies that a non-working rule on a weekday is preferred over the upcoming weekend when it occurs first.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenWeekdayHolidayPrecedesWeekend_ShouldReturnHoliday()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateTime result = new DateTime(2026, 1, 6).NextNonWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 7), result);
    }

    /// <summary>
    /// Verifies that requesting a count greater than one advances through that many non-working days.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenCountIsTwo_ShouldAdvanceTwoNonWorkingDays()
    {
        NotableDateService service = BuildService();

        // From Tuesday 2026-01-06 the next non-working days are Saturday 10, Sunday 11.
        DateTime result = new DateTime(2026, 1, 6).NextNonWorkingDay(service, count: 2);

        Assert.AreEqual(new DateTime(2026, 1, 11), result);
    }

    /// <summary>
    /// Verifies that requesting a count of zero returns a fresh <see cref="DateTime" /> equal to the input.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenCountIsZero_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        var input = new DateTime(2026, 1, 6, 8, 0, 0, DateTimeKind.Utc);

        DateTime result = input.NextNonWorkingDay(service, count: 0);

        Assert.AreEqual(input, result);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
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

            DateTime result = new DateTime(2026, 1, 6).NextNonWorkingDay();

            Assert.AreEqual(new DateTime(2026, 1, 10), result);
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
    public void NextNonWorkingDay_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 6).NextNonWorkingDay(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a negative count throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenCountIsNegative_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateTime(2026, 1, 6).NextNonWorkingDay(service, count: -1);
        });
    }

    /// <summary>
    /// Verifies that advancing past <see cref="DateTime.MaxValue" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextNonWorkingDay_WhenAdvancePastMaxValue_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTime.MaxValue.NextNonWorkingDay(service);
        });
    }

    /// <summary>
    /// Verifies that the returned <see cref="DateTime" /> preserves the input <see cref="DateTime.Kind" /> and time-of-day across each
    /// supported <see cref="DateTimeKind" /> value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeKindPreservationTestData))]
    public void NextNonWorkingDay_WhenCalled_ShouldPreserveKindAndTimeOfDay(DateTimeKind kind)
    {
        NotableDateService service = BuildService();
        var input = new DateTime(2026, 1, 6, 11, 22, 33, kind);

        DateTime result = input.NextNonWorkingDay(service);

        Assert.AreEqual(kind, result.Kind);
        Assert.AreEqual(new TimeSpan(11, 22, 33), result.TimeOfDay);
    }
}
