// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GregorianEasterSundayNotableDateProviderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Providers;
using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the correctness and contract of <see cref="GregorianEasterSundayNotableDateProvider" />.
/// </summary>
[TestClass]
public sealed class GregorianEasterSundayNotableDateProviderTests
{
    private readonly GregorianEasterSundayNotableDateProvider _provider = new();

    /// <summary>
    /// Verifies that the provider advertises the documented supported-year window.
    /// </summary>
    [TestMethod]
    public void SupportedYearRange_ShouldBe1583To9999()
    {
        Assert.AreEqual(1583, _provider.MinSupportedYear);
        Assert.AreEqual(9999, _provider.MaxSupportedYear);
    }

    /// <summary>
    /// Verifies that <see cref="EasterSundayNotableDateProviderBase.SupportsYear(int)" /> returns
    /// the correct answer on both sides of the supported-year window.
    /// </summary>
    [DataRow(1582, false)]
    [DataRow(1583, true)]
    [DataRow(2026, true)]
    [DataRow(9999, true)]
    [DataRow(10000, false)]
    [TestMethod]
    public void SupportsYear_ShouldReflectBounds(int year, bool expected)
    {
        Assert.AreEqual(expected, _provider.SupportsYear(year));
    }

    /// <summary>
    /// Verifies that requesting Easter below year 1 throws
    /// <see cref="ArgumentOutOfRangeException" /> from the base guard.
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
    /// Verifies that passing a non-Gregorian calendar throws <see cref="NotSupportedException" />
    /// via the overridden <c>ValidateCalendar</c> contract.
    /// </summary>
    [TestMethod]
    public void GetDates_WhenCalendarIsJulian_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = _provider.GetDates(2026, new SysGlob.JulianCalendar());
        });

        Assert.IsTrue(ex.Message.Contains("JulianCalendar", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("GregorianEasterSundayNotableDateProvider", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that requesting Easter with no calendar returns a single
    /// <see cref="NotableDate" /> tagged with the provider's default metadata.
    /// </summary>
    [TestMethod]
    public void GetDates_WhenYearIsSupportedAndCalendarIsNull_ShouldReturnSingleTaggedNotableDate()
    {
        IReadOnlyList<NotableDate> result = _provider.GetDates(2026);

        Assert.AreEqual(1, result.Count);
        NotableDate notable = result[0];
        Assert.AreEqual("Easter Sunday", notable.Name);
        Assert.AreEqual(NotableDateCategory.Religious, notable.Category);
        Assert.AreEqual(typeof(SysGlob.GregorianCalendar), notable.CalendarType);
        Assert.AreEqual(1, notable.DurationDays);
        Assert.IsFalse(notable.IsNonWorkingDay);
        Assert.IsTrue(notable.Tags.Contains("Easter"));
        Assert.IsTrue(notable.Tags.Contains("Christianity"));
        Assert.AreEqual("Calculated using the Gregorian computus.", notable.Comment);
    }

    /// <summary>
    /// Verifies that passing an explicit <see cref="SysGlob.GregorianCalendar" /> sets
    /// <see cref="NotableDate.CalendarType" /> from the supplied calendar rather than the
    /// provider-default type.
    /// </summary>
    [TestMethod]
    public void GetDates_WhenCalendarIsGregorian_ShouldUseSuppliedCalendarType()
    {
        IReadOnlyList<NotableDate> result = _provider.GetDates(2026, new SysGlob.GregorianCalendar());

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(typeof(SysGlob.GregorianCalendar), result[0].CalendarType);
    }

    /// <summary>
    /// Verifies a known Gregorian Easter Sunday date (2026-04-05) to confirm the computus.
    /// </summary>
    [DataRow(2020, 2020, 4, 12)]
    [DataRow(2024, 2024, 3, 31)]
    [DataRow(2025, 2025, 4, 20)]
    [DataRow(2026, 2026, 4, 5)]
    [DataRow(2027, 2027, 3, 28)]
    [DataRow(1583, 1583, 4, 10)]
    [TestMethod]
    public void GetDates_WhenSupportedYear_ShouldReturnComputusResult(
        int year,
        int expectedYear,
        int expectedMonth,
        int expectedDay)
    {
        IReadOnlyList<NotableDate> result = _provider.GetDates(year);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new DateTime(expectedYear, expectedMonth, expectedDay), result[0].Date);
    }

    /// <summary>
    /// Verifies that the cache path returns the same <see cref="DateTime" /> for repeated
    /// invocations with the same year.
    /// </summary>
    [TestMethod]
    public void GetDates_WhenCalledTwiceForSameYear_ShouldReturnEqualDates()
    {
        DateTime first = _provider.GetDates(2026)[0].Date;
        DateTime second = _provider.GetDates(2026)[0].Date;

        Assert.AreEqual(first, second);
    }
}
