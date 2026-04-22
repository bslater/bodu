// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NearestDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    public partial class DateTimeExtensionsTests
    {
        // =========================================================================
        // NearestDayOfWeek(this DateTime, DayOfWeek)
        // =========================================================================

        /// <summary>
        /// Provides (reference DateTime, target day-of-week, expected nearest DateTime).
        /// Wednesday 17 April 2024 is the reference.
        /// </summary>
        public static IEnumerable<object[]> NearestDayOfWeekDateTimeTestData()
        {
            yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Wednesday, new DateTime(2024, 4, 17) };
            yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Tuesday, new DateTime(2024, 4, 16) };
            yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Thursday, new DateTime(2024, 4, 18) };
            yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Sunday, new DateTime(2024, 4, 14) };
            yield return new object[] { new DateTime(2024, 4, 17), DayOfWeek.Saturday, new DateTime(2024, 4, 20) };
        }

        /// <summary>
        /// Verifies that <see cref="DateTimeExtensions.NearestDayOfWeek(DateTime, DayOfWeek)" /> returns the closest occurrence of the requested <see cref="DayOfWeek" /> in either direction.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(NearestDayOfWeekDateTimeTestData), DynamicDataSourceType.Method)]
        public void NearestDayOfWeek_WhenCalled_ShouldReturnExpectedDate(DateTime dateTime, DayOfWeek dayOfWeek, DateTime expected)
        {
            DateTime actual = dateTime.NearestDayOfWeek(dayOfWeek);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DateTimeExtensions.NearestDayOfWeek(DateTime, DayOfWeek)" /> preserves the input's <see cref="DateTime.Kind" />.
        /// </summary>
        [TestMethod]
        public void NearestDayOfWeek_WhenCalled_ShouldPreserveInputKind()
        {
            DateTime dateTime = new DateTime(2024, 4, 17, 0, 0, 0, DateTimeKind.Local);
            DateTime actual = dateTime.NearestDayOfWeek(DayOfWeek.Monday);
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);
        }

        /// <summary>
        /// Verifies that when the past and future occurrences are equidistant (3 days each), the earlier date is returned.
        /// </summary>
        [TestMethod]
        public void NearestDayOfWeek_WhenTiedBetweenPastAndFuture_ShouldReturnEarlierDate()
        {
            DateTime dateTime = new DateTime(2024, 4, 17);
            DateTime actual = dateTime.NearestDayOfWeek(DayOfWeek.Sunday);
            Assert.AreEqual(new DateTime(2024, 4, 14), actual);
        }

        /// <summary>
        /// Verifies that an undefined <see cref="DayOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        [TestMethod]
        public void NearestDayOfWeek_WhenDayOfWeekIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime dateTime = new DateTime(2024, 4, 17);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = dateTime.NearestDayOfWeek((DayOfWeek)999);
            });
        }

        // =========================================================================
        // NearestDayOfWeek(int year, int month, int day, DayOfWeek)
        // =========================================================================

        /// <summary>
        /// Verifies that the static <see cref="DateTimeExtensions.NearestDayOfWeek(int, int, int, DayOfWeek)" /> overload returns the expected nearest date.
        /// </summary>
        [TestMethod]
        public void NearestDayOfWeek_WhenUsingYearMonthDay_ShouldReturnExpectedDate()
        {
            DateTime actual = DateTimeExtensions.NearestDayOfWeek(2024, 4, 17, DayOfWeek.Monday);
            Assert.AreEqual(new DateTime(2024, 4, 15), actual);
        }

        /// <summary>
        /// Verifies that the static <c>(year, month, day)</c> overload always returns a result with <see cref="DateTimeKind.Unspecified" />.
        /// </summary>
        [TestMethod]
        public void NearestDayOfWeek_WhenUsingYearMonthDay_ShouldReturnResultWithUnspecifiedKind()
        {
            DateTime actual = DateTimeExtensions.NearestDayOfWeek(2024, 4, 17, DayOfWeek.Monday);
            Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
        }

        /// <summary>
        /// Verifies that the static <c>(year, month, day)</c> overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="DayOfWeek" />.
        /// </summary>
        [TestMethod]
        public void NearestDayOfWeek_WhenUsingYearMonthDayWithInvalidDayOfWeek_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateTimeExtensions.NearestDayOfWeek(2024, 4, 17, (DayOfWeek)99);
            });
        }
    }
}
