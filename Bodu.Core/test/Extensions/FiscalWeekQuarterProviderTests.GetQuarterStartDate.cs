// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests_GetQuarterStartDate.cs" company="PlaceholderCompany">
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
        // GetQuarterStartDate(int)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(int)" /> returns a
        /// <see cref="DateOnly" /> consistent with <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int)" />
        /// for each quarter across all four providers.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterStartDate_WhenCalledWithQuarterNumber_ShouldMatchGetQuarterStartConvertedToDateOnly(
            FiscalWeekQuarterProvider provider,
            int quarter,
            DateTime _,
            DateTime __)
        {
            Assert.AreEqual(
                DateOnly.FromDateTime(provider.GetQuarterStart(quarter)),
                provider.GetQuarterStartDate(quarter));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is outside the valid range.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(5)]
        public void GetQuarterStartDate_WhenQuarterIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterStartDate(quarter));
        }

        // -----------------------------------------------------------------------
        // GetQuarterStartDate(DateOnly)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> returns
        /// the correct quarter start when the input is the last day of each quarter, for Q1 through Q3.
        /// Q4 last-day inputs are skipped because those dates fall in the validation dead zone
        /// (see class-level remarks).
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterStartDate_WhenCalledWithLastDayOfQuarter_ShouldReturnExpectedStartDate(
            FiscalWeekQuarterProvider provider,
            int quarter,
            DateTime expectedStart,
            DateTime expectedEnd)
        {
            if (quarter == 4) return; // Q4 last day is in the validation dead zone; see class remarks.

            Assert.AreEqual(
                DateOnly.FromDateTime(expectedStart),
                provider.GetQuarterStartDate(DateOnly.FromDateTime(expectedEnd)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> returns
        /// the Q1 start when the input is leap day (29 February 2020) within the <see cref="Sunday53" />
        /// provider.
        /// </summary>
        [TestMethod]
        public void GetQuarterStartDate_WhenDateOnlyIsLeapDay_ShouldReturnQ1StartDate()
        {
            Assert.AreEqual(
                new DateOnly(2019, 12, 29),
                Sunday53.GetQuarterStartDate(new DateOnly(2020, 2, 29)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> returns
        /// the Q4 start when the input is 28 December 2020, the first day of fiscal week 53 in the
        /// <see cref="Sunday53" /> provider (a Sunday, always valid).
        /// </summary>
        [TestMethod]
        public void GetQuarterStartDate_WhenDateOnlyIsInThe53rdWeek_ShouldReturnQ4StartDate()
        {
            Assert.AreEqual(
                new DateOnly(2020, 9, 27),
                Sunday53.GetQuarterStartDate(new DateOnly(2020, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls before the fiscal year start.
        /// </summary>
        [TestMethod]
        public void GetQuarterStartDate_WhenDateOnlyIsBeforeFiscalYearStart_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterStartDate(new DateOnly(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStartDate(DateOnly)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls after the fiscal year end.
        /// </summary>
        [TestMethod]
        public void GetQuarterStartDate_WhenDateOnlyIsAfterFiscalYearEnd_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterStartDate(new DateOnly(2023, 12, 31)));
        }
    }
}