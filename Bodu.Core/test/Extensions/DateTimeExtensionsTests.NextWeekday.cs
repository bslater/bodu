// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NextWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    public partial class DateTimeExtensionsTests
    {
        public static IEnumerable<object[]> NextWeekdaySaturdaySundayDateTimeTestData()
        {
            yield return new object[] { new DateTime(2024, 4, 19), new DateTime(2024, 4, 22) }; // Fri → Mon
            yield return new object[] { new DateTime(2024, 4, 20), new DateTime(2024, 4, 22) }; // Sat → Mon
            yield return new object[] { new DateTime(2024, 4, 21), new DateTime(2024, 4, 22) }; // Sun → Mon
            yield return new object[] { new DateTime(2024, 4, 22), new DateTime(2024, 4, 23) }; // Mon → Tue
        }

        [TestMethod]
        [DynamicData(nameof(NextWeekdaySaturdaySundayDateTimeTestData), DynamicDataSourceType.Method)]
        public void NextWeekday_WhenWeekendIsSaturdaySunday_ShouldReturnExpectedDate(DateTime input, DateTime expected)
        {
            DateTime actual = input.NextWeekday(CalendarWeekendDefinition.SaturdaySunday);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NextWeekday_WhenWeekendIsFridaySaturday_ShouldSkipFridayAndSaturday()
        {
            DateTime input = new DateTime(2024, 4, 18); // Thursday
            DateTime actual = input.NextWeekday(CalendarWeekendDefinition.FridaySaturday);
            Assert.AreEqual(new DateTime(2024, 4, 21), actual); // Sunday
        }

        [TestMethod]
        public void NextWeekday_WhenWeekendIsNone_ShouldThrowArgumentOutOfRangeException()
        {
            // IsWeekend's switch does not handle CalendarWeekendDefinition.None explicitly, so it
            // falls into the default AOOR branch during the search loop.
            DateTime input = new DateTime(2024, 4, 20);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.NextWeekday(CalendarWeekendDefinition.None);
            });
        }

        [TestMethod]
        public void NextWeekday_WhenCalled_ShouldPreserveInputKind()
        {
            DateTime input = new DateTime(2024, 4, 19, 12, 34, 56, DateTimeKind.Utc);
            DateTime actual = input.NextWeekday(CalendarWeekendDefinition.SaturdaySunday);
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
        }

        [TestMethod]
        public void NextWeekday_WhenCalled_ShouldPreserveTimeOfDay()
        {
            DateTime input = new DateTime(2024, 4, 19, 13, 45, 15);
            DateTime actual = input.NextWeekday(CalendarWeekendDefinition.SaturdaySunday);
            Assert.AreEqual(new TimeSpan(13, 45, 15), actual.TimeOfDay);
        }

        [TestMethod]
        public void NextWeekday_WhenWeekendIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime input = new DateTime(2024, 4, 20);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.NextWeekday((CalendarWeekendDefinition)999);
            });
        }

        // =========================================================================
        // Provider overload
        // =========================================================================

        [TestMethod]
        public void NextWeekday_WhenProviderIsNull_ShouldUseWeekendEnum()
        {
            DateTime input = new DateTime(2024, 4, 19);
            DateTime actual = input.NextWeekday(CalendarWeekendDefinition.SaturdaySunday, provider: null);
            Assert.AreEqual(new DateTime(2024, 4, 22), actual);
        }

        [TestMethod]
        public void NextWeekday_WhenUsingCustomProvider_ShouldApplyProviderRule()
        {
            DateTime input = new DateTime(2024, 4, 18); // Thursday
            IWeekendDefinitionProvider provider = new FridayOnlyWeekendProvider();

            // Using the Custom weekend with a FridayOnly provider; Friday 19 Apr is the weekend, so next is Sat 20.
            DateTime actual = input.NextWeekday(CalendarWeekendDefinition.Custom, provider);
            Assert.AreEqual(new DateTime(2024, 4, 20), actual);
        }

        [TestMethod]
        public void NextWeekday_WhenProviderOverload_WeekendIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime input = new DateTime(2024, 4, 20);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.NextWeekday((CalendarWeekendDefinition)999, provider: null);
            });
        }
    }
}
