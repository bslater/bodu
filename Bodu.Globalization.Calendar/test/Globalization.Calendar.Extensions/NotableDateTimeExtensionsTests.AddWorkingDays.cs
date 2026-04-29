// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.AddWorkingDays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that a positive number of days advances forward, equivalent to <see cref="NotableDateTimeExtensions.NextWorkingDay(DateTime, INotableDateService, int, string?, Type?)" />.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenPositiveDays_ShouldAdvanceForward()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 5).AddWorkingDays(service, days: 3);

        Assert.AreEqual(new DateTime(2026, 1, 8), result);
    }

    /// <summary>
    /// Verifies that a negative number of days retreats backward, equivalent to <see cref="NotableDateTimeExtensions.PreviousWorkingDay(DateTime, INotableDateService, int, string?, Type?)" />.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenNegativeDays_ShouldRetreatBackward()
    {
        NotableDateService service = BuildService();

        DateTime result = new DateTime(2026, 1, 8).AddWorkingDays(service, days: -3);

        Assert.AreEqual(new DateTime(2026, 1, 5), result);
    }

    /// <summary>
    /// Verifies that zero days returns the input unchanged regardless of whether it is a working day.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenZeroDays_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService();
        DateTime weekend = new DateTime(2026, 1, 3, 9, 0, 0, DateTimeKind.Utc);

        DateTime result = weekend.AddWorkingDays(service, days: 0);

        Assert.AreEqual(weekend, result);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
    }

    /// <summary>
    /// Verifies that zero days returns the input unchanged even when the input matches a non-working rule.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenZeroDaysOnNonWorkingRuleDay_ShouldReturnInputUnchanged()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 1, 1, nonWorking: true));
        DateTime input = new DateTime(2026, 1, 1);

        DateTime result = input.AddWorkingDays(service, days: 0);

        Assert.AreEqual(input, result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService();
        try
        {
            NotableDateContext.Default = service;

            DateTime result = new DateTime(2026, 1, 5).AddWorkingDays(2);

            Assert.AreEqual(new DateTime(2026, 1, 7), result);
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
    public void AddWorkingDays_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateTime(2026, 1, 6).AddWorkingDays(service: null!, days: 1);
        });
    }
}
