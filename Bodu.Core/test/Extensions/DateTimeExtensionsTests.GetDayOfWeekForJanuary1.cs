// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.GetDayOfWeekForJanuary1.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetDayOfWeekForJanuary1" /> drives <see cref="DateTimeExtensions.GetIsoWeeksInYear" />
    /// correctly: a year known to contain 53 ISO weeks must produce Thursday for itself OR Friday for the following year.
    /// </summary>
    [TestMethod]
    [DataRow(2020)] // 53 weeks
    [DataRow(2026)] // 53 weeks
    public void GetDayOfWeekForJanuary1_For53WeekYear_ShouldMatchIsoWeeksInYearPredicate(int year) => Assert.AreEqual(53, DateTimeExtensions.GetIsoWeeksInYear(year));

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetDayOfWeekForJanuary1" /> returns the weekday of 1 January of the
    /// supplied year, as <see cref="DateTime.DayOfWeek" /> reports it.
    /// </summary>
    /// <param name="year">The year to evaluate.</param>
    [TestMethod]
    [DataRow(1)]      // Monday
    [DataRow(2003)]   // Wednesday
    [DataRow(2004)]   // Thursday
    [DataRow(2100)]   // Friday
    [DataRow(9999)]   // Friday
    public void GetDayOfWeekForJanuary1_WhenComparedWithDateTime_ShouldReturnSameDayOfWeek(int year) =>
        Assert.AreEqual(new DateTime(year, 1, 1).DayOfWeek, DateTimeExtensions.GetDayOfWeekForJanuary1(year));

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetDayOfWeekForJanuary1" /> returns a defined
    /// <see cref="DayOfWeek" /> value for representative years and is deterministic across repeated calls.
    /// </summary>
    [TestMethod]
    [DataRow(2024)]
    [DataRow(2023)]
    [DataRow(2020)]
    [DataRow(2000)]
    [DataRow(1900)]
    [DataRow(2100)]
    [DataRow(2026)]
    public void GetDayOfWeekForJanuary1_ShouldReturnDefinedAndStableDayOfWeek(int year)
    {
        DayOfWeek first = DateTimeExtensions.GetDayOfWeekForJanuary1(year);
        DayOfWeek second = DateTimeExtensions.GetDayOfWeekForJanuary1(year);

        Assert.IsTrue(Enum.IsDefined(first), $"Result {first} is not a defined DayOfWeek value.");
        Assert.AreEqual(first, second, "GetDayOfWeekForJanuary1 should be deterministic.");
    }

}
