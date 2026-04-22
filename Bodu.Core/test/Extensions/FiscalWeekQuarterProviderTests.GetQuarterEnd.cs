// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.GetQuarterEnd.cs" company="PlaceholderCompany">
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
        // GetQuarterEnd(int, int)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int, int)" /> returns the
        /// correct quarter end date for each quarter of each fiscal year across all four providers.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEnd_WhenCalledWithQuarterAndFiscalYear_ShouldReturnExpectedEndDate(
            FiscalWeekQuarterProvider provider,
            int quarter,
            int fiscalYear,
            DateTime _,
            DateTime expectedEnd)
        {
            Assert.AreEqual(expectedEnd, provider.GetQuarterEnd(quarter, fiscalYear));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int, int)" /> returns a
        /// <see cref="DateTime" /> with <see cref="DateTimeKind.Unspecified" />.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenCalledWithQuarterAndFiscalYear_ShouldReturnUnspecifiedKind()
        {
            Assert.AreEqual(DateTimeKind.Unspecified, Sunday52.GetQuarterEnd(1, Sunday52FiscalYear).Kind);
        }

        /// <summary>
        /// Verifies that Q4 spans exactly 13 weeks (91 days) in a 52-week fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenFiscalYearIs52Weeks_Q4ShouldSpan13Weeks()
        {
            var start = Sunday52.GetQuarterStart(4, Sunday52FiscalYear);
            var end = Sunday52.GetQuarterEnd(4, Sunday52FiscalYear);
            Assert.AreEqual(91, (end - start).Days + 1, "Q4 must span 91 days (13 weeks) in a 52-week year.");
        }

        /// <summary>
        /// Verifies that Q4 spans exactly 14 weeks (98 days) in a 53-week fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenFiscalYearIs53Weeks_Q4ShouldSpan14Weeks()
        {
            var start = Sunday53.GetQuarterStart(4, Sunday53FiscalYear);
            var end = Sunday53.GetQuarterEnd(4, Sunday53FiscalYear);
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
            var start = Sunday53.GetQuarterStart(quarter, Sunday53FiscalYear);
            var end = Sunday53.GetQuarterEnd(quarter, Sunday53FiscalYear);
            Assert.AreEqual(91, (end - start).Days + 1, $"Q{quarter} must span 91 days in a 53-week year.");
        }

        /// <summary>
        /// Verifies that the end of each quarter is exactly one day before the start of the next quarter
        /// for Q1 through Q3, across all four providers.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenConsecutiveQuarters_EndShouldBeOneDayBeforeNextStart()
        {
            var fixtures = new (FiscalWeekQuarterProvider Provider, int FiscalYear)[]
            {
                (Sunday52, Sunday52FiscalYear),
                (Monday52Leap, Monday52LeapFiscalYear),
                (Sunday53, Sunday53FiscalYear),
                (Saturday52, Saturday52FiscalYear),
            };

            foreach (var (provider, fiscalYear) in fixtures)
            {
                for (int q = 1; q <= 3; q++)
                {
                    var end = provider.GetQuarterEnd(q, fiscalYear);
                    var nextStart = provider.GetQuarterStart(q + 1, fiscalYear);
                    Assert.AreEqual(1, (nextStart - end).Days,
                        $"Q{q} end must be one day before Q{q + 1} start.");
                }
            }
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int, int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is less than 1.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void GetQuarterEnd_WhenQuarterIsBelowValidRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEnd(quarter, Sunday52FiscalYear));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int, int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is greater than 4.
        /// </summary>
        [TestMethod]
        [DataRow(5)]
        [DataRow(100)]
        public void GetQuarterEnd_WhenQuarterIsAboveValidRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEnd(quarter, Sunday52FiscalYear));
        }

        // -----------------------------------------------------------------------
        // GetQuarterEnd(int) — obsolete single-arg overload
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that the obsolete single-argument
        /// <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int)" /> overload throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        [TestMethod]
#pragma warning disable CS0618 // intentional: we verify the obsolete overload still throws
        public void GetQuarterEnd_ObsoleteSingleArgOverload_ShouldThrowNotSupportedException()
        {
            Assert.ThrowsExactly<NotSupportedException>(() => Sunday52.GetQuarterEnd(1));
        }
#pragma warning restore CS0618

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
            int __,
            DateTime expectedStart,
            DateTime expectedEnd)
        {
            Assert.AreEqual(expectedEnd, provider.GetQuarterEnd(expectedStart));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> returns the
        /// same end date when the input is the last day of that quarter, across every quarter and every
        /// fixture provider.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEnd_WhenDateTimeIsLastDayOfQuarter_ShouldReturnThatDate(
            FiscalWeekQuarterProvider provider,
            int _,
            int __,
            DateTime ___,
            DateTime expectedEnd)
        {
            Assert.AreEqual(expectedEnd, provider.GetQuarterEnd(expectedEnd));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> returns the
        /// Q4 end date (2 January 2021) for a date in the 53rd week of <see cref="Sunday53" />.
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
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> resolves a date
        /// before the anchor fiscal year into the preceding fiscal year and returns that year's Q4 end.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenDateTimeIsBeforeAnchorFiscalYear_ShouldResolveToPriorFiscalYear()
        {
            // Dec 31, 2022 resolves into FY 2022 (Jan 2, 2022 – Dec 31, 2022); Q4 ends Dec 31, 2022.
            Assert.AreEqual(new DateTime(2022, 12, 31), Sunday52.GetQuarterEnd(new DateTime(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> resolves a date
        /// after the anchor fiscal year into the following fiscal year and returns that year's Q1 end.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenDateTimeIsAfterAnchorFiscalYear_ShouldResolveToNextFiscalYear()
        {
            // Dec 31, 2023 = the first day of FY 2024 under Sunday52; Q1 of FY 2024 ends Mar 30, 2024.
            Assert.AreEqual(new DateTime(2024, 3, 30), Sunday52.GetQuarterEnd(new DateTime(2023, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(DateTime)" /> maps the first
        /// day of the next fiscal year following a 53-week year into Q1 end of that next year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEnd_WhenDateTimeIsFirstDayOfNextFiscalYearAfter53WeekYear_ShouldReturnQ1EndOfNextYear()
        {
            // Jan 3, 2021 = Sunday — first day of FY 2021 under Sunday53; Q1 ends Apr 3, 2021.
            Assert.AreEqual(new DateTime(2021, 4, 3), Sunday53.GetQuarterEnd(new DateTime(2021, 1, 3)));
        }
    }
}
