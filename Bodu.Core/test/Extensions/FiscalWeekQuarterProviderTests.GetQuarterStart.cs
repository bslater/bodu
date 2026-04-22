// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.GetQuarterStart.cs" company="PlaceholderCompany">
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
        // GetQuarterStart(int, int)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> returns the
        /// correct quarter start date for each quarter of each fiscal year across all four providers.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterStart_WhenCalledWithQuarterAndFiscalYear_ShouldReturnExpectedStartDate(
            FiscalWeekQuarterProvider provider,
            int quarter,
            int fiscalYear,
            DateTime expectedStart,
            DateTime _)
        {
            Assert.AreEqual(expectedStart, provider.GetQuarterStart(quarter, fiscalYear));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> returns a
        /// <see cref="DateTime" /> with <see cref="DateTimeKind.Unspecified" />.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenCalledWithQuarterAndFiscalYear_ShouldReturnUnspecifiedKind()
        {
            Assert.AreEqual(DateTimeKind.Unspecified, Sunday52.GetQuarterStart(1, Sunday52FiscalYear).Kind);
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
                var current = Sunday52.GetQuarterStart(q, Sunday52FiscalYear);
                var next = Sunday52.GetQuarterStart(q + 1, Sunday52FiscalYear);
                Assert.AreEqual(91, (next - current).Days, $"Gap between Q{q} and Q{q + 1} starts.");
            }
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is less than 1.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void GetQuarterStart_WhenQuarterIsBelowValidRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterStart(quarter, Sunday52FiscalYear));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int, int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is greater than 4.
        /// </summary>
        [TestMethod]
        [DataRow(5)]
        [DataRow(100)]
        public void GetQuarterStart_WhenQuarterIsAboveValidRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterStart(quarter, Sunday52FiscalYear));
        }

        // -----------------------------------------------------------------------
        // GetQuarterStart(int) — obsolete single-arg overload
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that the obsolete single-argument
        /// <see cref="FiscalWeekQuarterProvider.GetQuarterStart(int)" /> overload throws
        /// <see cref="NotSupportedException" />, because the provider no longer tracks a single fiscal
        /// year.
        /// </summary>
        [TestMethod]
#pragma warning disable CS0618 // intentional: we verify the obsolete overload still throws
        public void GetQuarterStart_ObsoleteSingleArgOverload_ShouldThrowNotSupportedException()
        {
            Assert.ThrowsExactly<NotSupportedException>(() => Sunday52.GetQuarterStart(1));
        }
#pragma warning restore CS0618

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
            int __,
            DateTime expectedStart,
            DateTime ___)
        {
            Assert.AreEqual(expectedStart, provider.GetQuarterStart(expectedStart));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> returns the
        /// correct quarter start date when the input is the last day of that quarter, across every
        /// quarter and every fixture provider.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterStart_WhenDateTimeIsLastDayOfQuarter_ShouldReturnStartOfThatQuarter(
            FiscalWeekQuarterProvider provider,
            int _,
            int __,
            DateTime expectedStart,
            DateTime expectedEnd)
        {
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
        /// 28 December 2020 is the first day of week 53.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenDateTimeIsInThe53rdWeek_ShouldReturnQ4StartDate()
        {
            Assert.AreEqual(new DateTime(2020, 9, 27), Sunday53.GetQuarterStart(new DateTime(2020, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> resolves a
        /// date prior to the anchor fiscal year into the preceding fiscal year and returns that year's
        /// Q4 start date.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenDateTimeIsBeforeAnchorFiscalYear_ShouldResolveToPriorFiscalYear()
        {
            // Dec 31, 2022 resolves into FY 2022 (starts Jan 2, 2022) at its Q4 boundary.
            // FY 2022 Q4 starts Jan 2 + 273 days = Oct 2, 2022.
            Assert.AreEqual(new DateTime(2022, 10, 2), Sunday52.GetQuarterStart(new DateTime(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> resolves a
        /// date after the anchor fiscal year into the following fiscal year and returns that year's
        /// Q1 start date.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenDateTimeIsAfterAnchorFiscalYear_ShouldResolveToNextFiscalYear()
        {
            // Dec 31, 2023 = the first day of FY 2024 under Sunday52 (nearest Sunday to Jan 1, 2024).
            Assert.AreEqual(new DateTime(2023, 12, 31), Sunday52.GetQuarterStart(new DateTime(2023, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterStart(DateTime)" /> resolves a
        /// date prior to the April-anchored fiscal year into the preceding fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarterStart_WhenDateTimeIsBeforeAprilAnchorFiscalYear_ShouldResolveToPriorFiscalYear()
        {
            // Mar 31, 2023 = Friday. FY 2022 under Saturday52 starts Apr 2, 2022, 52 weeks; Q4 starts
            // Apr 2 + 273 days = Dec 31, 2022.
            Assert.AreEqual(new DateTime(2022, 12, 31), Saturday52.GetQuarterStart(new DateTime(2023, 3, 31)));
        }
    }
}
