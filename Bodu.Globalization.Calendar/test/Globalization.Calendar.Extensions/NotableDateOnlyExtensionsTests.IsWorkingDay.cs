// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.IsWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that a regular Tuesday with no matching rule is reported as a working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenWeekdayWithNoMatchingRule_ShouldReturnTrue()
    {
        NotableDateService service = BuildService();

        bool result = new DateOnly(2026, 1, 6).IsWorkingDay(service);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a Saturday is reported as a non-working day under the default Saturday/Sunday weekend definition.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenSaturday_ShouldReturnFalse()
    {
        NotableDateService service = BuildService();

        bool result = new DateOnly(2026, 1, 3).IsWorkingDay(service);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a weekday matching a non-working rule is reported as a non-working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenWeekdayMatchesNonWorkingRule_ShouldReturnFalse()
    {
        NotableDateService service = BuildService(Fixed("New Year's Day", 1, 1, nonWorking: true));

        bool result = new DateOnly(2026, 1, 1).IsWorkingDay(service);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 5, 4, nonWorking: true));
        try
        {
            NotableDateContext.Default = service;

            bool result = new DateOnly(2026, 5, 4).IsWorkingDay();

            Assert.IsFalse(result);
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
    public void IsWorkingDay_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).IsWorkingDay(service: null!);
        });
    }
}
