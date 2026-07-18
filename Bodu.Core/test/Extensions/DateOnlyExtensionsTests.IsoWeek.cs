// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsoWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetFirstDateOfIsoWeek(int, int)" /> returns the Monday that anchors
    /// the requested ISO week.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 2024, 1, 1)]   // ISO 2024-W01 begins Monday 2024-01-01
    [DataRow(2024, 10, 2024, 3, 4)]
    [DataRow(2020, 1, 2019, 12, 30)] // ISO 2020-W01 begins Monday 2019-12-30
    public void GetFirstDateOfIsoWeek_ShouldReturnMondayThatAnchorsWeek(int isoYear, int isoWeek, int expY, int expM, int expD)
    {
        DateOnly actual = DateOnlyExtensions.GetFirstDateOfIsoWeek(isoYear, isoWeek);
        Assert.AreEqual(new DateOnly(expY, expM, expD), actual);
        Assert.AreEqual(DayOfWeek.Monday, actual.DayOfWeek);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetFirstDateOfIsoWeek(int, int)" /> throws when the ISO week is
    /// outside the valid range for the year.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 0)]
    [DataRow(2024, 53)]
    public void GetFirstDateOfIsoWeek_WhenIsoWeekIsOutOfRange_ShouldThrowExactly(int isoYear, int isoWeek)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfIsoWeek(isoYear, isoWeek);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetFirstDateOfIsoWeek(int, int)" /> throws when the ISO year is
    /// outside the supported range.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(10_000)]
    public void GetFirstDateOfIsoWeek_WhenIsoYearIsOutOfRange_ShouldThrowExactly(int isoYear)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetFirstDateOfIsoWeek(isoYear, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetIsoWeeksInYear(int)" /> returns 53 for long ISO years and 52 for
    /// ordinary years.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 52)]   // 2024-01-01 is Monday → 52
    [DataRow(2023, 52)]   // 2023-01-01 is Sunday → 52
    [DataRow(2020, 53)]   // 2020-01-01 is Wednesday but Dec 31 is Thursday → 53
    [DataRow(2026, 53)]   // 2026-01-01 is Thursday → 53
    public void GetIsoWeeksInYear_ShouldReturnExpectedWeekCount(int year, int expected) => Assert.AreEqual(expected, DateOnlyExtensions.GetIsoWeeksInYear(year));

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetIsoWeeksInYear(int)" /> throws when the year is outside the
    /// supported range.
    /// </summary>
    [TestMethod]
    public void GetIsoWeeksInYear_WhenYearIsOutOfRange_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetIsoWeeksInYear(DateOnly.MaxValue.Year + 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetLastDateOfIsoWeek(int, int)" /> returns the Sunday that ends the
    /// requested ISO week.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 2024, 1, 7)]    // ISO 2024-W01 ends Sunday 2024-01-07
    [DataRow(2020, 1, 2020, 1, 5)]    // ISO 2020-W01 ends Sunday 2020-01-05
    public void GetLastDateOfIsoWeek_ShouldReturnSundayThatEndsWeek(int isoYear, int isoWeek, int expY, int expM, int expD)
    {
        DateOnly actual = DateOnlyExtensions.GetLastDateOfIsoWeek(isoYear, isoWeek);
        Assert.AreEqual(new DateOnly(expY, expM, expD), actual);
        Assert.AreEqual(DayOfWeek.Sunday, actual.DayOfWeek);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.GetLastDateOfIsoWeek(int, int)" /> throws when the ISO week is
    /// outside the valid range for the year.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 0)]
    [DataRow(2024, 53)]
    public void GetLastDateOfIsoWeek_WhenIsoWeekIsOutOfRange_ShouldThrowExactly(int isoYear, int isoWeek)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateOnlyExtensions.GetLastDateOfIsoWeek(isoYear, isoWeek);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="DateOnly" /> ISO week anchors agree exactly with the <see cref="DateTime" /> twin
    /// across ordinary and 53-week years.
    /// </summary>
    [TestMethod]
    [DataRow(2015, 53)]
    [DataRow(2020, 26)]
    [DataRow(2024, 1)]
    [DataRow(2026, 1)]
    public void GetFirstDateOfIsoWeek_WhenComparedWithDateTimeTwin_ShouldReturnIdenticalResults(int isoYear, int isoWeek)
    {
        Assert.AreEqual(DateTimeExtensions.GetFirstDateOfIsoWeek(isoYear, isoWeek).ToDateOnly(), DateOnlyExtensions.GetFirstDateOfIsoWeek(isoYear, isoWeek));
        Assert.AreEqual(DateTimeExtensions.GetLastDateOfIsoWeek(isoYear, isoWeek).ToDateOnly(), DateOnlyExtensions.GetLastDateOfIsoWeek(isoYear, isoWeek));
    }

}
