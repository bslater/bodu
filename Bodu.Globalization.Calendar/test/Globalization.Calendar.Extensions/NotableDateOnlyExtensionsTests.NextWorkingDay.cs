// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.NextWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the next working day after a Friday skips the Saturday/Sunday weekend and lands on the following Monday.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenFridayWithNoRules_ShouldReturnFollowingMonday()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 2).NextWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that an intermediate non-working rule is skipped when locating the next working day.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenNextWeekdayIsNonWorking_ShouldSkipToFollowingWorkingDay()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 7, nonWorking: true));

        DateOnly result = new DateOnly(2026, 1, 6).NextWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 8), result);
    }

    /// <summary>
    /// Verifies that requesting a count greater than one advances through that many working days.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenCountIsThree_ShouldAdvanceThreeWorkingDays()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 5).NextWorkingDay(service, count: 3);

        Assert.AreEqual(new DateOnly(2026, 1, 8), result);
    }

    /// <summary>
    /// Verifies that requesting a count of zero returns a value equal to the input.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenCountIsZero_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        var input = new DateOnly(2026, 1, 3);

        DateOnly result = input.NextWorkingDay(service, count: 0);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            DateOnly result = new DateOnly(2026, 1, 2).NextWorkingDay();

            Assert.AreEqual(new DateOnly(2026, 1, 5), result);
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
            _ = new DateOnly(2026, 1, 6).NextWorkingDay(service: null!);
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
            _ = new DateOnly(2026, 1, 6).NextWorkingDay(service, count: -1);
        });
    }

    /// <summary>
    /// Verifies that advancing past <see cref="DateOnly.MaxValue" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenAdvancePastMaxValue_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnly.MaxValue.NextWorkingDay(service);
        });
    }

    /// <summary>
    /// Verifies the result of advancing by the supplied <c>count</c> across a representative spread of single-step, multi-step,
    /// weekend-bridging and weekend-input cases using an empty rule set.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NextWorkingDayCountTestData))]
    public void NextWorkingDay_WhenAdvancingCount_ShouldReturnExpectedDate(DateOnly start, int count, DateOnly expected)
    {
        NotableDateService service = BuildService();

        DateOnly actual = start.NextWorkingDay(service, count: count);

        Assert.AreEqual(expected, actual);
    }
}
