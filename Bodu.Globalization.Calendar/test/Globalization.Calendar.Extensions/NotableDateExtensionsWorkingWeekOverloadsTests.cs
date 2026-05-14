// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateExtensionsWorkingWeekOverloadsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

[TestClass]
public class NotableDateExtensionsWorkingWeekOverloadsTests
{
    private static NotableDateService BuildService() => new();

    /// <summary>
    /// Verifies that the per-call <see cref="WeekPattern" /> overload of <c>IsWorkingDay</c> treats Friday as
    /// non-working under a Sunday-to-Thursday pattern.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenSundayToThursdayPatternAndFriday_ShouldReturnFalse()
    {
        NotableDateService service = BuildService();

        // 2026-05-15 is a Friday.
        Assert.IsFalse(new DateOnly(2026, 5, 15).IsWorkingDay(service, WeekPattern.SundayToThursday));
        Assert.IsFalse(new DateTime(2026, 5, 15).IsWorkingDay(service, WeekPattern.SundayToThursday));
    }

    /// <summary>
    /// Verifies that the per-call <see cref="WorkingDaysOfWeek" /> sugar overload of <c>IsWorkingDay</c> agrees with the
    /// <see cref="WeekPattern" /> overload.
    /// </summary>
    [TestMethod]
    public void IsWorkingDay_WhenSugarOverloadAndPatternOverload_ShouldAgree()
    {
        NotableDateService service = BuildService();
        DateOnly friday = new DateOnly(2026, 5, 15);

        Assert.AreEqual(
            friday.IsWorkingDay(service, WeekPattern.SundayToThursday),
            friday.IsWorkingDay(service, WorkingDaysOfWeek.SundayToThursday));
    }

    /// <summary>
    /// Verifies that the <see cref="WeekPattern" /> overload of <c>AddWorkingDays</c> skips Fridays and Saturdays under a
    /// Sunday-to-Thursday pattern.
    /// </summary>
    [TestMethod]
    public void AddWorkingDays_WhenSundayToThursdayPattern_ShouldSkipFridayAndSaturday()
    {
        NotableDateService service = BuildService();

        // 2026-05-14 is a Thursday. +1 working day → Sunday 2026-05-17.
        DateOnly thursday = new DateOnly(2026, 5, 14);

        DateOnly next = thursday.AddWorkingDays(service, WeekPattern.SundayToThursday, 1);

        Assert.AreEqual(new DateOnly(2026, 5, 17), next);
    }

    /// <summary>
    /// Verifies that the <see cref="WeekPattern" /> overload of <c>NextWorkingDay</c> on a Friday under Sun-Thu rolls
    /// forward to Sunday.
    /// </summary>
    [TestMethod]
    public void NextWorkingDay_WhenFridayUnderSundayToThursdayPattern_ShouldReturnSunday()
    {
        NotableDateService service = BuildService();
        DateOnly friday = new DateOnly(2026, 5, 15);

        DateOnly next = friday.NextWorkingDay(service, WeekPattern.SundayToThursday, 1);

        Assert.AreEqual(new DateOnly(2026, 5, 17), next);
    }

    /// <summary>
    /// Verifies that the <see cref="WeekPattern" /> overload of <c>PreviousWorkingDay</c> on a Saturday under Sun-Thu
    /// rolls back to Thursday.
    /// </summary>
    [TestMethod]
    public void PreviousWorkingDay_WhenSaturdayUnderSundayToThursdayPattern_ShouldReturnThursday()
    {
        NotableDateService service = BuildService();
        DateOnly saturday = new DateOnly(2026, 5, 16);

        DateOnly prev = saturday.PreviousWorkingDay(service, WeekPattern.SundayToThursday, 1);

        Assert.AreEqual(new DateOnly(2026, 5, 14), prev);
    }

    /// <summary>
    /// Verifies that the <see cref="WeekPattern" /> overload of <c>WorkingDaysBetween</c> counts only days that are both
    /// in the working week and not flagged non-working by the holiday catalogue.
    /// </summary>
    [TestMethod]
    public void WorkingDaysBetween_WhenSundayToThursdayPattern_ShouldCountFiveDaysInOneWorkingWeek()
    {
        NotableDateService service = BuildService();

        // 2026-05-10 is a Sunday; 2026-05-16 is a Saturday. Under Sun-Thu, the working days are 10..14 = 5 days.
        int count = new DateOnly(2026, 5, 10).WorkingDaysBetween(new DateOnly(2026, 5, 16), service, WeekPattern.SundayToThursday);

        Assert.AreEqual(5, count);
    }

    /// <summary>
    /// Verifies that the <see cref="WeekPattern" /> overload of <c>IsNonWorkingDay</c> is the complement of
    /// <c>IsWorkingDay</c>.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenSundayToThursdayPattern_ShouldBeComplementOfIsWorkingDay()
    {
        NotableDateService service = BuildService();
        DateOnly friday = new DateOnly(2026, 5, 15);

        bool working = friday.IsWorkingDay(service, WeekPattern.SundayToThursday);
        bool nonWorking = friday.IsNonWorkingDay(service, WeekPattern.SundayToThursday);

        Assert.AreNotEqual(working, nonWorking);
    }
}
