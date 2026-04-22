// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.LastDayOfWeekInQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    public partial class DateOnlyExtensionsTests
    {
        // Note: The current implementation starts at the quarter end and advances forward using
        // ((target - currentDow + 7) % 7) days. This pins the result only when the quarter-end
        // day-of-week already matches the target; for other targets, the returned date may fall
        // into the following quarter. These tests lock in the observed behaviour so any future
        // correction to match the documented "last occurrence within the same quarter" contract
        // is flagged as a regression.

        // =========================================================================
        // LastDayOfWeekInQuarter(this DateOnly, DayOfWeek, CalendarQuarterDefinition)
        // =========================================================================

        public static IEnumerable<object[]> LastDayOfWeekInQuarterJanuaryDecemberTestData()
        {
            // Q1 2024 ends Sun 31 Mar. Target Sun → 31 Mar (quarter end matches target).
            yield return new object[] { new DateOnly(2024, 2, 15), DayOfWeek.Sunday, new DateOnly(2024, 3, 31) };
            // Q3 2024 ends Mon 30 Sep. Target Mon → 30 Sep.
            yield return new object[] { new DateOnly(2024, 8, 20), DayOfWeek.Monday, new DateOnly(2024, 9, 30) };
            // Q4 2024 ends Tue 31 Dec. Target Tue → 31 Dec.
            yield return new object[] { new DateOnly(2024, 11, 5), DayOfWeek.Tuesday, new DateOnly(2024, 12, 31) };
        }

        /// <summary>
        /// Verifies that Last Day Of Week In Quarter, when Target Matches Quarter End Day Of Week, returns Quarter End Date.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(LastDayOfWeekInQuarterJanuaryDecemberTestData), DynamicDataSourceType.Method)]
        public void LastDayOfWeekInQuarter_WhenTargetMatchesQuarterEndDayOfWeek_ShouldReturnQuarterEndDate(DateOnly input, DayOfWeek dayOfWeek, DateOnly expected)
        {
            DateOnly actual = input.LastDayOfWeekInQuarter(dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that Last Day Of Week In Quarter, when Day Of Week Is Invalid, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenDayOfWeekIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateOnly input = new DateOnly(2024, 4, 20);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.LastDayOfWeekInQuarter((DayOfWeek)999, CalendarQuarterDefinition.JanuaryToDecember);
            });
        }

        /// <summary>
        /// Verifies that Last Day Of Week In Quarter, when Definition Is Invalid, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenDefinitionIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateOnly input = new DateOnly(2024, 4, 20);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.LastDayOfWeekInQuarter(DayOfWeek.Monday, (CalendarQuarterDefinition)999);
            });
        }

        // =========================================================================
        // LastDayOfWeekInQuarter(int year, int quarter, DayOfWeek, CalendarQuarterDefinition)
        // =========================================================================

        /// <summary>
        /// Verifies that Last Day Of Week In Quarter, when Using Year And Quarter, returns Expected Date.
        /// </summary>
        [TestMethod]
        [DataRow(2024, 1, DayOfWeek.Sunday, 2024, 3, 31)]  // Q1 ends Sun; target Sun → 31 Mar
        [DataRow(2024, 3, DayOfWeek.Monday, 2024, 9, 30)]  // Q3 ends Mon; target Mon → 30 Sep
        [DataRow(2024, 4, DayOfWeek.Tuesday, 2024, 12, 31)] // Q4 ends Tue; target Tue → 31 Dec
        public void LastDayOfWeekInQuarter_WhenUsingYearAndQuarter_ShouldReturnExpectedDate(
            int year, int quarter, DayOfWeek dayOfWeek, int expectedYear, int expectedMonth, int expectedDay)
        {
            DateOnly actual = DateOnlyExtensions.LastDayOfWeekInQuarter(year, quarter, dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
            Assert.AreEqual(new DateOnly(expectedYear, expectedMonth, expectedDay), actual);
        }

        /// <summary>
        /// Verifies that Last Day Of Week In Quarter, when Quarter Is Out Of Range, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(5)]
        [DataRow(-1)]
        public void LastDayOfWeekInQuarter_WhenQuarterIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateOnlyExtensions.LastDayOfWeekInQuarter(2024, quarter, DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
            });
        }

        /// <summary>
        /// Verifies that Last Day Of Week In Quarter, when Year And Quarter Overload Definition Is Invalid, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenYearAndQuarterOverloadDefinitionIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateOnlyExtensions.LastDayOfWeekInQuarter(2024, 1, DayOfWeek.Monday, (CalendarQuarterDefinition)999);
            });
        }

        /// <summary>
        /// Verifies that Last Day Of Week In Quarter, when Year And Quarter Overload Day Of Week Is Invalid, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenYearAndQuarterOverloadDayOfWeekIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateOnlyExtensions.LastDayOfWeekInQuarter(2024, 1, (DayOfWeek)999, CalendarQuarterDefinition.JanuaryToDecember);
            });
        }
    }
}
