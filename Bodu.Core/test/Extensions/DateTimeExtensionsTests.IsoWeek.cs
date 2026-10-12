// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsoWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

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
        DateTime actual = DateTimeExtensions.GetFirstDateOfIsoWeek(isoYear, isoWeek);
        Assert.AreEqual(new DateTime(expY, expM, expD), actual);
        Assert.AreEqual(DayOfWeek.Monday, actual.DayOfWeek);
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
    /// Verifies that <see cref="DateTimeExtensions.GetIsoWeeksInYear(int)" /> returns 53 for years whose first day is Thursday and 52
    /// for ordinary years.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 52)]   // 2024-01-01 is Monday → 52
    [DataRow(2023, 52)]   // 2023-01-01 is Sunday → 52
    [DataRow(2020, 53)]   // 2020-01-01 is Wednesday but Dec 31 is Thursday → 53
    [DataRow(2026, 53)]   // 2026-01-01 is Thursday → 53
    public void GetIsoWeeksInYear_ShouldReturnExpectedWeekCount(int year, int expected) => Assert.AreEqual(expected, DateTimeExtensions.GetIsoWeeksInYear(year));

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetIsoWeeksInYear(int)" /> returns the week count that
    /// <see cref="ISOWeek.GetWeeksInYear(int)" /> reports, for years whose first or last days straddle a Thursday.
    /// </summary>
    /// <param name="year">The ISO year to evaluate.</param>
    [TestMethod]
    [DataRow(2003)]   // 52 weeks: 2003-01-01 is Wednesday, 2003-12-31 is Wednesday
    [DataRow(2004)]   // 53 weeks: 2004-01-01 is Thursday
    [DataRow(2009)]   // 53 weeks: 2009-01-01 is Thursday
    [DataRow(2015)]   // 53 weeks: 2015-01-01 is Thursday
    [DataRow(2020)]   // 53 weeks: 2020-12-31 is Thursday
    [DataRow(2026)]   // 53 weeks: 2026-01-01 is Thursday
    [DataRow(2031)]   // 52 weeks: 2031-01-01 is Wednesday, 2031-12-31 is Wednesday
    [DataRow(2032)]   // 53 weeks: 2032-01-01 is Thursday
    public void GetIsoWeeksInYear_WhenComparedWithIsoWeek_ShouldReturnSameWeekCount(int year) =>
        Assert.AreEqual(ISOWeek.GetWeeksInYear(year), DateTimeExtensions.GetIsoWeeksInYear(year));

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetIsoWeeksInYear(int)" /> returns the week count that
    /// <see cref="ISOWeek.GetWeeksInYear(int)" /> reports for every year from 1 to 9999.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void GetIsoWeeksInYear_WhenEveryYearIsComparedWithIsoWeek_ShouldReturnSameWeekCount()
    {
        var mismatches = new List<string>();
        for (int year = DateTime.MinValue.Year; year <= DateTime.MaxValue.Year; year++)
        {
            int expected = ISOWeek.GetWeeksInYear(year);
            int actual = DateTimeExtensions.GetIsoWeeksInYear(year);
            if (actual != expected)
                mismatches.Add(string.Create(CultureInfo.InvariantCulture, $"{year}: expected {expected}, actual {actual}"));
        }

        Assert.AreEqual(0, mismatches.Count, $"{mismatches.Count} years disagree, first: {string.Join("; ", mismatches.Take(5))}");
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfIsoWeek(int, int)" /> and
    /// <see cref="DateTimeExtensions.GetLastDateOfIsoWeek(int, int)" /> return the Monday and Sunday that
    /// <see cref="ISOWeek.ToDateTime(int, int, DayOfWeek)" /> reports for the last week of an ISO year.
    /// </summary>
    /// <param name="isoYear">The ISO year.</param>
    /// <param name="isoWeek">The last ISO week of <paramref name="isoYear" />.</param>
    [TestMethod]
    [DataRow(2003, 52)]
    [DataRow(2004, 53)]
    [DataRow(2009, 53)]
    [DataRow(2032, 53)]
    public void GetFirstDateOfIsoWeek_WhenWeekIsLastOfIsoYear_ShouldMatchIsoWeekToDateTime(int isoYear, int isoWeek)
    {
        Assert.AreEqual(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday), DateTimeExtensions.GetFirstDateOfIsoWeek(isoYear, isoWeek));
        Assert.AreEqual(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Sunday), DateTimeExtensions.GetLastDateOfIsoWeek(isoYear, isoWeek));
    }

    /// <summary>
    /// Verifies that the last day of a 53-week ISO year round-trips: its
    /// <see cref="DateTimeExtensions.IsoWeekOfYear" /> is 53, and the week that
    /// <see cref="DateTimeExtensions.GetFirstDateOfIsoWeek(int, int)" /> and
    /// <see cref="DateTimeExtensions.GetLastDateOfIsoWeek(int, int)" /> bound for week 53 contains it.
    /// </summary>
    /// <param name="year">A year whose 31 December lies in ISO week 53 of the same ISO year.</param>
    [TestMethod]
    [DataRow(2004)]
    [DataRow(2009)]
    [DataRow(2020)]
    public void GetFirstDateOfIsoWeek_WhenWeekOfLastDayOfLongIsoYear_ShouldContainThatDay(int year)
    {
        var lastDay = new DateTime(year, 12, 31);

        Assert.AreEqual(53, lastDay.IsoWeekOfYear);
        Assert.IsTrue(DateTimeExtensions.GetFirstDateOfIsoWeek(year, 53) <= lastDay);
        Assert.IsTrue(lastDay <= DateTimeExtensions.GetLastDateOfIsoWeek(year, 53));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.GetFirstDateOfIsoWeek(int, int)" /> and
    /// <see cref="DateTimeExtensions.GetLastDateOfIsoWeek(int, int)" /> reject week 53 of an ISO year that has only 52
    /// weeks.
    /// </summary>
    /// <param name="isoYear">An ISO year with 52 weeks.</param>
    [TestMethod]
    [DataRow(2003)]
    [DataRow(2031)]
    public void GetFirstDateOfIsoWeek_WhenWeek53OfShortIsoYear_ShouldThrowArgumentOutOfRangeException(int isoYear)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetFirstDateOfIsoWeek(isoYear, 53);
        });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = DateTimeExtensions.GetLastDateOfIsoWeek(isoYear, 53);
        });
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
    /// Verifies that <see cref="DateTimeExtensions.GetLastDateOfIsoWeek(int, int)" /> returns the Sunday that ends the requested ISO week.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 1, 2024, 1, 7)]    // ISO 2024-W01 ends Sunday 2024-01-07
    [DataRow(2020, 1, 2020, 1, 5)]    // ISO 2020-W01 ends Sunday 2020-01-05
    public void GetLastDateOfIsoWeek_ShouldReturnSundayThatEndsWeek(int isoYear, int isoWeek, int expY, int expM, int expD)
    {
        DateTime actual = DateTimeExtensions.GetLastDateOfIsoWeek(isoYear, isoWeek);
        Assert.AreEqual(new DateTime(expY, expM, expD), actual);
        Assert.AreEqual(DayOfWeek.Sunday, actual.DayOfWeek);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
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
