// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.GetQuarterStartDate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class FiscalWeekQuarterProviderTests
{

    // -----------------------------------------------------------------------
    // GetQuarterStartDate(int) — obsolete single-arg overload
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that the obsolete single-argument
    /// <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(int)" /> overload throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
#pragma warning disable CS0618 // intentional: we verify the obsolete overload still throws
    public void GetQuarterStartDate_ObsoleteSingleArgOverload_ShouldThrowNotSupportedException() => Assert.ThrowsExactly<NotSupportedException>(() => s_sunday52.GetQuarterStartDate(1));
#pragma warning restore CS0618

    // -----------------------------------------------------------------------
    // GetQuarterStartDate(DateOnly)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> returns
    /// the correct quarter start when the input is the last day of that quarter, across every quarter
    /// and every fixture provider.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetQuarterBoundaryTestData))]
    public void GetQuarterStartDate_WhenCalledWithLastDayOfQuarter_ShouldReturnExpectedStartDate(
        FiscalWeekQuarterProvider provider,
        int _,
        int __,
        DateTime expectedStart,
        DateTime expectedEnd)
    {
        Assert.AreEqual(
            DateOnly.FromDateTime(expectedStart),
            provider.GetQuarterStartDate(DateOnly.FromDateTime(expectedEnd)));
    }
    // -----------------------------------------------------------------------
    // GetQuarterStartDate(int, int)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(int, int)" /> returns a
    /// <see cref="DateOnly" /> consistent with
    /// <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> for each quarter of each
    /// fiscal year across all four providers.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetQuarterBoundaryTestData))]
    public void GetQuarterStartDate_WhenCalledWithQuarterAndFiscalYear_ShouldMatchGetQuarterStartConvertedToDateOnly(
        FiscalWeekQuarterProvider provider,
        int quarter,
        int fiscalYear,
        DateTime _,
        DateTime __)
    {
        Assert.AreEqual(
            DateOnly.FromDateTime(provider.GetQuarterStart(quarter, fiscalYear)),
            provider.GetQuarterStartDate(quarter, fiscalYear));
    }

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> resolves
    /// a date after the anchor fiscal year into the following fiscal year.
    /// </summary>
    [TestMethod]
    public void GetQuarterStartDate_WhenDateOnlyIsAfterAnchorFiscalYear_ShouldResolveToNextFiscalYear() =>
        // Dec 31, 2023 → FY 2024 Q1 start = Dec 31, 2023.
        Assert.AreEqual(new DateOnly(2023, 12, 31), s_sunday52.GetQuarterStartDate(new DateOnly(2023, 12, 31)));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> resolves
    /// a date before the anchor fiscal year into the preceding fiscal year.
    /// </summary>
    [TestMethod]
    public void GetQuarterStartDate_WhenDateOnlyIsBeforeAnchorFiscalYear_ShouldResolveToPriorFiscalYear() =>
        // Dec 31, 2022 → FY 2022 Q4 start = Oct 2, 2022.
        Assert.AreEqual(new DateOnly(2022, 10, 2), s_sunday52.GetQuarterStartDate(new DateOnly(2022, 12, 31)));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> returns
    /// the Q4 start when the input is 28 December 2020, the first day of fiscal week 53 in the
    /// <see cref="s_sunday53" /> provider.
    /// </summary>
    [TestMethod]
    public void GetQuarterStartDate_WhenDateOnlyIsInThe53rdWeek_ShouldReturnQ4StartDate()
    {
        Assert.AreEqual(
            new DateOnly(2020, 9, 27),
            s_sunday53.GetQuarterStartDate(new DateOnly(2020, 12, 28)));
    }

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> returns
    /// the Q1 start when the input is leap day (29 February 2020) within the <see cref="s_sunday53" />
    /// provider.
    /// </summary>
    [TestMethod]
    public void GetQuarterStartDate_WhenDateOnlyIsLeapDay_ShouldReturnQ1StartDate()
    {
        Assert.AreEqual(
            new DateOnly(2019, 12, 29),
            s_sunday53.GetQuarterStartDate(new DateOnly(2020, 2, 29)));
    }

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(int, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is outside the valid range.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    public void GetQuarterStartDate_WhenQuarterIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int quarter)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            s_sunday52.GetQuarterStartDate(quarter, Sunday52FiscalYear));
    }

}
