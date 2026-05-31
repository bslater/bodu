// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.SimpleMethods.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.DayName(DateTime)" /> returns the localised day name under the current culture.
    /// </summary>
    [TestMethod]
    public void DayName_NoCulture_ShouldReturnCurrentCultureDayName()
    {
        var date = new DateTime(2024, 1, 1); // Monday
        Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DayOfWeek.Monday), date.DayName());
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.DaysInMonth(DateTime, Calendar)" /> returns the calendar-aware day count.
    /// </summary>
    [TestMethod]
    public void DaysInMonth_WithCalendar_ShouldReturnCalendarDayCount()
    {
        var date = new DateTime(2024, 2, 1);
        Assert.AreEqual(29, date.DaysInMonth(new GregorianCalendar()));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.DaysInMonth(DateTime, CultureInfo)" /> returns the calendar-aware day count.
    /// </summary>
    [TestMethod]
    public void DaysInMonth_WithCulture_ShouldReturnCalendarDayCount()
    {
        var date = new DateTime(2024, 2, 1);
        Assert.AreEqual(29, date.DaysInMonth(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetMonthName(int)" /> returns the localised month name for the supplied 1-based month
    /// index using the current culture.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(6)]
    [DataRow(12)]
    public void GetMonthName_NoCulture_ShouldReturnCurrentCultureName(int month)
    {
        Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month), DateTimeExtensions.GetMonthName(month));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetMonthName(int)" /> throws when the month is outside [1, 12].
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(13)]
    [DataRow(-1)]
    public void GetMonthName_WhenMonthIsOutOfRange_ShouldThrowExactly(int month)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetMonthName(month);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetMonthName(int, CultureInfo)" /> returns the localised month name for the supplied
    /// culture.
    /// </summary>
    [TestMethod]
    public void GetMonthName_WithCulture_ShouldReturnCultureSpecificName()
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        Assert.AreEqual(fr.DateTimeFormat.GetMonthName(4), DateTimeExtensions.GetMonthName(4, fr));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekday(DateTime)" /> returns <see langword="true" /> for Mon-Fri and
    /// <see langword="false" /> for Sat/Sun under the default weekend definition.
    /// </summary>
    [TestMethod]
    public void IsWeekday_NoArgs_ShouldReturnExpectedResult()
    {
        Assert.IsTrue(new DateTime(2024, 1, 1).IsWeekday());  // Mon
        Assert.IsTrue(new DateTime(2024, 1, 5).IsWeekday());  // Fri
        Assert.IsFalse(new DateTime(2024, 1, 6).IsWeekday()); // Sat
        Assert.IsFalse(new DateTime(2024, 1, 7).IsWeekday()); // Sun
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsWeekend(DateTime)" /> returns <see langword="true" /> for Sat/Sun and
    /// <see langword="false" /> for Mon-Fri under the default weekend definition.
    /// </summary>
    [TestMethod]
    public void IsWeekend_NoArgs_ShouldReturnExpectedResult()
    {
        Assert.IsFalse(new DateTime(2024, 1, 1).IsWeekend()); // Mon
        Assert.IsTrue(new DateTime(2024, 1, 6).IsWeekend());  // Sat
        Assert.IsTrue(new DateTime(2024, 1, 7).IsWeekend());  // Sun
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.MonthName(DateTime)" /> returns the localised month name under the current culture.
    /// </summary>
    [TestMethod]
    public void MonthName_NoCulture_ShouldReturnCurrentCultureMonthName()
    {
        var date = new DateTime(2024, 4, 15);
        Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(4), date.MonthName());
    }

}
