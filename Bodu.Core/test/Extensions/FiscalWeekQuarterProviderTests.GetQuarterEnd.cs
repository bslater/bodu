// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests_GetQuarterEnd.cs" company="PlaceholderCompany">
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
        // GetQuarterEnd(int)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int)" /> returns the correct
        /// quarter end date for each quarter across all four providers.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEnd_WhenCalledWithQuarterNumber_ShouldReturnExpectedEndDate(
            FiscalWeekQuarterProvider provider,
            int quarter,
            DateTime _,
            DateTime expectedEnd)
        {
            Assert.AreEqual(expectedEnd, provider.GetQuarterEnd(quarter));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int)" /> returns a
        /// <see cref="DateTime" /> with <see cref="DateTimeKind.Unspecified" />.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenCalledWithQuarterNumber_ShouldReturnUnspecifiedKind()
        {
            Assert.AreEqual(DateTimeKind.Unspecified, Sunday52.GetQuarterEnd(1).Kind);
        }

        /// <summary>
        /// Verifies that Q4 spans exactly 13 weeks (91 days) in a 52-week fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenFiscalYearIs52Weeks_Q4ShouldSpan13Weeks()
        {
            var start = Sunday52.GetQuarterStart(4);
            var end = Sunday52.GetQuarterEnd(4);
            Assert.AreEqual(91, (end - start).Days + 1, "Q4 must span 91 days (13 weeks) in a 52-week year.");
        }

        /// <summary>
        /// Verifies that Q4 spans exactly 14 weeks (98 days) in a 53-week fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenFiscalYearIs53Weeks_Q4ShouldSpan14Weeks()
        {
            var start = Sunday53.GetQuarterStart(4);
            var end = Sunday53.GetQuarterEnd(4);
            Assert.AreEqual(98, (end - start).Days + 1, "Q4 must span 98 days (14 weeks) in a 53-week year.");
        }

        /// <summary>
        /// Verifies that Q1, Q2, and Q3 each span exactly 13 weeks (91 days) in the 53-week provider,
        /// confirming that the extra week is confined to Q4.
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void GetQuarterEnd_WhenFiscalYearIs53WeeksAndQuarterIs1To3_ShouldSpan13Weeks(int quarter)
        {
            var start = Sunday53.GetQuarterStart(quarter);
            var end = Sunday53.GetQuarterEnd(quarter);
            Assert.AreEqual(91, (end - start).Days + 1, $"Q{quarter} must span 91 days in a 53-week year.");
        }

        /// <summary>
        /// Verifies that the end of each quarter is exactly one day before the start of the next quarter
        /// for Q1 through Q3, across all four providers.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenConsecutiveQuarters_EndShouldBeOneDayBeforeNextStart()
        {
            foreach (var provider in new[] { Sunday52, Monday52Leap, Sunday53, Saturday52 })
            {
                for (int q = 1; q <= 3; q++)
                {
                    var end = provider.GetQuarterEnd(q);
                    var nextStart = provider.GetQuarterStart(q + 1);
                    Assert.AreEqual(1, (nextStart - end).Days,
                        $"Q{q} end must be one day before Q{q + 1} start.");
                }
            }
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is less than 1.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void GetQuarterEnd_WhenQuarterIsBelowValidRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEnd(quarter));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is greater than 4.
        /// </summary>
        [TestMethod]
        [DataRow(5)]
        [DataRow(100)]
        public void GetQuarterEnd_WhenQuarterIsAboveValidRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEnd(quarter));
        }

        // -----------------------------------------------------------------------
        // GetQuarterEnd(DateTime)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> returns the
        /// correct quarter end date when the input is the first day of each quarter.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEnd_WhenDateTimeIsFirstDayOfQuarter_ShouldReturnEndOfThatQuarter(
            FiscalWeekQuarterProvider provider,
            int _,
            DateTime expectedStart,
            DateTime expectedEnd)
        {
            Assert.AreEqual(expectedEnd, provider.GetQuarterEnd(expectedStart));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> returns the
        /// same end date when the input is the last day of that quarter, for Q1 through Q3.
        /// Q4 last-day entries are skipped because those dates fall in the validation dead zone
        /// (see class-level remarks).
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEnd_WhenDateTimeIsLastDayOfQuarter_ShouldReturnThatDate(
            FiscalWeekQuarterProvider provider,
            int quarter,
            DateTime __,
            DateTime expectedEnd)
        {
            if (quarter == 4) return; // Q4 last day is in the validation dead zone; see class remarks.

            Assert.AreEqual(expectedEnd, provider.GetQuarterEnd(expectedEnd));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> returns the
        /// Q4 end date (2 January 2021) for a date in the 53rd week of <see cref="Sunday53" />.
        /// 28 December 2020 is used — the first day of week 53, always valid.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenDateTimeIsInThe53rdWeek_ShouldReturnQ4EndDate()
        {
            Assert.AreEqual(new DateTime(2021, 1, 2), Sunday53.GetQuarterEnd(new DateTime(2020, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> returns the
        /// Q1 end date for leap day (29 February 2020) in the <see cref="Sunday53" /> provider.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenDateTimeIsLeapDayInQ1_ShouldReturnQ1EndDate()
        {
            Assert.AreEqual(new DateTime(2020, 3, 28), Sunday53.GetQuarterEnd(new DateTime(2020, 2, 29)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls before the fiscal year start.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenDateTimeIsBeforeFiscalYearStart_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEnd(new DateTime(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the date falls after the fiscal year end.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenDateTimeIsAfterFiscalYearEnd_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEnd(new DateTime(2023, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> for the first day of the next fiscal year in a
        /// 53-week provider, confirming the 371-day upper boundary is enforced.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenDateTimeIsFirstDayOfNextFiscalYearIn53WeekYear_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday53.GetQuarterEnd(new DateTime(2021, 1, 3)));
        }
    }
}