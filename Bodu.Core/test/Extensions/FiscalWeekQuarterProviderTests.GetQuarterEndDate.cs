// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests_GetQuarterEndDate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    public partial class FiscalWeekQuarterProviderTests
    {
        // -----------------------------------------------------------------------
        // GetQuarterEndDate(int)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(int)" /> returns a result
        /// equal to <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int)" /> converted to
        /// <see cref="DateOnly" />, for all quarters and all providers.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEndDate_WhenCalledWithQuarterNumber_ShouldMatchGetQuarterEndConvertedToDateOnly(
            FiscalWeekQuarterProvider provider,
            int quarter,
            DateTime _,
            DateTime __)
        {
            Assert.AreEqual(
                DateOnly.FromDateTime(provider.GetQuarterEnd(quarter)),
                provider.GetQuarterEndDate(quarter));
        }

        /// <summary>
        /// Verifies that the Q4 end date for <see cref="Sunday53" /> is 2 January 2021, confirming the
        /// 53rd week end is correctly represented as a <see cref="DateOnly" />.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenFiscalYearIs53Weeks_Q4ShouldEndOnCorrectDate()
        {
            Assert.AreEqual(new DateOnly(2021, 1, 2), Sunday53.GetQuarterEndDate(4));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is outside the valid range.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(5)]
        public void GetQuarterEndDate_WhenQuarterIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEndDate(quarter));
        }

        // -----------------------------------------------------------------------
        // GetQuarterEndDate(DateOnly)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> returns the
        /// correct quarter end date when the input is the first day of each quarter, for all four providers.
        /// First days are always the fiscal week start day and are always valid.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEndDate_WhenCalledWithFirstDayOfQuarter_ShouldReturnExpectedEndDate(
            FiscalWeekQuarterProvider provider,
            int quarter,
            DateTime expectedStart,
            DateTime expectedEnd)
        {
            Assert.AreEqual(
                DateOnly.FromDateTime(expectedEnd),
                provider.GetQuarterEndDate(DateOnly.FromDateTime(expectedStart)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> and
        /// <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(int)" /> return the same result when
        /// supplied with the first day of each quarter.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEndDate_WhenCalledWithFirstDayOfQuarter_ShouldMatchIntOverload(
            FiscalWeekQuarterProvider provider,
            int quarter,
            DateTime expectedStart,
            DateTime _)
        {
            Assert.AreEqual(
                provider.GetQuarterEndDate(quarter),
                provider.GetQuarterEndDate(DateOnly.FromDateTime(expectedStart)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> returns the
        /// Q1 end date for leap day (29 February 2020) in the <see cref="Sunday53" /> provider.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsLeapDay_ShouldReturnQ1EndDate()
        {
            Assert.AreEqual(new DateOnly(2020, 3, 28), Sunday53.GetQuarterEndDate(new DateOnly(2020, 2, 29)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> returns the
        /// Q4 end date (2 January 2021) for a date in the 53rd week of the <see cref="Sunday53" />
        /// provider. 28 December 2020 (first day of week 53) is used as the input.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsInThe53rdWeek_ShouldReturnQ4EndDate()
        {
            Assert.AreEqual(new DateOnly(2021, 1, 2), Sunday53.GetQuarterEndDate(new DateOnly(2020, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> returns the
        /// correct Q4 end date for a mid-Q4 date in the <see cref="Saturday52" /> provider, which spans
        /// into February 2024 (a leap month).
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsInQ4OfLeapYearProvider_ShouldReturnCorrectEndDate()
        {
            Assert.AreEqual(new DateOnly(2024, 3, 29), Saturday52.GetQuarterEndDate(new DateOnly(2024, 2, 14)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls before the fiscal year start.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsBeforeFiscalYearStart_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEndDate(new DateOnly(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls after the fiscal year end.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsAfterFiscalYearEnd_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEndDate(new DateOnly(2023, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls before the
        /// <see cref="Sunday53" /> fiscal year, which begins in December of the prior calendar year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsBeforeFiscalYearStartAcrossCalendarYear_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday53.GetQuarterEndDate(new DateOnly(2019, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> for the first day of the next fiscal year following
        /// the 53-week <see cref="Sunday53" /> provider.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsFirstDayOfNextFiscalYearIn53WeekYear_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday53.GetQuarterEndDate(new DateOnly(2021, 1, 3)));
        }
    }
}