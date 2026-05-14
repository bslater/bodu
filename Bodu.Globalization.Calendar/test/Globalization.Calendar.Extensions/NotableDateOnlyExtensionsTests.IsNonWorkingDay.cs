// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.IsNonWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that a weekday with no matching non-working rule is reported as a working day.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenWeekdayWithNoMatchingRule_ShouldReturnFalse()
    {
        NotableDateService service = BuildService();

        bool result = new DateOnly(2026, 1, 6).IsNonWorkingDay(service);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a Saturday is reported as a non-working day.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenSaturday_ShouldReturnTrue()
    {
        NotableDateService service = BuildService();

        bool result = new DateOnly(2026, 1, 3).IsNonWorkingDay(service);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a weekday matching a non-working rule is reported as non-working.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenWeekdayMatchesNonWorkingRule_ShouldReturnTrue()
    {
        NotableDateService service = BuildService(Fixed("New Year's Day", 1, 1, nonWorking: true));

        bool result = new DateOnly(2026, 1, 1).IsNonWorkingDay(service);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that the ambient-service overload routes through <see cref="NotableDateContext.Default" />.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenUsingAmbientService_ShouldDelegateToDefaultContext()
    {
        NotableDateService service = BuildService(Fixed("Holiday", 7, 14, nonWorking: true));
        try
        {
            NotableDateContext.Default = service;

            bool result = new DateOnly(2026, 7, 14).IsNonWorkingDay();

            Assert.IsTrue(result);
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
    public void IsNonWorkingDay_WhenServiceIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DateOnly(2026, 1, 6).IsNonWorkingDay(service: null!);
        });
    }

    /// <summary>
    /// Verifies the non-working classification across every day of a single calendar week under the default Saturday/Sunday weekend
    /// definition.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WorkingDayClassificationTestData))]
    public void IsNonWorkingDay_WhenScannedAcrossWeek_ShouldReturnInverseOfWorkingClassification(DateOnly input, bool expectedWorking)
    {
        NotableDateService service = BuildService();

        bool actual = input.IsNonWorkingDay(service);

        Assert.AreEqual(!expectedWorking, actual);
    }
}
