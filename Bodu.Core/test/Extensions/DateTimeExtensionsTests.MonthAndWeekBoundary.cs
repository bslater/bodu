// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.MonthAndWeekBoundary.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfMonth(int, int)" /> returns the first day of the supplied month and year.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 2024, 1, 1)]
    [DataRow(2024, 12, 2024, 12, 1)]
    [DataRow(2000, 2, 2000, 2, 1)]
    public void GetFirstDateOfMonth_ShouldReturnFirstDayOfMonth(int year, int month, int expY, int expM, int expD)
    {
        DateTime actual = DateTimeExtensions.GetFirstDateOfMonth(year, month);
        Assert.AreEqual(new DateTime(expY, expM, expD), actual);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfMonth(int, int)" /> throws when the month is outside [1, 12].
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(13)]
    public void GetFirstDateOfMonth_WhenMonthIsOutOfRange_ShouldThrowExactly(int month)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetFirstDateOfMonth(2024, month);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfWeekInMonth(int, int, DayOfWeek)" /> returns the first occurrence of
    /// the specified weekday in the given month and year.
    /// </summary>
    [TestMethod]
    public void GetFirstDateOfWeekInMonth_ShouldReturnFirstOccurrenceOfWeekday()
    {
        // First Monday of April 2024 is April 1, 2024.
        DateTime actual = DateTimeExtensions.GetFirstDateOfWeekInMonth(2024, 4, DayOfWeek.Monday);
        Assert.AreEqual(new DateTime(2024, 4, 1), actual);
        Assert.AreEqual(DayOfWeek.Monday, actual.DayOfWeek);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfWeekInMonth(int, int, DayOfWeek)" /> throws when arguments are invalid.
    /// </summary>
    [TestMethod]
    public void GetFirstDateOfWeekInMonth_WhenArgumentsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetFirstDateOfWeekInMonth(2024, 13, DayOfWeek.Monday);
        });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetFirstDateOfWeekInMonth(2024, 1, (DayOfWeek)10);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfWeekInMonth(int, int, DayOfWeek)" /> finds the first occurrence when the
    /// month does not begin on the target weekday.
    /// </summary>
    [TestMethod]
    public void GetFirstDateOfWeekInMonth_WhenMonthStartsDifferentDay_ShouldReturnFirstMatchingDay()
    {
        // First Saturday of April 2024 (which starts on a Monday) is April 6, 2024.
        DateTime actual = DateTimeExtensions.GetFirstDateOfWeekInMonth(2024, 4, DayOfWeek.Saturday);
        Assert.AreEqual(new DateTime(2024, 4, 6), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetLastDateOfMonth(int, int)" /> returns the last day of the supplied month and year,
    /// handling leap-year February correctly.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 2024, 1, 31)]
    [DataRow(2024, 2, 2024, 2, 29)] // leap year
    [DataRow(2023, 2, 2023, 2, 28)] // non-leap year
    [DataRow(2024, 4, 2024, 4, 30)]
    [DataRow(2024, 12, 2024, 12, 31)]
    public void GetLastDateOfMonth_ShouldReturnLastDayOfMonth(int year, int month, int expY, int expM, int expD)
    {
        DateTime actual = DateTimeExtensions.GetLastDateOfMonth(year, month);
        Assert.AreEqual(new DateTime(expY, expM, expD), actual);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetLastDateOfMonth(int, int)" /> throws when the month is outside [1, 12].
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(13)]
    public void GetLastDateOfMonth_WhenMonthIsOutOfRange_ShouldThrowExactly(int month)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfMonth(2024, month);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetLastDateOfWeekInMonth(int, int, DayOfWeek)" /> returns the last occurrence of the
    /// specified weekday in the given month and year.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInMonth_ShouldReturnLastOccurrenceOfWeekday()
    {
        // Last Monday of April 2024 is April 29, 2024.
        DateTime actual = DateTimeExtensions.GetLastDateOfWeekInMonth(2024, 4, DayOfWeek.Monday);
        Assert.AreEqual(new DateTime(2024, 4, 29), actual);
        Assert.AreEqual(DayOfWeek.Monday, actual.DayOfWeek);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetLastDateOfWeekInMonth(int, int, DayOfWeek)" /> throws when arguments are invalid.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInMonth_WhenArgumentsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfWeekInMonth(2024, 0, DayOfWeek.Monday);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetLastDateOfWeekInMonth(int, int, DayOfWeek)" /> returns the last occurrence when
    /// the month ends on a weekday other than the target.
    /// </summary>
    [TestMethod]
    public void GetLastDateOfWeekInMonth_WhenMonthEndsDifferentDay_ShouldReturnLastMatchingDay()
    {
        // Last Sunday of April 2024 is April 28, 2024.
        DateTime actual = DateTimeExtensions.GetLastDateOfWeekInMonth(2024, 4, DayOfWeek.Sunday);
        Assert.AreEqual(new DateTime(2024, 4, 28), actual);
    }

}
