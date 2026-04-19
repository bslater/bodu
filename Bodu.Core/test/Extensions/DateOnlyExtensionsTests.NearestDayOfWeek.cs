// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.NearestDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    public partial class DateOnlyExtensionsTests
    {
        // =========================================================================
        // NearestDayOfWeek(this DateOnly, DayOfWeek)
        // =========================================================================

        /// <summary>
        /// Provides (reference date, target day-of-week, expected nearest date) tuples.
        /// April 17 2024 is a Wednesday.
        /// </summary>
        public static IEnumerable<object[]> NearestDayOfWeekTestData()
        {
            // Wednesday 17 April 2024, target Wednesday → same day.
            yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Wednesday, new DateOnly(2024, 4, 17) };
            // Wednesday target Tuesday → 16 April (1 day back).
            yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Tuesday, new DateOnly(2024, 4, 16) };
            // Wednesday target Thursday → 18 April (1 day forward).
            yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Thursday, new DateOnly(2024, 4, 18) };
            // Wednesday target Sunday → Sunday 14 is 3 days back, Sunday 21 is 4 forward → earlier wins.
            yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Sunday, new DateOnly(2024, 4, 14) };
            // Wednesday target Saturday → Sat 20 is 3 forward, Sat 13 is 4 back → forward wins.
            yield return new object[] { new DateOnly(2024, 4, 17), DayOfWeek.Saturday, new DateOnly(2024, 4, 20) };
        }

        [TestMethod]
        [DynamicData(nameof(NearestDayOfWeekTestData), DynamicDataSourceType.Method)]
        public void NearestDayOfWeek_WhenCalled_ShouldReturnExpectedDate(DateOnly date, DayOfWeek dayOfWeek, DateOnly expected)
        {
            DateOnly actual = date.NearestDayOfWeek(dayOfWeek);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NearestDayOfWeek_WhenTiedBetweenPastAndFuture_ShouldReturnEarlierDate()
        {
            // Wednesday 17 April 2024, target Sunday: Sun 14 (3 days back) vs Sun 21 (4 days fwd) — back wins.
            DateOnly date = new DateOnly(2024, 4, 17);
            DateOnly actual = date.NearestDayOfWeek(DayOfWeek.Sunday);
            Assert.AreEqual(new DateOnly(2024, 4, 14), actual);
        }

        [TestMethod]
        public void NearestDayOfWeek_WhenDayOfWeekIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateOnly date = new DateOnly(2024, 4, 17);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = date.NearestDayOfWeek((DayOfWeek)999);
            });
        }

        // =========================================================================
        // NearestDayOfWeek(int year, int month, int day, DayOfWeek)
        // =========================================================================

        [TestMethod]
        public void NearestDayOfWeek_WhenUsingYearMonthDay_ShouldReturnExpectedDate()
        {
            // Wed 17 Apr 2024 → Monday 15 Apr (2 days back) is nearer than Mon 22 (5 fwd).
            DateOnly actual = DateOnlyExtensions.NearestDayOfWeek(2024, 4, 17, DayOfWeek.Monday);
            Assert.AreEqual(new DateOnly(2024, 4, 15), actual);
        }

        [TestMethod]
        public void NearestDayOfWeek_WhenUsingYearMonthDayWithInvalidDayOfWeek_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateOnlyExtensions.NearestDayOfWeek(2024, 4, 17, (DayOfWeek)99);
            });
        }
    }
}
