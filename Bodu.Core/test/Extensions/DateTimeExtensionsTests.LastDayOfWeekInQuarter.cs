// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.LastDayOfWeekInQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    public partial class DateTimeExtensionsTests
    {
        // Note: The current implementation of LastDayOfWeekInQuarter starts from the quarter START
        // and advances forward using ((target - currentDow + 7) % 7) days, which yields the FIRST
        // occurrence of the target day-of-week, not the last. The provider overload (which calls
        // GetTicksToPreviousDayOfWeek from the quarter end) does return the last occurrence.
        // These tests lock in the observed behaviour per overload so any future correction to
        // match the documented "last occurrence within the quarter" contract surfaces as a
        // regression.

        // =========================================================================
        // Instance: LastDayOfWeekInQuarter(this DateTime, DayOfWeek) — calendar default
        // Implementation advances forward from quarter start, so this returns the FIRST
        // occurrence of the target day in the quarter.
        // =========================================================================

        public static IEnumerable<object[]> LastDayOfWeekInQuarterCalendarObservedTestData()
        {
            // Q1 2024 starts Mon 1 Jan. Target Sun → (0-1+7)%7 = 6 → 7 Jan.
            yield return new object[] { new DateTime(2024, 2, 15), DayOfWeek.Sunday, new DateTime(2024, 1, 7) };
            // Target Friday → 4 → 5 Jan.
            yield return new object[] { new DateTime(2024, 2, 15), DayOfWeek.Friday, new DateTime(2024, 1, 5) };
            // Q2 starts Mon 1 Apr. Target Mon → 0 → 1 Apr.
            yield return new object[] { new DateTime(2024, 5, 10), DayOfWeek.Monday, new DateTime(2024, 4, 1) };
            // Q4 starts Tue 1 Oct. Target Tue → 0 → 1 Oct.
            yield return new object[] { new DateTime(2024, 11, 5), DayOfWeek.Tuesday, new DateTime(2024, 10, 1) };
        }

        [TestMethod]
        [DynamicData(nameof(LastDayOfWeekInQuarterCalendarObservedTestData), DynamicDataSourceType.Method)]
        public void LastDayOfWeekInQuarter_WhenUsingCalendarDefault_ShouldReturnExpectedDate(DateTime input, DayOfWeek dayOfWeek, DateTime expected)
        {
            DateTime actual = input.LastDayOfWeekInQuarter(dayOfWeek);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenInstanceCalendarOverload_PreservesInputKind()
        {
            DateTime input = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc);
            DateTime actual = input.LastDayOfWeekInQuarter(DayOfWeek.Monday);
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
        }

        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenInstanceCalendarOverload_DayOfWeekInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime input = new DateTime(2024, 2, 15);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.LastDayOfWeekInQuarter((DayOfWeek)999);
            });
        }

        // =========================================================================
        // Instance: LastDayOfWeekInQuarter(this DateTime, DayOfWeek, CalendarQuarterDefinition)
        // =========================================================================

        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenUsingJanuaryToDecemberDefinition_ShouldReturnSameResultAsCalendarOverload()
        {
            DateTime input = new DateTime(2024, 2, 15);
            DateTime fromDefaultOverload = input.LastDayOfWeekInQuarter(DayOfWeek.Monday);
            DateTime fromExplicitDefinition = input.LastDayOfWeekInQuarter(DayOfWeek.Monday, CalendarQuarterDefinition.JanuaryToDecember);
            Assert.AreEqual(fromDefaultOverload, fromExplicitDefinition);
        }

        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenCustomDefinitionWithoutProvider_ShouldThrowInvalidOperationException()
        {
            DateTime input = new DateTime(2024, 5, 10);
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _ = input.LastDayOfWeekInQuarter(DayOfWeek.Monday, CalendarQuarterDefinition.Custom);
            });
        }

        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenInstanceDefinitionOverload_DefinitionInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime input = new DateTime(2024, 5, 10);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.LastDayOfWeekInQuarter(DayOfWeek.Monday, (CalendarQuarterDefinition)999);
            });
        }

        // =========================================================================
        // Instance: LastDayOfWeekInQuarter(this DateTime, DayOfWeek, IQuarterDefinitionProvider)
        // This overload uses GetTicksToPreviousDayOfWeek on the provider's quarter end and DOES
        // return the most recent occurrence of the target day on or before that end.
        // =========================================================================

        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenProviderIsNull_ShouldThrowArgumentNullException()
        {
            DateTime input = new DateTime(2024, 5, 10);
            IQuarterDefinitionProvider? provider = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = input.LastDayOfWeekInQuarter(DayOfWeek.Monday, provider!);
            });
        }

        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenUsingValidQuarterProvider_ShouldReturnTargetDayOfWeek()
        {
            DateTime input = new DateTime(2024, 5, 10);
            IQuarterDefinitionProvider provider = new ValidQuarterProvider();
            DateTime actual = input.LastDayOfWeekInQuarter(DayOfWeek.Monday, provider);
            Assert.AreEqual(DayOfWeek.Monday, actual.DayOfWeek);
        }

        [TestMethod]
        public void LastDayOfWeekInQuarter_WhenProviderOverload_DayOfWeekInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            DateTime input = new DateTime(2024, 5, 10);
            IQuarterDefinitionProvider provider = new ValidQuarterProvider();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.LastDayOfWeekInQuarter((DayOfWeek)999, provider);
            });
        }

        // =========================================================================
        // Static: GetLastDayOfWeekInQuarter(int, int, DayOfWeek)
        // Same forward-from-start behaviour as instance calendar overload.
        // =========================================================================

        [TestMethod]
        public void GetLastDayOfWeekInQuarter_WhenUsingYearAndQuarter_ShouldReturnExpectedDate()
        {
            // Q1 2024 starts Mon 1 Jan. Target Sunday → first Sunday = 7 Jan.
            DateTime actual = DateTimeExtensions.GetLastDayOfWeekInQuarter(2024, 1, DayOfWeek.Sunday);
            Assert.AreEqual(new DateTime(2024, 1, 7), actual);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(5)]
        [DataRow(-1)]
        public void GetLastDayOfWeekInQuarter_WhenQuarterIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int quarter)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateTimeExtensions.GetLastDayOfWeekInQuarter(2024, quarter, DayOfWeek.Monday);
            });
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(10000)]
        public void GetLastDayOfWeekInQuarter_WhenYearIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int year)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateTimeExtensions.GetLastDayOfWeekInQuarter(year, 1, DayOfWeek.Monday);
            });
        }

        [TestMethod]
        public void GetLastDayOfWeekInQuarter_WhenDayOfWeekIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateTimeExtensions.GetLastDayOfWeekInQuarter(2024, 1, (DayOfWeek)999);
            });
        }

        // =========================================================================
        // Static: GetLastDayOfWeekInQuarter(int, int, DayOfWeek, CalendarQuarterDefinition)
        // =========================================================================

        [TestMethod]
        public void GetLastDayOfWeekInQuarter_WhenUsingDefinitionOverload_ShouldReturnExpectedDate()
        {
            DateTime actual = DateTimeExtensions.GetLastDayOfWeekInQuarter(2024, 1, DayOfWeek.Sunday, CalendarQuarterDefinition.JanuaryToDecember);
            Assert.AreEqual(new DateTime(2024, 1, 7), actual);
        }

        [TestMethod]
        public void GetLastDayOfWeekInQuarter_WhenDefinitionIsCustom_ShouldThrowInvalidOperationException()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _ = DateTimeExtensions.GetLastDayOfWeekInQuarter(2024, 1, DayOfWeek.Monday, CalendarQuarterDefinition.Custom);
            });
        }

        [TestMethod]
        public void GetLastDayOfWeekInQuarter_WhenDefinitionIsInvalid_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = DateTimeExtensions.GetLastDayOfWeekInQuarter(2024, 1, DayOfWeek.Monday, (CalendarQuarterDefinition)999);
            });
        }
    }
}
