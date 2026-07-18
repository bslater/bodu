// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.GetQuarterStart.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class FiscalWeekQuarterProviderTests
{

    // -----------------------------------------------------------------------
    // GetQuarterStart(int, int)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> returns the
    /// correct quarter start date for each quarter of each fiscal year across all four providers.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetQuarterBoundaryTestData))]
    public void GetQuarterStart_WhenCalledWithQuarterAndFiscalYear_ShouldReturnExpectedStartDate(
        FiscalWeekQuarterProvider provider,
        int quarter,
        int fiscalYear,
        DateTime expectedStart,
        DateTime _) => Assert.AreEqual(expectedStart, provider.GetQuarterStart(quarter, fiscalYear));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> returns a
    /// <see cref="DateTime" /> with <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    [TestMethod]
    public void GetQuarterStart_WhenCalledWithQuarterAndFiscalYear_ShouldReturnUnspecifiedKind() => Assert.AreEqual(DateTimeKind.Unspecified, s_sunday52.GetQuarterStart(1, Sunday52FiscalYear).Kind);

    /// <summary>
    /// Verifies that the start of each successive quarter is exactly 91 days (13 weeks) after the
    /// previous quarter start, for Q1 through Q3 in a 52-week provider.
    /// </summary>
    [TestMethod]
    public void GetQuarterStart_WhenConsecutiveQuarters_ShouldBeSeparatedBy91Days()
    {
        for (int q = 1; q <= 3; q++)
        {
            DateTime current = s_sunday52.GetQuarterStart(q, Sunday52FiscalYear);
            DateTime next = s_sunday52.GetQuarterStart(q + 1, Sunday52FiscalYear);
            Assert.AreEqual(91, (next - current).Days, $"Gap between Q{q} and Q{q + 1} starts.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> resolves a
    /// date after the anchor fiscal year into the following fiscal year and returns that year's
    /// Q1 start date.
    /// </summary>
    [TestMethod]
    public void GetQuarterStart_WhenDateTimeIsAfterAnchorFiscalYear_ShouldResolveToNextFiscalYear() =>
        // Dec 31, 2023 = the first day of FY 2024 under Sunday52 (nearest Sunday to Jan 1, 2024).
        Assert.AreEqual(new DateTime(2023, 12, 31), s_sunday52.GetQuarterStart(new DateTime(2023, 12, 31)));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> resolves a
    /// date prior to the anchor fiscal year into the preceding fiscal year and returns that year's
    /// Q4 start date.
    /// </summary>
    [TestMethod]
    public void GetQuarterStart_WhenDateTimeIsBeforeAnchorFiscalYear_ShouldResolveToPriorFiscalYear() =>
        // Dec 31, 2022 resolves into FY 2022 (starts Jan 2, 2022) at its Q4 boundary.
        // FY 2022 Q4 starts Jan 2 + 273 days = Oct 2, 2022.
        Assert.AreEqual(new DateTime(2022, 10, 2), s_sunday52.GetQuarterStart(new DateTime(2022, 12, 31)));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> resolves a
    /// date prior to the April-anchored fiscal year into the preceding fiscal year.
    /// </summary>
    [TestMethod]
    public void GetQuarterStart_WhenDateTimeIsBeforeAprilAnchorFiscalYear_ShouldResolveToPriorFiscalYear() =>
        // Mar 31, 2023 = Friday. FY 2022 under Saturday52 starts Apr 2, 2022, 52 weeks; Q4 starts
        // Apr 2 + 273 days = Dec 31, 2022.
        Assert.AreEqual(new DateTime(2022, 12, 31), s_saturday52.GetQuarterStart(new DateTime(2023, 3, 31)));
    // -----------------------------------------------------------------------
    // GetQuarterStart(DateTime)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> returns the
    /// correct quarter start date when the input is the first day of each quarter.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetQuarterBoundaryTestData))]
    public void GetQuarterStart_WhenDateTimeIsFirstDayOfQuarter_ShouldReturnThatDate(
        FiscalWeekQuarterProvider provider,
        int _,
        int __,
        DateTime expectedStart,
        DateTime ___) => Assert.AreEqual(expectedStart, provider.GetQuarterStart(expectedStart));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> returns the
    /// Q4 start date for a date in the 53rd week of the <see cref="s_sunday53" /> fiscal year.
    /// 28 December 2020 is the first day of week 53.
    /// </summary>
    [TestMethod]
    public void GetQuarterStart_WhenDateTimeIsInThe53rdWeek_ShouldReturnQ4StartDate() => Assert.AreEqual(new DateTime(2020, 9, 27), s_sunday53.GetQuarterStart(new DateTime(2020, 12, 28)));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> returns the
    /// correct quarter start date when the input is the last day of that quarter, across every
    /// quarter and every fixture provider.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetQuarterBoundaryTestData))]
    public void GetQuarterStart_WhenDateTimeIsLastDayOfQuarter_ShouldReturnStartOfThatQuarter(
        FiscalWeekQuarterProvider provider,
        int _,
        int __,
        DateTime expectedStart,
        DateTime expectedEnd) => Assert.AreEqual(expectedStart, provider.GetQuarterStart(expectedEnd));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> returns the
    /// Q1 start date when the input is leap day (29 February 2020), which falls within Q1 of the
    /// <see cref="s_sunday53" /> provider.
    /// </summary>
    [TestMethod]
    public void GetQuarterStart_WhenDateTimeIsLeapDayInQ1_ShouldReturnQ1StartDate() => Assert.AreEqual(new DateTime(2019, 12, 29), s_sunday53.GetQuarterStart(new DateTime(2020, 2, 29)));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is greater than 4.
    /// </summary>
    [TestMethod]
    [DataRow(5)]
    [DataRow(100)]
    public void GetQuarterStart_WhenQuarterIsAboveValidRange_ShouldThrowExactly(int quarter)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            s_sunday52.GetQuarterStart(quarter, Sunday52FiscalYear));
    }

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is less than 1.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void GetQuarterStart_WhenQuarterIsBelowValidRange_ShouldThrowExactly(int quarter)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            s_sunday52.GetQuarterStart(quarter, Sunday52FiscalYear));
    }

    // -----------------------------------------------------------------------
    // December fiscal-year-end anchor (month == 13 sentinel)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that a December fiscal-year-end anchor (<c>month = 12</c>, <c>isFiscalYearEnd = true</c>), whose
    /// internal anchor month is the month-13 sentinel, resolves each quarter's <see cref="DateTime" /> start to the
    /// same boundary as an equivalent January-start anchor, confirming the sentinel maps to January of the following
    /// calendar year.
    /// </summary>
    [TestMethod]
    public void GetQuarterStart_WhenDecemberFiscalYearEndAnchor_ShouldMatchEquivalentJanuaryStartAnchor()
    {
        // month=12 + isFiscalYearEnd:true yields the month-13 sentinel = January of the next calendar year, so this
        // provider's fiscal calendar is physically identical to a January-anchored one.
        var decemberEnd = new FiscalWeekQuarterProvider(12, DayOfWeek.Sunday, isFiscalYearEnd: true, useNearestDayOfWeek: false);
        var januaryStart = new FiscalWeekQuarterProvider(1, DayOfWeek.Sunday, isFiscalYearEnd: false, useNearestDayOfWeek: false);

        for (var date = new DateTime(2020, 1, 1); date < new DateTime(2025, 1, 1); date = date.AddDays(29))
        {
            Assert.AreEqual(
                januaryStart.GetQuarterStart(date),
                decemberEnd.GetQuarterStart(date),
                $"Quarter start mismatch for {date:yyyy-MM-dd}.");
        }
    }

    /// <summary>
    /// Verifies that a December fiscal-year-end anchor's fiscal year <c>Y</c> begins on the same date as the
    /// January-start anchor's fiscal year <c>Y + 1</c>, confirming the month-13 sentinel advances the calendar year.
    /// </summary>
    [TestMethod]
    public void GetQuarterStart_WhenDecemberFiscalYearEndAnchor_ShouldStartOneCalendarYearAheadOfJanuaryAnchor()
    {
        var decemberEnd = new FiscalWeekQuarterProvider(12, DayOfWeek.Sunday, isFiscalYearEnd: true, useNearestDayOfWeek: false);
        var januaryStart = new FiscalWeekQuarterProvider(1, DayOfWeek.Sunday, isFiscalYearEnd: false, useNearestDayOfWeek: false);

        Assert.AreEqual(
            januaryStart.GetQuarterStart(1, 2024),
            decemberEnd.GetQuarterStart(1, 2023));
    }

}
