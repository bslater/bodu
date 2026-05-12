// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsoWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetIsoWeeksInYear(int)" /> returns 53 for years whose first day is Thursday and 52
    /// for ordinary years.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 52)]   // 2024-01-01 is Monday → 52
    [DataRow(2023, 52)]   // 2023-01-01 is Sunday → 52
    [DataRow(2020, 53)]   // 2020-01-01 is Wednesday but Dec 31 is Thursday → 53
    [DataRow(2026, 53)]   // 2026-01-01 is Thursday → 53
    public void GetIsoWeeksInYear_ShouldReturnExpectedWeekCount(int year, int expected)
    {
        Assert.AreEqual(expected, DateTimeExtensions.GetIsoWeeksInYear(year));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetIsoWeeksInYear(int)" /> throws when the year is outside the supported range.
    /// </summary>
    [TestMethod]
    public void GetIsoWeeksInYear_WhenYearIsOutOfRange_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetIsoWeeksInYear(DateTime.MaxValue.Year + 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfIsoWeek(int, int)" /> returns the Monday that anchors the requested
    /// ISO week.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 2024, 1, 1)]   // ISO 2024-W01 begins Monday 2024-01-01
    [DataRow(2024, 10, 2024, 3, 4)]
    [DataRow(2020, 1, 2019, 12, 30)] // ISO 2020-W01 begins Monday 2019-12-30
    public void GetFirstDateOfIsoWeek_ShouldReturnMondayThatAnchorsWeek(int isoYear, int isoWeek, int expY, int expM, int expD)
    {
        var actual = DateTimeExtensions.GetFirstDateOfIsoWeek(isoYear, isoWeek);
        Assert.AreEqual(new DateTime(expY, expM, expD), actual);
        Assert.AreEqual(DayOfWeek.Monday, actual.DayOfWeek);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetLastDateOfIsoWeek(int, int)" /> returns the Sunday that ends the requested ISO week.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 2024, 1, 7)]    // ISO 2024-W01 ends Sunday 2024-01-07
    [DataRow(2020, 1, 2020, 1, 5)]    // ISO 2020-W01 ends Sunday 2020-01-05
    public void GetLastDateOfIsoWeek_ShouldReturnSundayThatEndsWeek(int isoYear, int isoWeek, int expY, int expM, int expD)
    {
        var actual = DateTimeExtensions.GetLastDateOfIsoWeek(isoYear, isoWeek);
        Assert.AreEqual(new DateTime(expY, expM, expD), actual);
        Assert.AreEqual(DayOfWeek.Sunday, actual.DayOfWeek);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfIsoWeek(int, int)" /> throws when the ISO week is outside the valid
    /// range for the year.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 0)]
    [DataRow(2024, 53)]
    public void GetFirstDateOfIsoWeek_WhenIsoWeekIsOutOfRange_ShouldThrowExactly(int isoYear, int isoWeek)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetFirstDateOfIsoWeek(isoYear, isoWeek);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetLastDateOfIsoWeek(int, int)" /> throws when the ISO week is outside the valid
    /// range for the year.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 0)]
    [DataRow(2024, 53)]
    public void GetLastDateOfIsoWeek_WhenIsoWeekIsOutOfRange_ShouldThrowExactly(int isoYear, int isoWeek)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfIsoWeek(isoYear, isoWeek);
        });
    }
}
