// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests_GetQuarter.cs" company="PlaceholderCompany">
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
        // GetQuarter(DateTime)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> returns the correct
        /// quarter number for mid-quarter, first-day, and near-end-of-quarter dates across all four
        /// providers, including leap days and dates that span calendar year boundaries.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterNumberTestData), DynamicDataSourceType.Method)]
        public void GetQuarter_WhenDateTimeIsWithinFiscalYear_ShouldReturnExpectedQuarter(
            FiscalWeekQuarterProvider provider,
            DateTime date,
            int expectedQuarter)
        {
            Assert.AreEqual(expectedQuarter, provider.GetQuarter(date));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> returns 4 for a
        /// date in the 53rd week of a 53-week fiscal year. 28 December 2020 is the first day of fiscal
        /// week 53 in <see cref="Sunday53" /> (364 days from the FY start) and is always valid.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateIsInThe53rdWeek_ShouldReturnQuarter4()
        {
            // Dec 28, 2020 = Sunday, 364 days after Dec 29, 2019. weeksFromStart = 52; quarter = 5 → clamped to 4.
            Assert.AreEqual(4, Sunday53.GetQuarter(new DateTime(2020, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls before the fiscal year start.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsBeforeFiscalYearStart_ShouldThrowException()
        {
            // Dec 31, 2022 is one week before the Sunday52 fiscal year that starts Jan 1, 2023.
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarter(new DateTime(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls after the fiscal year end.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsAfterFiscalYearEnd_ShouldThrowException()
        {
            // Dec 31, 2023 is the first day of the next fiscal year for Sunday52 (which starts on that Sunday).
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarter(new DateTime(2023, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date is the first day of the fiscal year
        /// that follows the 53-week <see cref="Sunday53" /> provider.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsFirstDayOfNextFiscalYearIn53WeekYear_ShouldThrowException()
        {
            // Jan 3, 2021 is the next fiscal year start for Sunday53 (nearest Sunday to Jan 1, 2021).
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday53.GetQuarter(new DateTime(2021, 1, 3)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls before the fiscal year start in
        /// a provider whose fiscal year begins in a prior calendar year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsBeforeFiscalYearStartAcrossCalendarYear_ShouldThrowException()
        {
            // Dec 28, 2019 is one week before the Sunday53 fiscal year that starts Dec 29, 2019.
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday53.GetQuarter(new DateTime(2019, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls after the fiscal year end in
        /// the <see cref="Saturday52" /> provider, whose fiscal year ends in March of the following year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsAfterFiscalYearEndAcrossCalendarYear_ShouldThrowException()
        {
            // Mar 30, 2024 is the first day of the fiscal year following Saturday52.
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Saturday52.GetQuarter(new DateTime(2024, 3, 30)));
        }

        // -----------------------------------------------------------------------
        // GetQuarter(DateOnly)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateOnly)" /> returns the same
        /// quarter number as the <see cref="DateTime" /> overload for equivalent date values across all
        /// four providers.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterNumberTestData), DynamicDataSourceType.Method)]
        public void GetQuarter_WhenCalledWithDateOnly_ShouldReturnSameResultAsDateTimeOverload(
            FiscalWeekQuarterProvider provider,
            DateTime date,
            int expectedQuarter)
        {
            Assert.AreEqual(expectedQuarter, provider.GetQuarter(DateOnly.FromDateTime(date)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateOnly)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls before the fiscal year start.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateOnlyIsBeforeFiscalYearStart_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                Sunday52.GetQuarter(new DateOnly(2022, 12, 31));
            });
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateOnly)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls after the fiscal year end.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateOnlyIsAfterFiscalYearEnd_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                Sunday52.GetQuarter(new DateOnly(2023, 12, 31));
            });
        }
    }
}