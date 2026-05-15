// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.MultiYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class FiscalWeekQuarterProviderTests
{

    /// <summary>
    /// Verifies that the <see cref="DateOnly" /> overloads of every quarter-computation method return
    /// results identical to their <see cref="DateTime" /> counterparts for the same input date.
    /// </summary>
    [TestMethod]
    public void DateOnlyOverloads_ShouldProduceSameResultsAsDateTimeOverloads()
    {
        var provider = new FiscalWeekQuarterProvider(
            month: 1,
            dayOfWeek: DayOfWeek.Saturday,
            isFiscalYearEnd: true);

        var dateTime = new DateTime(2024, 6, 15);
        var dateOnly = DateOnly.FromDateTime(dateTime);

        Assert.AreEqual(provider.GetQuarter(dateTime), provider.GetQuarter(dateOnly));
        Assert.AreEqual(
            DateOnly.FromDateTime(provider.GetQuarterStart(dateTime)),
            provider.GetQuarterStartDate(dateOnly));
        Assert.AreEqual(
            DateOnly.FromDateTime(provider.GetQuarterEnd(dateTime)),
            provider.GetQuarterEndDate(dateOnly));
    }

    /// <summary>
    /// Verifies that the last day of Q4 in fiscal year <c>y</c> maps back to quarter 4, and that the
    /// first day of Q1 in fiscal year <c>y + 1</c> maps to quarter 1, exercising the
    /// <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> boundary handoff.
    /// </summary>
    [TestMethod]
    public void GetQuarter_AtFiscalYearBoundary_ShouldHandoffCorrectly()
    {
        DateTime q4End = s_sunday52.GetQuarterEnd(4, Sunday52FiscalYear);
        Assert.AreEqual(4, s_sunday52.GetQuarter(q4End));

        DateTime q1NextStart = s_sunday52.GetQuarterStart(1, Sunday52FiscalYear + 1);
        Assert.AreEqual(1, s_sunday52.GetQuarter(q1NextStart));
    }

    /// <summary>
    /// Verifies the cross-year boundary invariant holds for every fiscal year in 2000–2040: the start
    /// of Q1 in year <c>y</c> must equal the day after the end of Q4 in year <c>y - 1</c>, and each
    /// consecutive quarter within a year must also be contiguous. The total number of days across all
    /// four quarters must equal <c>GetWeeksInFiscalYear(y) × 7</c>. Verified under both alignment
    /// strategies.
    /// </summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetQuarterBoundaries_AcrossFiscalYears2000To2040_ShouldBeContiguous(bool useNearestDayOfWeek)
    {
        var provider = new FiscalWeekQuarterProvider(
            month: 1,
            dayOfWeek: DayOfWeek.Saturday,
            isFiscalYearEnd: true,
            useNearestDayOfWeek: useNearestDayOfWeek);

        for (var year = 2001; year <= 2040; year++)
        {
            DateTime q4PrevEnd = provider.GetQuarterEnd(4, year - 1);
            DateTime q1Start = provider.GetQuarterStart(1, year);
            Assert.AreEqual(q4PrevEnd.AddDays(1), q1Start,
                $"Q1 of FY {year} must begin the day after Q4 of FY {year - 1}.");

            for (var q = 1; q <= 3; q++)
            {
                DateTime end = provider.GetQuarterEnd(q, year);
                DateTime nextStart = provider.GetQuarterStart(q + 1, year);
                Assert.AreEqual(end.AddDays(1), nextStart,
                    $"FY {year} Q{q} end must be the day before Q{q + 1} start.");
            }

            var totalDays = 0;
            for (var q = 1; q <= 4; q++)
            {
                DateTime start = provider.GetQuarterStart(q, year);
                DateTime end = provider.GetQuarterEnd(q, year);
                totalDays += (int)(end - start).TotalDays + 1;
            }

            var expectedDays = provider.GetWeeksInFiscalYear(year) * 7;
            Assert.AreEqual(expectedDays, totalDays,
                $"FY {year} must contain exactly {expectedDays} days across all four quarters.");
        }
    }

    /// <summary>
    /// Verifies that in a 53-week fiscal year Q4 spans exactly 98 days (14 weeks) while Q1–Q3 each
    /// span exactly 91 days (13 weeks), confirming the extra week is confined to Q4.
    /// </summary>
    [TestMethod]
    public void GetQuarterStartEnd_In53WeekYear_ShouldConfineExtraWeekToQ4()
    {
        var provider = new FiscalWeekQuarterProvider(
            month: 1,
            dayOfWeek: DayOfWeek.Saturday,
            isFiscalYearEnd: true);

        var fiscalYear = FindFirst53WeekYear(provider, 2000, 2040);

        for (var q = 1; q <= 3; q++)
        {
            DateTime start = provider.GetQuarterStart(q, fiscalYear);
            DateTime end = provider.GetQuarterEnd(q, fiscalYear);
            var days = (int)(end - start).TotalDays + 1;
            Assert.AreEqual(91, days, $"Q{q} of FY {fiscalYear} must span 91 days.");
        }

        DateTime q4Start = provider.GetQuarterStart(4, fiscalYear);
        DateTime q4End = provider.GetQuarterEnd(4, fiscalYear);
        var q4Days = (int)(q4End - q4Start).TotalDays + 1;
        Assert.AreEqual(98, q4Days, $"Q4 of FY {fiscalYear} must span 98 days (14 weeks).");
    }

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetWeeksInFiscalYear(int)" /> returns
    /// either 52 or 53 consistent with <see cref="FiscalWeekQuarterProvider.Is53WeekFiscalYear(int)" />
    /// across a range of fiscal years.
    /// </summary>
    [TestMethod]
    public void GetWeeksInFiscalYear_AcrossMultipleYears_ShouldBeConsistentWithIs53WeekFiscalYear()
    {
        var provider = new FiscalWeekQuarterProvider(
            month: 1,
            dayOfWeek: DayOfWeek.Saturday,
            isFiscalYearEnd: true);

        for (var year = 2000; year <= 2040; year++)
        {
            var expected = provider.Is53WeekFiscalYear(year) ? 53 : 52;
            Assert.AreEqual(expected, provider.GetWeeksInFiscalYear(year),
                $"GetWeeksInFiscalYear({year}) must agree with Is53WeekFiscalYear({year}).");
        }
    }
    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.Is53WeekFiscalYear(int)" /> returns the
    /// expected result for multiple fiscal years under a single provider instance, confirming that
    /// the provider is stateless with respect to any single year and computes values on demand.
    /// </summary>
    /// <remarks>
    /// Under a Saturday-aligned, nearest-day, fiscal-year-end-January rule, the 53-week fiscal years
    /// between 2000 and 2030 are 2000, 2005, 2011, 2016, 2022, and 2028 — each has a span of 371 days
    /// between the nearest-Saturday alignments of consecutive 1 February anchors.
    /// </remarks>
    [TestMethod]
    [DataRow(2005, true)]
    [DataRow(2011, true)]
    [DataRow(2016, true)]
    [DataRow(2022, true)]
    [DataRow(2028, true)]
    [DataRow(2020, false)]
    [DataRow(2024, false)]
    [DataRow(2025, false)]
    public void Is53WeekFiscalYear_WhenInvokedForMultipleYearsUnderSameRule_ShouldReturnExpected(int fiscalYear, bool expected)
    {
        var provider = new FiscalWeekQuarterProvider(
            month: 1,
            dayOfWeek: DayOfWeek.Saturday,
            isFiscalYearEnd: true);

        Assert.AreEqual(expected, provider.Is53WeekFiscalYear(fiscalYear));
    }

    private static int FindFirst53WeekYear(FiscalWeekQuarterProvider provider, int fromYear, int toYear)
    {
        for (var year = fromYear; year <= toYear; year++)
        {
            if (provider.Is53WeekFiscalYear(year))
                return year;
        }

        throw new InvalidOperationException(
            $"No 53-week fiscal year found in the range {fromYear}–{toYear}.");
    }

}
