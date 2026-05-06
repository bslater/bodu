// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.PreviousWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the previous working day before a Monday skips back across the Saturday/Sunday weekend to the prior Friday.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenMondayWithNoRules_ShouldReturnPreviousFriday()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 5).PreviousWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 2), result);
    }

    /// <summary>
    /// Verifies that an intermediate non-working rule is skipped when locating the previous working day.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenPreviousWeekdayIsNonWorking_ShouldSkipToEarlierWorkingDay()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 5, nonWorking: true));

        DateOnly result = new DateOnly(2026, 1, 6).PreviousWorkingDay(service);

        Assert.AreEqual(new DateOnly(2026, 1, 2), result);
    }

    /// <summary>
    /// Verifies that requesting a count greater than one retreats through that many working days.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenCountIsThree_ShouldRetreatThreeWorkingDays()
    {
        NotableDateService service = BuildService();

        DateOnly result = new DateOnly(2026, 1, 8).PreviousWorkingDay(service, count: 3);

        Assert.AreEqual(new DateOnly(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that requesting a count of zero returns a value equal to the input.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenCountIsZero_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        DateOnly input = new DateOnly(2026, 1, 3);

        DateOnly result = input.PreviousWorkingDay(service, count: 0);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            DateOnly result = new DateOnly(2026, 1, 5).PreviousWorkingDay();

            Assert.AreEqual(new DateOnly(2026, 1, 2), result);
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
    public void PreviousWorkingDay_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).PreviousWorkingDay(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a negative count throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).PreviousWorkingDay(service, count: -1);
        });
    }

    /// <summary>
    /// Verifies that retreating past <see cref="DateOnly.MinValue" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenRetreatPastMinValue_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnly.MinValue.PreviousWorkingDay(service);
        });
    }

    /// <summary>
    /// Verifies the result of retreating by the supplied <c>count</c> across a representative spread of single-step, multi-step,
    /// weekend-bridging and weekend-input cases using an empty rule set.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PreviousWorkingDayCountTestData))]
    public void PreviousWorkingDay_WhenRetreatingCount_ShouldReturnExpectedDate(DateOnly start, int count, DateOnly expected)
    {
        NotableDateService service = BuildService();

        DateOnly actual = start.PreviousWorkingDay(service, count: count);

        Assert.AreEqual(expected, actual);
    }
}
