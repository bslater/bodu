// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.PreviousWeekday.cs" company="PlaceholderCompany">
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
        // PreviousWeekday(this DateOnly, CalendarWeekendDefinition)
        // =========================================================================

        /// <summary>
        /// Provides cases for PreviousWeekday under the Saturday/Sunday weekend model.
        /// </summary>
        public static IEnumerable<object[]> PreviousWeekdaySaturdaySundayTestData()
        {
            // Mon 22 Apr 2024 → previous weekday skips Sun/Sat → Fri 19 Apr.
            yield return new object[] { new DateOnly(2024, 4, 22), new DateOnly(2024, 4, 19) };
            // Sun 21 Apr 2024 → Fri 19 Apr.
            yield return new object[] { new DateOnly(2024, 4, 21), new DateOnly(2024, 4, 19) };
            // Sat 20 Apr 2024 → Fri 19 Apr.
            yield return new object[] { new DateOnly(2024, 4, 20), new DateOnly(2024, 4, 19) };
            // Wed 17 Apr 2024 → Tue 16 Apr.
            yield return new object[] { new DateOnly(2024, 4, 17), new DateOnly(2024, 4, 16) };
        }

        [TestMethod]
        [DynamicData(nameof(PreviousWeekdaySaturdaySundayTestData), DynamicDataSourceType.Method)]
        public void PreviousWeekday_WhenWeekendIsSaturdaySunday_ShouldReturnExpectedDate(DateOnly date, DateOnly expected)
        {
            DateOnly actual = date.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PreviousWeekday_WhenWeekendIsFridaySaturday_ShouldSkipSaturdayAndFriday()
        {
            // Sun 21 Apr 2024 → skip Sat 20, Fri 19 → Thu 18 Apr.
            DateOnly input = new DateOnly(2024, 4, 21);
            DateOnly actual = input.PreviousWeekday(CalendarWeekendDefinition.FridaySaturday);
            Assert.AreEqual(new DateOnly(2024, 4, 18), actual);
        }

        [TestMethod]
        public void PreviousWeekday_WhenWeekendIsNone_ShouldThrowArgumentOutOfRangeException()
        {
            // IsWeekend's switch does not handle CalendarWeekendDefinition.None explicitly, so it
            // falls into the default AOOR branch during the search loop.
            DateOnly input = new DateOnly(2024, 4, 21);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.PreviousWeekday(CalendarWeekendDefinition.None);
            });
        }

        [TestMethod]
        public void PreviousWeekday_WhenWeekendIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateOnly input = new DateOnly(2024, 4, 22);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.PreviousWeekday((CalendarWeekendDefinition)999);
            });
        }

        // =========================================================================
        // PreviousWeekday(this DateOnly, CalendarWeekendDefinition, IWeekendDefinitionProvider?)
        // =========================================================================

        [TestMethod]
        public void PreviousWeekday_WhenProviderIsNull_ShouldUseWeekendEnum()
        {
            DateOnly input = new DateOnly(2024, 4, 22); // Monday
            DateOnly actual = input.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday, provider: null);
            Assert.AreEqual(new DateOnly(2024, 4, 19), actual);
        }

        [TestMethod]
        public void PreviousWeekday_WhenProviderOverload_WeekendIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateOnly input = new DateOnly(2024, 4, 22);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.PreviousWeekday((CalendarWeekendDefinition)999, provider: null);
            });
        }
    }
}
