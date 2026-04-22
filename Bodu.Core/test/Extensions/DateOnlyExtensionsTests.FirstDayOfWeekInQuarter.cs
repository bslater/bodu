// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.FirstDayOfWeekInQuarter.cs" company="PlaceholderCompany">
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
        // FirstDayOfWeekInQuarter(this DateOnly, DayOfWeek, CalendarQuarterDefinition)
        // =========================================================================

        public static IEnumerable<object[]> FirstDayOfWeekInQuarterJanuaryDecemberTestData()
        {
            // (input date, target DayOfWeek, expected first occurrence)
            // Q1 2024 starts on Mon 1 Jan. First Monday = 1 Jan; first Friday = 5 Jan.
            yield return new object[] { new DateOnly(2024, 2, 15), DayOfWeek.Monday, new DateOnly(2024, 1, 1) };
            yield return new object[] { new DateOnly(2024, 2, 15), DayOfWeek.Friday, new DateOnly(2024, 1, 5) };
            // Q2 2024 starts Mon 1 Apr. First Monday = 1 Apr; first Sunday = 7 Apr.
            yield return new object[] { new DateOnly(2024, 5, 10), DayOfWeek.Monday, new DateOnly(2024, 4, 1) };
            yield return new object[] { new DateOnly(2024, 5, 10), DayOfWeek.Sunday, new DateOnly(2024, 4, 7) };
            // Q3 2024 starts Mon 1 Jul.
            yield return new object[] { new DateOnly(2024, 8, 20), DayOfWeek.Monday, new DateOnly(2024, 7, 1) };
            yield return new object[] { new DateOnly(2024, 8, 20), DayOfWeek.Wednesday, new DateOnly(2024, 7, 3) };
            // Q4 2024 starts Tue 1 Oct. First Monday = 7 Oct.
            yield return new object[] { new DateOnly(2024, 11, 5), DayOfWeek.Monday, new DateOnly(2024, 10, 7) };
            yield return new object[] { new DateOnly(2024, 11, 5), DayOfWeek.Tuesday, new DateOnly(2024, 10, 1) };
        }

        /// <summary>
        /// Verifies that the instance overload returns the expected first occurrence of the requested <see cref="DayOfWeek" /> within the January-to-December quarter for the given input.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(FirstDayOfWeekInQuarterJanuaryDecemberTestData), DynamicDataSourceType.Method)]
        public void FirstDayOfWeekInQuarter_WhenUsingJanuaryToDecember_ShouldReturnExpectedDate(DateOnly input, DayOfWeek dayOfWeek, DateOnly expected)
        {
            DateOnly actual = input.FirstDayOfWeekInQuarter(dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that when the quarter starts on the requested <see cref="DayOfWeek" />, the result is the quarter-start date itself.
        /// </summary>
        [TestMethod]
        public void FirstDayOfWeekInQuarter_WhenDateFallsOnTargetDayOfWeekFirstInQuarter_ShouldReturnSameDate()
        {
            DateOnly input = new DateOnly(2024, 1, 1); // Monday, Q1 start
            DateOnly actual = input.FirstDayOfWeekInQuarter(DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
            Assert.AreEqual(new DateOnly(2024, 1, 1), actual);
        }

        /// <summary>
        /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" /> on the instance overload.
        /// </summary>
        [TestMethod]
        public void FirstDayOfWeekInQuarter_WhenDayOfWeekIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateOnly input = new DateOnly(2024, 4, 20);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.FirstDayOfWeekInQuarter((DayOfWeek)999, CalendarQuarterDefinition.JanuaryToDecember);
            });
        }

        /// <summary>
        /// Verifies that an undefined <see cref="CalendarQuarterDefinition" /> value throws <see cref="ArgumentOutOfRangeException" /> on the instance overload.
        /// </summary>
        [TestMethod]
        public void FirstDayOfWeekInQuarter_WhenDefinitionIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateOnly input = new DateOnly(2024, 4, 20);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.FirstDayOfWeekInQuarter(DayOfWeek.Monday, (CalendarQuarterDefinition)999);
            });
        }

        // =========================================================================
        // FirstDayOfWeekInQuarter(int year, int quarter, DayOfWeek, CalendarQuarterDefinition)
        // =========================================================================

        /// <summary>
        /// Verifies that the static <see cref="DateOnlyExtensions.FirstDayOfWeekInQuarter(int, int, DayOfWeek, CalendarQuarterDefinition)" /> overload returns the expected first occurrence for each <c>(year, quarter, dayOfWeek)</c> tuple.
        /// </summary>
        [TestMethod]
        [DataRow(2024, 1, DayOfWeek.Monday, 2024, 1, 1)]
        [DataRow(2024, 1, DayOfWeek.Friday, 2024, 1, 5)]
        [DataRow(2024, 4, DayOfWeek.Monday, 2024, 10, 7)]
        [DataRow(2023, 2, DayOfWeek.Sunday, 2023, 4, 2)]
        public void FirstDayOfWeekInQuarter_WhenUsingYearAndQuarter_ShouldReturnExpectedDate(
            int year, int quarter, DayOfWeek dayOfWeek, int expectedYear, int expectedMonth, int expectedDay)
        {
            DateOnly actual = DateOnlyExtensions.FirstDayOfWeekInQuarter(year, quarter, dayOfWeek, CalendarQuarterDefinition.JanuaryToDecember);
            Assert.AreEqual(new DateOnly(expectedYear, expectedMonth, expectedDay), actual);
        }

        /// <summary>
        /// Verifies that the static overload throws <see cref="ArgumentOutOfRangeException" /> for quarter values outside <c>1..4</c>.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(5)]
        [DataRow(-1)]
        public void FirstDayOfWeekInQuarter_WhenQuarterIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateOnlyExtensions.FirstDayOfWeekInQuarter(2024, quarter, DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
            });
        }

        /// <summary>
        /// Verifies that the static overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="CalendarQuarterDefinition" /> value.
        /// </summary>
        [TestMethod]
        public void FirstDayOfWeekInQuarter_WhenYearAndQuarterOverloadDefinitionIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateOnlyExtensions.FirstDayOfWeekInQuarter(2024, 1, DayOfWeek.Monday, (CalendarQuarterDefinition)999);
            });
        }

        /// <summary>
        /// Verifies that the static overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" /> value.
        /// </summary>
        [TestMethod]
        public void FirstDayOfWeekInQuarter_WhenYearAndQuarterOverloadDayOfWeekIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateOnlyExtensions.FirstDayOfWeekInQuarter(2024, 1, (DayOfWeek)999, CalendarQuarterDefinition.JanuaryToDecember);
            });
        }
    }
}
