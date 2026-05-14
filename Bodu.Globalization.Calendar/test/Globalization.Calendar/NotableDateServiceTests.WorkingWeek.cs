// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.WorkingWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that the parameterless <see cref="NotableDateService" /> constructor surfaces
    /// <see cref="WeekPattern.Weekdays" /> as its <see cref="INotableDateService.WorkingWeek" />.
    /// </summary>
    [TestMethod]
    public void WorkingWeek_WhenDefaultService_ShouldExposeWeekdays()
    {
        INotableDateService service = new NotableDateService();

        Assert.AreEqual(WeekPattern.Weekdays, service.WorkingWeek);
    }

    /// <summary>
    /// Verifies that a <see cref="NotableDateService" /> constructed with an explicit
    /// <see cref="WeekPattern" /> surfaces that pattern via <see cref="INotableDateService.WorkingWeek" />.
    /// </summary>
    [TestMethod]
    public void WorkingWeek_WhenConstructedWithWeekPattern_ShouldExposeSamePattern()
    {
        INotableDateService service = new NotableDateService(WeekPattern.SundayToThursday);

        Assert.AreEqual(WeekPattern.SundayToThursday, service.WorkingWeek);
    }

    /// <summary>
    /// Verifies that a <see cref="NotableDateService" /> constructed with a
    /// <see cref="WorkingDaysOfWeek" /> preset surfaces the equivalent <see cref="WeekPattern" />.
    /// </summary>
    [TestMethod]
    public void WorkingWeek_WhenConstructedWithWorkingDaysOfWeek_ShouldExposeMatchingPattern()
    {
        INotableDateService service = new NotableDateService(WorkingDaysOfWeek.SundayToThursday);

        Assert.AreEqual(WeekPattern.SundayToThursday, service.WorkingWeek);
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.IsNonWorkingDay" /> treats Friday and Saturday as non-working when
    /// the configured working week is Sunday through Thursday.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenSundayToThursdayWorkingWeek_ShouldTreatFridayAsNonWorking()
    {
        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider() },
            WorkingDaysOfWeek.SundayToThursday);

        // 2026-05-15 is a Friday; 2026-05-16 is a Saturday; 2026-05-17 is a Sunday.
        Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2026, 5, 15)), "Friday must be non-working under Sun-Thu working week.");
        Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2026, 5, 16)), "Saturday must be non-working under Sun-Thu working week.");
        Assert.IsFalse(service.IsNonWorkingDay(new DateTime(2026, 5, 17)), "Sunday must be working under Sun-Thu working week.");
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.IsWeekend" /> returns the complement of the configured
    /// <see cref="WorkingWeek" />.
    /// </summary>
    [TestMethod]
    public void IsWeekend_WhenSundayToThursdayWorkingWeek_ShouldReturnTrueForFridayAndSaturday()
    {
        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider() },
            WorkingDaysOfWeek.SundayToThursday);

        Assert.IsTrue(service.IsWeekend(new DateTime(2026, 5, 15)));
        Assert.IsTrue(service.IsWeekend(new DateTime(2026, 5, 16)));
        Assert.IsFalse(service.IsWeekend(new DateTime(2026, 5, 17)));
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.IsHolidayNonWorkingDay" /> returns <see langword="false" /> on a
    /// Saturday with no holiday, even though that Saturday is classified non-working by <see cref="INotableDateService.IsNonWorkingDay" />.
    /// </summary>
    [TestMethod]
    public void IsHolidayNonWorkingDay_WhenSaturdayWithoutHoliday_ShouldReturnFalse()
    {
        var service = BuildService();

        // 2025-01-04 is a Saturday with no holiday under the minimal rule set.
        Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2025, 1, 4)));
        Assert.IsFalse(service.IsHolidayNonWorkingDay(new DateTime(2025, 1, 4)));
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.IsHolidayNonWorkingDay" /> returns <see langword="true" /> when a
    /// non-working notable date covers the supplied day, irrespective of the working week.
    /// </summary>
    [TestMethod]
    public void IsHolidayNonWorkingDay_WhenHolidayOnDay_ShouldReturnTrue()
    {
        var service = BuildService(Fixed("Christmas Day", 12, 25, nonWorking: true));

        Assert.IsTrue(service.IsHolidayNonWorkingDay(new DateTime(2025, 12, 25)));
    }

    /// <summary>
    /// Verifies that <see cref="INotableDateService.IsHolidayNonWorkingDay" /> returns <see langword="false" /> for a
    /// notable date that is not flagged as non-working.
    /// </summary>
    [TestMethod]
    public void IsHolidayNonWorkingDay_WhenObservanceOnlyHoliday_ShouldReturnFalse()
    {
        var service = BuildService(Fixed("Earth Day", 4, 22, nonWorking: false));

        Assert.IsFalse(service.IsHolidayNonWorkingDay(new DateTime(2025, 4, 22)));
    }
}
