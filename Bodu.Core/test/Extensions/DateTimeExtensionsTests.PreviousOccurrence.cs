// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.PreviousOccurrence.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    public partial class DateTimeExtensionsTests
    {
        /// <summary>
        /// Verifies that when <paramref name="before"/> sits between occurrences, the greatest occurrence
        /// strictly less than <paramref name="before"/> is returned.
        /// </summary>
        [TestMethod]
        public void PreviousOccurrence_WhenBeforeIsBetweenOccurrences_ShouldReturnMostRecentOccurrence()
        {
            DateTime start = new DateTime(2025, 7, 7, 9, 0, 0);
            TimeSpan interval = TimeSpan.FromHours(1);
            DateTime before = new DateTime(2025, 7, 7, 10, 45, 0);

            DateTime actual = start.PreviousOccurrence(interval, before);

            Assert.AreEqual(new DateTime(2025, 7, 7, 10, 0, 0), actual);
        }

        /// <summary>
        /// Verifies that when <paramref name="before"/> falls exactly on an occurrence boundary, the previous
        /// occurrence (one interval earlier) is returned instead.
        /// </summary>
        [TestMethod]
        public void PreviousOccurrence_WhenBeforeIsOnOccurrenceBoundary_ShouldReturnPriorOccurrence()
        {
            DateTime start = new DateTime(2025, 7, 7, 9, 0, 0);
            TimeSpan interval = TimeSpan.FromHours(1);
            DateTime before = new DateTime(2025, 7, 7, 11, 0, 0);

            DateTime actual = start.PreviousOccurrence(interval, before);

            Assert.AreEqual(new DateTime(2025, 7, 7, 10, 0, 0), actual);
        }

        /// <summary>
        /// Verifies that when <paramref name="before"/> equals <paramref name="dateTime"/>, the occurrence one
        /// interval before the start is returned.
        /// </summary>
        [TestMethod]
        public void PreviousOccurrence_WhenBeforeEqualsStart_ShouldReturnStartMinusInterval()
        {
            DateTime start = new DateTime(2025, 7, 7, 9, 0, 0);
            TimeSpan interval = TimeSpan.FromHours(1);

            DateTime actual = start.PreviousOccurrence(interval, start);

            Assert.AreEqual(new DateTime(2025, 7, 7, 8, 0, 0), actual);
        }

        /// <summary>
        /// Verifies that when <paramref name="before"/> precedes <paramref name="dateTime"/>, the occurrence
        /// one interval before the start is returned.
        /// </summary>
        [TestMethod]
        public void PreviousOccurrence_WhenBeforeIsEarlierThanStart_ShouldReturnStartMinusInterval()
        {
            DateTime start = new DateTime(2025, 7, 7, 9, 0, 0);
            TimeSpan interval = TimeSpan.FromHours(1);
            DateTime before = new DateTime(2025, 7, 7, 8, 30, 0);

            DateTime actual = start.PreviousOccurrence(interval, before);

            Assert.AreEqual(new DateTime(2025, 7, 7, 8, 0, 0), actual);
        }

        /// <summary>
        /// Verifies that the returned value preserves the <see cref="DateTimeKind"/> of the start instant.
        /// </summary>
        [TestMethod]
        public void PreviousOccurrence_WhenCalled_ShouldPreserveStartKind()
        {
            DateTime start = new DateTime(2025, 7, 7, 9, 0, 0, DateTimeKind.Utc);
            TimeSpan interval = TimeSpan.FromHours(1);
            DateTime before = new DateTime(2025, 7, 7, 10, 45, 0, DateTimeKind.Utc);

            DateTime actual = start.PreviousOccurrence(interval, before);

            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
        }

        /// <summary>
        /// Verifies that a zero <see cref="TimeSpan"/> interval throws <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        [TestMethod]
        public void PreviousOccurrence_WhenIntervalIsZero_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime start = new DateTime(2025, 7, 7, 9, 0, 0);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = start.PreviousOccurrence(TimeSpan.Zero, start);
            });
        }

        /// <summary>
        /// Verifies that a negative <see cref="TimeSpan"/> interval throws <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        [TestMethod]
        public void PreviousOccurrence_WhenIntervalIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime start = new DateTime(2025, 7, 7, 9, 0, 0);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = start.PreviousOccurrence(TimeSpan.FromMinutes(-5), start);
            });
        }

        /// <summary>
        /// Verifies that when <paramref name="before"/> is many intervals after the start, the method
        /// returns the occurrence immediately before it without drift.
        /// </summary>
        [TestMethod]
        public void PreviousOccurrence_WhenBeforeIsManyIntervalsAhead_ShouldReturnCorrectOccurrence()
        {
            DateTime start = new DateTime(2025, 1, 1, 0, 0, 0);
            TimeSpan interval = TimeSpan.FromDays(7);
            DateTime before = start.AddDays(100); // 100 / 7 = 14 remainder 2 → previous = start + 14*7 = 99 days
            // Actually: remainder 2 is non-zero → previousIntervalCount = quotient = 14 → start + 14*7 = 98 days.
            DateTime expected = start.AddDays(98);

            DateTime actual = start.PreviousOccurrence(interval, before);

            Assert.AreEqual(expected, actual);
        }
    }
}
