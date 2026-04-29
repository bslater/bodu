// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.PreviousWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the previous working day before a Monday skips back across the Saturday/Sunday weekend to the prior Friday.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenMondayWithNoRules_ShouldReturnPreviousFriday()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 5).PreviousWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 2), result);
    }

    /// <summary>
    /// Verifies that the previous working day before a Tuesday with no rules is the prior Monday.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenWeekday_ShouldReturnPreviousDay()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 6).PreviousWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that an intermediate non-working rule on the previous weekday is skipped when locating the previous working day.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenPreviousWeekdayIsNonWorking_ShouldSkipToEarlierWorkingDay()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 5, nonWorking: true));

        DateTime result = new DateTime(2026, 1, 6).PreviousWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 2), result);
    }

    /// <summary>
    /// Verifies that requesting a count greater than one retreats through that many working days.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenCountIsThree_ShouldRetreatThreeWorkingDays()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 8).PreviousWorkingDay(service, count: 3);

        Assert.AreEqual(new DateTime(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that requesting a count of zero returns a fresh <see cref="DateTime" /> equal to the input.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenCountIsZero_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        DateTime input = new DateTime(2026, 1, 3, 14, 30, 0, DateTimeKind.Utc);

        DateTime result = input.PreviousWorkingDay(service, count: 0);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that the returned <see cref="DateTime" /> preserves the input <see cref="DateTime.Kind" /> and time-of-day.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenCalled_ShouldPreserveKindAndTimeOfDay()
    {
        NotableDateService service = BuildService();
        DateTime input = new DateTime(2026, 1, 6, 9, 15, 30, DateTimeKind.Local);

        DateTime result = input.PreviousWorkingDay(service);

        Assert.AreEqual(DateTimeKind.Local, result.Kind);
        Assert.AreEqual(new TimeSpan(9, 15, 30), result.TimeOfDay);
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

            DateTime result = new DateTime(2026, 1, 5).PreviousWorkingDay();

            Assert.AreEqual(new DateTime(2026, 1, 2), result);
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
            _ = new DateTime(2026, 1, 6).PreviousWorkingDay(service: null!);
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
            _ = new DateTime(2026, 1, 6).PreviousWorkingDay(service, count: -1);
        });
    }

    /// <summary>
    /// Verifies that retreating past <see cref="DateTime.MinValue" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenRetreatPastMinValue_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTime.MinValue.PreviousWorkingDay(service);
        });
    }
}
