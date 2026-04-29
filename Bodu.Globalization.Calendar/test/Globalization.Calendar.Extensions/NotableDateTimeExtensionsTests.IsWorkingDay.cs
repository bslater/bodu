// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.IsWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that a regular Tuesday with no matching rule is reported as a working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenWeekdayWithNoMatchingRule_ShouldReturnTrue()
    {
        NotableDateService service = BuildService();

        bool result = new DateTime(2026, 1, 6).IsWorkingDay(service);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a Saturday is reported as a non-working day under the default Saturday/Sunday weekend definition.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenSaturday_ShouldReturnFalse()
    {
        NotableDateService service = BuildService();

        bool result = new DateTime(2026, 1, 3).IsWorkingDay(service);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a Sunday is reported as a non-working day under the default Saturday/Sunday weekend definition.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenSunday_ShouldReturnFalse()
    {
        NotableDateService service = BuildService();

        bool result = new DateTime(2026, 1, 4).IsWorkingDay(service);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a weekday matching a non-working rule is reported as a non-working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenWeekdayMatchesNonWorkingRule_ShouldReturnFalse()
    {
        NotableDateService service = BuildService(Fixed("New Year's Day", 1, 1, nonWorking: true));

        bool result = new DateTime(2026, 1, 1).IsWorkingDay(service);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a weekday matching a notable but working rule (e.g. a cultural observance) remains a working day.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenWeekdayMatchesWorkingRule_ShouldReturnTrue()
    {
        NotableDateService service = BuildService(Fixed("Cultural Day", 6, 5, NotableDateCategory.Cultural, nonWorking: false));

        bool result = new DateTime(2026, 6, 5).IsWorkingDay(service);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that the territory-aware overload only applies a non-working rule when the supplied territory matches.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenRuleScopedToOtherTerritory_ShouldReturnTrue()
    {
        NotableDateService service = BuildService(Fixed("Anzac Day", 4, 25, nonWorking: true, territory: "AU"));

        bool result = new DateTime(2026, 4, 27).IsWorkingDay(service, territoryCode: "NZ");

        Assert.IsTrue(result);
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

            bool result = new DateTime(2026, 5, 4).IsWorkingDay();

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
            _ = new DateTime(2026, 1, 6).IsWorkingDay(service: null!);
        });
    }

    /// <summary>
    /// Verifies the working-day classification across every day of a single calendar week under the default Saturday/Sunday weekend
    /// definition.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WorkingDayClassificationTestData), DynamicDataSourceType.Method)]
    public void IsWorkingDay_WhenScannedAcrossWeek_ShouldReturnExpectedClassification(DateTime input, bool expected)
    {
        NotableDateService service = BuildService();

        bool actual = input.IsWorkingDay(service);

        Assert.AreEqual(expected, actual);
    }
}
