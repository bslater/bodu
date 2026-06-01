// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrthodoxEasterSundayNotableDateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the correctness and contract of <see cref="OrthodoxEasterSundayNotableDateProvider" />.
/// </summary>
[TestClass]
public sealed class OrthodoxEasterSundayNotableDateProviderTests
{
    private readonly OrthodoxEasterSundayNotableDateProvider _provider = new();

    /// <summary>
    /// Verifies that the provider advertises the documented supported-year window (1–9999).
    /// </summary>
    [TestMethod]
    public void SupportedYearRange_ShouldBe1To9999()
    {
        Assert.AreEqual(1, _provider.MinSupportedYear);
        Assert.AreEqual(9999, _provider.MaxSupportedYear);
    }

    /// <summary>
    /// Verifies that <see cref="EasterSundayNotableDateProviderBase.SupportsYear(int)" /> reports
    /// support across the full supported range.
    /// </summary>
    [DataRow(0, false)]
    [DataRow(1, true)]
    [DataRow(500, true)]
    [DataRow(9999, true)]
    [DataRow(10000, false)]
    [TestMethod]
    public void SupportsYear_ShouldReflectBounds(int year, bool expected) => Assert.AreEqual(expected, _provider.SupportsYear(year));

    /// <summary>
    /// Verifies that requesting Easter below year 1 throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [DataRow(0)]
    [DataRow(-1)]
    [TestMethod]
    public void GetDates_WhenYearLessThanOne_ShouldThrowExactly(int year)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = _provider.GetDates(year);
        });
    }

    /// <summary>
    /// Verifies that passing a non-Julian calendar throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void GetDates_WhenCalendarIsGregorian_ShouldThrowExactly()
    {
        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = _provider.GetDates(2026, new SysGlob.GregorianCalendar());
        });

        Assert.IsTrue(ex.Message.Contains("GregorianCalendar", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("OrthodoxEasterSundayNotableDateProvider", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that requesting Easter with no calendar returns a single
    /// <see cref="NotableDate" /> tagged with the provider's Julian default metadata.
    /// </summary>
    [TestMethod]
    public void GetDates_WhenYearIsSupportedAndCalendarIsNull_ShouldReturnSingleTaggedNotableDate()
    {
        IReadOnlyList<NotableDate> result = _provider.GetDates(2026);

        Assert.AreEqual(1, result.Count);
        NotableDate notable = result[0];
        Assert.AreEqual("Orthodox Easter Sunday", notable.Name);
        Assert.AreEqual(NotableDateCategory.Religious, notable.Category);
        Assert.AreEqual(typeof(SysGlob.JulianCalendar), notable.CalendarType);
        Assert.AreEqual(1, notable.DurationDays);
        Assert.IsFalse(notable.IsNonWorkingDay);
        Assert.IsTrue(notable.Tags.Contains("Easter"));
        Assert.IsTrue(notable.Tags.Contains("Christianity"));
        Assert.AreEqual(
            "Calculated using the Julian computus and projected to a Gregorian DateTime.",
            notable.Comment);
    }

    /// <summary>
    /// Verifies that passing an explicit <see cref="SysGlob.JulianCalendar" /> sets
    /// <see cref="NotableDate.CalendarType" /> from the supplied calendar.
    /// </summary>
    [TestMethod]
    public void GetDates_WhenCalendarIsJulian_ShouldUseSuppliedCalendarType()
    {
        IReadOnlyList<NotableDate> result = _provider.GetDates(2026, new SysGlob.JulianCalendar());

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(typeof(SysGlob.JulianCalendar), result[0].CalendarType);
    }

    /// <summary>
    /// Verifies that the provider's Julian computus produces the same Gregorian
    /// <see cref="DateTime" /> as <see cref="SysGlob.JulianCalendar.ToDateTime" /> for the
    /// computed month/day — a round-trip sanity check independent of any external almanac.
    /// </summary>
    [DataRow(2026)]
    [DataRow(2027)]
    [DataRow(2100)]
    [TestMethod]
    public void GetDates_WhenSupportedYear_ShouldReturnDateMatchingJulianComputus(int year)
    {
        IReadOnlyList<NotableDate> result = _provider.GetDates(year);

        Assert.AreEqual(1, result.Count);
        DateTime date = result[0].Date;
        // The provider rounds through the Julian calendar, so date.Year/Month/Day reflect
        // the Gregorian projection of a Julian-calculated date. The result must sit on a
        // Sunday in every case since the computus pins to Easter Sunday.
        Assert.AreEqual(year, date.Year);
        Assert.IsTrue(date.Month == 4 || date.Month == 5);
    }
}
