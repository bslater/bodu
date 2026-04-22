// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.GetQuarter.cs" company="PlaceholderCompany">
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
        /// week 53 in <see cref="Sunday53" /> (364 days from the FY start).
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateIsInThe53rdWeek_ShouldReturnQuarter4()
        {
            // Dec 28, 2020 = Sunday, 364 days after Dec 29, 2019. weeksFromStart = 52; quarter = 5 → clamped to 4.
            Assert.AreEqual(4, Sunday53.GetQuarter(new DateTime(2020, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> resolves a date
        /// that falls before the anchor fiscal year into the preceding fiscal year and returns its
        /// quarter number from that year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsBeforeAnchorFiscalYear_ShouldResolveToPriorFiscalYear()
        {
            // Dec 31, 2022 = Saturday. Under Sunday52 the week starts Dec 25, 2022, which sits in FY 2022
            // (starting Jan 2, 2022). FY 2022 is a 52-week year, so this date is in its Q4.
            Assert.AreEqual(4, Sunday52.GetQuarter(new DateTime(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> resolves a date
        /// that falls after the anchor fiscal year into the following fiscal year and returns its
        /// quarter number from that year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsAfterAnchorFiscalYear_ShouldResolveToNextFiscalYear()
        {
            // Dec 31, 2023 = Sunday; this is the first day of FY 2024 under Sunday52 (nearest Sunday to Jan 1, 2024).
            Assert.AreEqual(1, Sunday52.GetQuarter(new DateTime(2023, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> maps the first day
        /// of the next fiscal year following a 53-week year into Q1 of that next fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsFirstDayOfNextFiscalYearAfter53WeekYear_ShouldReturnQuarter1()
        {
            // Jan 3, 2021 = Sunday — the first day of FY 2021 under Sunday53.
            Assert.AreEqual(1, Sunday53.GetQuarter(new DateTime(2021, 1, 3)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> resolves a date
        /// before the anchor fiscal year in a provider whose fiscal year begins in the prior calendar
        /// year into the correct preceding fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsBeforeAnchorFiscalYearAcrossCalendarYear_ShouldResolveToPriorFiscalYear()
        {
            // Dec 28, 2019 = Saturday. FY 2020 under Sunday53 begins Dec 29, 2019, so Dec 28, 2019 sits
            // in FY 2019 (begins Dec 30, 2018, 52 weeks) at its Q4.
            Assert.AreEqual(4, Sunday53.GetQuarter(new DateTime(2019, 12, 28)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> maps the first day
        /// of the next fiscal year in the <see cref="Saturday52" /> provider (whose fiscal year ends in
        /// March) into Q1 of that fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsAfterAnchorFiscalYearAcrossCalendarYear_ShouldResolveToNextFiscalYear()
        {
            // Mar 30, 2024 = Saturday — the first day of FY 2024 under Saturday52.
            Assert.AreEqual(1, Saturday52.GetQuarter(new DateTime(2024, 3, 30)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateTime)" /> resolves a date in
        /// the calendar year preceding the anchor month of a fiscal-year-end provider into the correct
        /// fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateTimeIsInPriorCalendarYearOfFiscalYearEndProvider_ShouldResolveToPriorFiscalYear()
        {
            // Provider: fiscal year ends in January; so FY 2024 begins Feb 2024 and FY 2023 ends Jan 2024.
            // Jan 15, 2024 must resolve to FY 2023.
            var provider = new FiscalWeekQuarterProvider(
                month: 1,
                dayOfWeek: DayOfWeek.Saturday,
                isFiscalYearEnd: true,
                useNearestDayOfWeek: true);

            int expectedQuarter = provider.GetQuarter(new DateTime(2024, 1, 15));
            Assert.AreEqual(4, expectedQuarter);
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
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateOnly)" /> resolves a date
        /// before the anchor fiscal year into the preceding fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateOnlyIsBeforeAnchorFiscalYear_ShouldResolveToPriorFiscalYear()
        {
            Assert.AreEqual(4, Sunday52.GetQuarter(new DateOnly(2022, 12, 31)));
        }

        /// <summary>
        /// Verifies that <see cref="FiscalWeekQuarterProvider.GetQuarter(DateOnly)" /> resolves a date
        /// after the anchor fiscal year into the following fiscal year.
        /// </summary>
        [TestMethod]
        public void GetQuarter_WhenDateOnlyIsAfterAnchorFiscalYear_ShouldResolveToNextFiscalYear()
        {
            Assert.AreEqual(1, Sunday52.GetQuarter(new DateOnly(2023, 12, 31)));
        }
    }
}
