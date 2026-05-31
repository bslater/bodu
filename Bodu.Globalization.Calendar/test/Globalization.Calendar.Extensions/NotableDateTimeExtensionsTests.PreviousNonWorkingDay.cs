// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.PreviousNonWorkingDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that the previous non-working day before a Tuesday with no rules is the immediately preceding Sunday.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenWeekdayWithNoRules_ShouldReturnPriorSunday()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 6).PreviousNonWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 4), result);
    }

    /// <summary>
    /// Verifies that a non-working rule on a weekday is found when it lies between the input and the prior weekend.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenWeekdayHolidayPrecedesInput_ShouldReturnHoliday()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 8, nonWorking: true));

        DateTime result = new DateTime(2026, 1, 9).PreviousNonWorkingDay(service);

        Assert.AreEqual(new DateTime(2026, 1, 8), result);
    }

    /// <summary>
    /// Verifies that requesting a count greater than one retreats through that many non-working days.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenCountIsTwo_ShouldRetreatTwoNonWorkingDays()
    {
        NotableDateService service = BuildService();

        // From Tuesday 2026-01-13 the previous non-working days are Sunday 11, Saturday 10.
        DateTime result = new DateTime(2026, 1, 13).PreviousNonWorkingDay(service, count: 2);

        Assert.AreEqual(new DateTime(2026, 1, 10), result);
    }

    /// <summary>
    /// Verifies that requesting a count of zero returns a fresh <see cref="DateTime" /> equal to the input.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenCountIsZero_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        var input = new DateTime(2026, 1, 6, 8, 0, 0, DateTimeKind.Utc);

        DateTime result = input.PreviousNonWorkingDay(service, count: 0);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            DateTime result = new DateTime(2026, 1, 6).PreviousNonWorkingDay();

            Assert.AreEqual(new DateTime(2026, 1, 4), result);
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
    public void PreviousNonWorkingDay_WhenServiceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 6).PreviousNonWorkingDay(service: null!);
        });
    }

    /// <summary>
    /// Verifies that supplying a negative count throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenCountIsNegative_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateTime(2026, 1, 6).PreviousNonWorkingDay(service, count: -1);
        });
    }

    /// <summary>
    /// Verifies that retreating past <see cref="DateTime.MinValue" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousNonWorkingDay_WhenRetreatPastMinValue_ShouldThrowExactly()
    {
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTime.MinValue.PreviousNonWorkingDay(service);
        });
    }

    /// <summary>
    /// Verifies that the returned <see cref="DateTime" /> preserves the input <see cref="DateTime.Kind" /> and time-of-day across each
    /// supported <see cref="DateTimeKind" /> value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeKindPreservationTestData))]
    public void PreviousNonWorkingDay_WhenCalled_ShouldPreserveKindAndTimeOfDay(DateTimeKind kind)
    {
        NotableDateService service = BuildService();
        var input = new DateTime(2026, 1, 6, 11, 22, 33, kind);

        DateTime result = input.PreviousNonWorkingDay(service);

        Assert.AreEqual(kind, result.Kind);
        Assert.AreEqual(new TimeSpan(11, 22, 33), result.TimeOfDay);
    }
}
