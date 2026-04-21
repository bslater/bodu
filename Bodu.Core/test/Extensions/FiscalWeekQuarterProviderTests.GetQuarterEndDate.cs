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
        // GetQuarterEndDate(int, int)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(int, int)" /> returns a
        /// result equal to <see cref="FiscalWeekQuarterProvider.GetQuarterEnd(int, int)" /> converted to
        /// <see cref="DateOnly" />, for all quarters, fiscal years, and providers.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEndDate_WhenCalledWithQuarterAndFiscalYear_ShouldMatchGetQuarterEndConvertedToDateOnly(
            FiscalWeekQuarterProvider provider,
            int quarter,
            int fiscalYear,
            DateTime _,
            DateTime __)
        {
            Assert.AreEqual(
                DateOnly.FromDateTime(provider.GetQuarterEnd(quarter, fiscalYear)),
                provider.GetQuarterEndDate(quarter, fiscalYear));
        }

        /// <summary>
        /// Verifies that the Q4 end date for <see cref="Sunday53" /> in fiscal year 2020 is 2 January 2021,
        /// confirming the 53rd week end is correctly represented as a <see cref="DateOnly" />.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenFiscalYearIs53Weeks_Q4ShouldEndOnCorrectDate()
        {
            Assert.AreEqual(new DateOnly(2021, 1, 2), Sunday53.GetQuarterEndDate(4, Sunday53FiscalYear));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(int, int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <c>quarter</c> is outside the valid range.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(5)]
        public void GetQuarterEndDate_WhenQuarterIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                Sunday52.GetQuarterEndDate(quarter, Sunday52FiscalYear));
        }

        // -----------------------------------------------------------------------
        // GetQuarterEndDate(int) — obsolete single-arg overload
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that the obsolete single-argument
        /// <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(int)" /> overload throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        [TestMethod]
#pragma warning disable CS0618 // intentional: we verify the obsolete overload still throws
        public void GetQuarterEndDate_ObsoleteSingleArgOverload_ShouldThrowNotSupportedException()
        {
            Assert.ThrowsExactly<NotSupportedException>(() => Sunday52.GetQuarterEndDate(1));
        }
#pragma warning restore CS0618

        // -----------------------------------------------------------------------
        // GetQuarterEndDate(DateOnly)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> returns the
        /// correct quarter end date when the input is the first day of each quarter.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEndDate_WhenCalledWithFirstDayOfQuarter_ShouldReturnExpectedEndDate(
            FiscalWeekQuarterProvider provider,
            int _,
            int __,
            DateTime expectedStart,
            DateTime expectedEnd)
        {
            Assert.AreEqual(
                DateOnly.FromDateTime(expectedEnd),
                provider.GetQuarterEndDate(DateOnly.FromDateTime(expectedStart)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> and
        /// <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(int, int)" /> return the same result
        /// when supplied with the first day of each quarter.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetQuarterBoundaryTestData), DynamicDataSourceType.Method)]
        public void GetQuarterEndDate_WhenCalledWithFirstDayOfQuarter_ShouldMatchIntOverload(
            FiscalWeekQuarterProvider provider,
            int quarter,
            int fiscalYear,
            DateTime expectedStart,
            DateTime _)
        {
            Assert.AreEqual(
                provider.GetQuarterEndDate(quarter, fiscalYear),
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
        /// provider.
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
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> resolves a
        /// date before the anchor fiscal year into the preceding fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsBeforeAnchorFiscalYear_ShouldResolveToPriorFiscalYear()
        {
            // Dec 31, 2022 → FY 2022 Q4 end = Dec 31, 2022.
            Assert.AreEqual(new DateOnly(2022, 12, 31), Sunday52.GetQuarterEndDate(new DateOnly(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> resolves a
        /// date after the anchor fiscal year into the following fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsAfterAnchorFiscalYear_ShouldResolveToNextFiscalYear()
        {
            // Dec 31, 2023 → FY 2024 Q1 end = Mar 30, 2024.
            Assert.AreEqual(new DateOnly(2024, 3, 30), Sunday52.GetQuarterEndDate(new DateOnly(2023, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> resolves a
        /// date prior to the 53-week fiscal year (<see cref="Sunday53" />) into the preceding fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsBeforeAnchorFiscalYearAcrossCalendarYear_ShouldResolveToPriorFiscalYear()
        {
            // Dec 28, 2019 resolves into FY 2019 (starts Dec 30, 2018, 52 weeks); Q4 end = Dec 28, 2019.
            Assert.AreEqual(new DateOnly(2019, 12, 28), Sunday53.GetQuarterEndDate(new DateOnly(2019, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarterEndDate(DateOnly)" /> maps the
        /// first day of the next fiscal year following the 53-week <see cref="Sunday53" /> provider into
        /// Q1 end of that next year.
        /// </summary>
        [TestMethod]
        public void GetQuarterEndDate_WhenDateOnlyIsFirstDayOfNextFiscalYearAfter53WeekYear_ShouldReturnQ1EndOfNextYear()
        {
            // Jan 3, 2021 = first day of FY 2021 under Sunday53; Q1 ends Apr 3, 2021.
            Assert.AreEqual(new DateOnly(2021, 4, 3), Sunday53.GetQuarterEndDate(new DateOnly(2021, 1, 3)));
        }
    }
}
