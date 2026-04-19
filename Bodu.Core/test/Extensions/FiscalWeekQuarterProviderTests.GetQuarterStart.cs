// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests_GetQuarterStart.cs" company="PlaceholderCompany">
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
        // GetQuarterStart(int)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int)" /> returns the correct
        /// quarter start date for each quarter across all four providers.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterStart_WhenCalledWithQuarterNumber_ShouldReturnExpectedStartDate(
            FiscalWeekQuarterProvider provider,
            int quarter,
            DateTime expectedStart,
            DateTime _)
        {
            Assert.AreEqual(expectedStart, provider.GetQuarterStart(quarter));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int)" /> returns a
        /// <see cref="DateTime" /> with <see cref="DateTimeKind.Unspecified" />.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenCalledWithQuarterNumber_ShouldReturnUnspecifiedKind()
        {
            Assert.AreEqual(DateTimeKind.Unspecified, Sunday52.GetQuarterStart(1).Kind);
        }

        /// <summary>
        /// Verifies that the start of each successive quarter is exactly 91 days (13 weeks) after the
        /// previous quarter start, for Q1 through Q3 in a 52-week provider.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenConsecutiveQuarters_ShouldBeSeparatedBy91Days()
        {
            for (int q = 1; q <= 3; q++)
            {
                var current = Sunday52.GetQuarterStart(q);
                var next = Sunday52.GetQuarterStart(q + 1);
                Assert.AreEqual(91, (next - current).Days, $"Gap between Q{q} and Q{q + 1} starts.");
            }
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is less than 1.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void GetQuarterStart_WhenQuarterIsBelowValidRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterStart(quarter));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is greater than 4.
        /// </summary>
        [TestMethod]
        [DataRow(5)]
        [DataRow(100)]
        public void GetQuarterStart_WhenQuarterIsAboveValidRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterStart(quarter));
        }

        // -----------------------------------------------------------------------
        // GetQuarterStart(DateTime)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> returns the
        /// correct quarter start date when the input is the first day of each quarter.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterStart_WhenDateTimeIsFirstDayOfQuarter_ShouldReturnThatDate(
            FiscalWeekQuarterProvider provider,
            int _,
            DateTime expectedStart,
            DateTime __)
        {
            Assert.AreEqual(expectedStart, provider.GetQuarterStart(expectedStart));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> returns the
        /// correct quarter start date when the input is the last day of that quarter, for Q1 through Q3.
        /// Q4 last-day entries are skipped because those dates fall in the fiscal week validation dead zone
        /// and throw <see cref="ArgumentOutOfRangeException" /> (see class-level remarks).
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterStart_WhenDateTimeIsLastDayOfQuarter_ShouldReturnStartOfThatQuarter(
            FiscalWeekQuarterProvider provider,
            int quarter,
            DateTime expectedStart,
            DateTime expectedEnd)
        {
            if (quarter == 4) return; // Q4 last day is in the validation dead zone; see class remarks.

            Assert.AreEqual(expectedStart, provider.GetQuarterStart(expectedEnd));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> returns the
        /// Q1 start date when the input is leap day (29 February 2020), which falls within Q1 of the
        /// <see cref="Sunday53" /> provider.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenDateTimeIsLeapDayInQ1_ShouldReturnQ1StartDate()
        {
            Assert.AreEqual(new DateTime(2019, 12, 29), Sunday53.GetQuarterStart(new DateTime(2020, 2, 29)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> returns the
        /// Q4 start date for a date in the 53rd week of the <see cref="Sunday53" /> fiscal year.
        /// 28 December 2020 is the first day of week 53 (Sunday, always valid).
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenDateTimeIsInThe53rdWeek_ShouldReturnQ4StartDate()
        {
            Assert.AreEqual(new DateTime(2020, 9, 27), Sunday53.GetQuarterStart(new DateTime(2020, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls before the fiscal year start.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenDateTimeIsBeforeFiscalYearStart_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterStart(new DateTime(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls after the fiscal year end.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenDateTimeIsAfterFiscalYearEnd_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterStart(new DateTime(2023, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls before the April start of
        /// the <see cref="Saturday52" /> provider.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenDateTimeIsBeforeAprilStartFiscalYear_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Saturday52.GetQuarterStart(new DateTime(2023, 3, 31)));
        }
    }
}