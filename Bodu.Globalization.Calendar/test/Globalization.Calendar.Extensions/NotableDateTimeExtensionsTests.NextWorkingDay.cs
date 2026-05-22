// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.NextWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the next working day after a Friday skips the Saturday/Sunday weekend and lands on the following Monday.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenFridayWithNoRules_ShouldReturnFollowingMonday()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 2).NextWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that the next working day after a Tuesday with no rules is the following Wednesday.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenWeekday_ShouldReturnNextDay()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 6).NextWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 7), result);
    }

    /// <summary>
    /// Verifies that an intermediate non-working rule on the next weekday is skipped when locating the next working day.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenNextWeekdayIsNonWorking_ShouldSkipToFollowingWorkingDay()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateTime result = new DateTime(2026, 1, 6).NextWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 8), result);
    }

    /// <summary>
    /// Verifies that requesting a count greater than one advances through that many working days.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenCountIsThree_ShouldAdvanceThreeWorkingDays()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 5).NextWorkingDay(service, count: 3);

        Assert.AreEqual(new DateTime(2026, 1, 8), result);
    }

    /// <summary>
    /// Verifies that requesting a count of zero returns a fresh <see cref="DateTime" /> equal to the input.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenCountIsZero_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        var input = new DateTime(2026, 1, 3, 14, 30, 0, DateTimeKind.Utc);

        DateTime result = input.NextWorkingDay(service, count: 0);

        Assert.AreEqual(input, result);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
    }

    /// <summary>
    /// Verifies that the returned <see cref="DateTime" /> preserves the input <see cref="DateTime.Kind" /> and time-of-day.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenCalled_ShouldPreserveKindAndTimeOfDay()
    {
        NotableDateService service = BuildService();
        var input = new DateTime(2026, 1, 6, 9, 15, 30, DateTimeKind.Utc);

        DateTime result = input.NextWorkingDay(service);

        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        Assert.AreEqual(new TimeSpan(9, 15, 30), result.TimeOfDay);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 3, 4, nonWorking: true));
        try
        {
            NotableDateContext.Default = service;

            DateTime result = new DateTime(2026, 3, 3).NextWorkingDay();

            Assert.AreEqual(new DateTime(2026, 3, 5), result);
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
    public void NextWorkingDay_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 6).NextWorkingDay(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a negative count throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenCountIsNegative_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateTime(2026, 1, 6).NextWorkingDay(service, count: -1);
        });
    }

    /// <summary>
    /// Verifies that advancing past <see cref="DateTime.MaxValue" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenAdvancePastMaxValue_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTime.MaxValue.NextWorkingDay(service);
        });
    }

    /// <summary>
    /// Verifies the result of advancing by the supplied <c>count</c> across a representative spread of single-step, multi-step,
    /// weekend-bridging and weekend-input cases using an empty rule set.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NextWorkingDayCountTestData))]
    public void NextWorkingDay_WhenAdvancingCount_ShouldReturnExpectedDate(DateTime start, int count, DateTime expected)
    {
        NotableDateService service = BuildService();

        DateTime actual = start.NextWorkingDay(service, count: count);

        Assert.AreEqual(expected, actual);
    }
}
