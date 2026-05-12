// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.GetDayOfWeekForJanuary1.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetDayOfWeekForJanuary1" /> returns the same weekday as
    /// <see cref="DateTime"/> reports for January 1 of the supplied year, across a sample of representative years.
    /// </summary>
    [TestMethod]
    [DataRow(2024)] // Mon
    [DataRow(2023)] // Sun
    [DataRow(2020)] // Wed
    [DataRow(2000)] // Sat (leap year)
    [DataRow(1900)] // Mon (non-leap century)
    [DataRow(2100)] // Fri (non-leap century)
    public void GetDayOfWeekForJanuary1_ShouldReturnExpectedDayOfWeek(int year)
    {
        var expected = new DateTime(year, 1, 1).DayOfWeek;
        Assert.AreEqual(expected, DateTimeExtensions.GetDayOfWeekForJanuary1(year));
    }
}
