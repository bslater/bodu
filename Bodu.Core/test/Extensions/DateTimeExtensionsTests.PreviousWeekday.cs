// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.PreviousWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    public partial class DateTimeExtensionsTests
    {
        public static IEnumerable<object[]> PreviousWeekdaySaturdaySundayDateTimeTestData()
        {
            yield return new object[] { new DateTime(2024, 4, 22), new DateTime(2024, 4, 19) }; // Mon → Fri
            yield return new object[] { new DateTime(2024, 4, 21), new DateTime(2024, 4, 19) }; // Sun → Fri
            yield return new object[] { new DateTime(2024, 4, 20), new DateTime(2024, 4, 19) }; // Sat → Fri
            yield return new object[] { new DateTime(2024, 4, 17), new DateTime(2024, 4, 16) }; // Wed → Tue
        }

        [TestMethod]
        [DynamicData(nameof(PreviousWeekdaySaturdaySundayDateTimeTestData), DynamicDataSourceType.Method)]
        public void PreviousWeekday_WhenWeekendIsSaturdaySunday_ShouldReturnExpectedDate(DateTime input, DateTime expected)
        {
            DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PreviousWeekday_WhenWeekendIsFridaySaturday_ShouldSkipSaturdayAndFriday()
        {
            DateTime input = new DateTime(2024, 4, 21); // Sunday
            DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.FridaySaturday);
            Assert.AreEqual(new DateTime(2024, 4, 18), actual); // Thursday
        }

        [TestMethod]
        public void PreviousWeekday_WhenWeekendIsNone_ShouldThrowArgumentOutOfRangeException()
        {
            // IsWeekend's switch does not handle CalendarWeekendDefinition.None explicitly, so it
            // falls into the default AOOR branch during the search loop.
            DateTime input = new DateTime(2024, 4, 21);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.PreviousWeekday(CalendarWeekendDefinition.None);
            });
        }

        [TestMethod]
        public void PreviousWeekday_WhenCalled_ShouldPreserveInputKind()
        {
            DateTime input = new DateTime(2024, 4, 22, 0, 0, 0, DateTimeKind.Utc);
            DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday);
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
        }

        [TestMethod]
        public void PreviousWeekday_WhenCalled_ShouldPreserveTimeOfDay()
        {
            DateTime input = new DateTime(2024, 4, 22, 9, 15, 0);
            DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday);
            Assert.AreEqual(new TimeSpan(9, 15, 0), actual.TimeOfDay);
        }

        [TestMethod]
        public void PreviousWeekday_WhenWeekendIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime input = new DateTime(2024, 4, 22);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.PreviousWeekday((CalendarWeekendDefinition)999);
            });
        }

        // =========================================================================
        // Provider overload
        // =========================================================================

        [TestMethod]
        public void PreviousWeekday_WhenProviderIsNull_ShouldUseWeekendEnum()
        {
            DateTime input = new DateTime(2024, 4, 22);
            DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.SaturdaySunday, provider: null);
            Assert.AreEqual(new DateTime(2024, 4, 19), actual);
        }

        [TestMethod]
        public void PreviousWeekday_WhenUsingCustomProvider_ShouldApplyProviderRule()
        {
            DateTime input = new DateTime(2024, 4, 20); // Saturday
            IWeekendDefinitionProvider provider = new FridayOnlyWeekendProvider();

            // Only Friday is weekend per provider. Previous calendar day is Fri 19 → weekend, so skip to Thu 18.
            DateTime actual = input.PreviousWeekday(CalendarWeekendDefinition.Custom, provider);
            Assert.AreEqual(new DateTime(2024, 4, 18), actual);
        }

        [TestMethod]
        public void PreviousWeekday_WhenProviderOverload_WeekendIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime input = new DateTime(2024, 4, 22);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.PreviousWeekday((CalendarWeekendDefinition)999, provider: null);
            });
        }
    }
}
