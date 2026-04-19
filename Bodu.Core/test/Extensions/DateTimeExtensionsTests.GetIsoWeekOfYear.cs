// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.GetIsoWeekOfYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions
{
    public partial class DateTimeExtensionsTests
    {
        /// <summary>
        /// Provides representative dates paired with their expected ISO 8601 week numbers.
        /// </summary>
        public static IEnumerable<object[]> GetIsoWeekOfYearTestData()
        {
            // Thu 4 Jan 2024 is always in ISO week 1 by definition (first Thursday of year).
            yield return new object[] { new DateTime(2024, 1, 4), 1 };
            // Mon 1 Jan 2024 is also in week 1 since week containing 4 Jan starts Mon 1 Jan.
            yield return new object[] { new DateTime(2024, 1, 1), 1 };
            // Sun 31 Dec 2023 is in ISO week 52 of 2023.
            yield return new object[] { new DateTime(2023, 12, 31), 52 };
            // Fri 1 Jan 2021 is in ISO week 53 of 2020 (long year).
            yield return new object[] { new DateTime(2021, 1, 1), 53 };
            // Mid-year sanity check.
            yield return new object[] { new DateTime(2024, 6, 17), 25 };
        }

        [TestMethod]
        [DynamicData(nameof(GetIsoWeekOfYearTestData), DynamicDataSourceType.Method)]
        public void GetIsoWeekOfYear_WhenCalled_ShouldReturnExpectedWeekNumber(DateTime input, int expected)
        {
            int actual = input.GetIsoWeekOfYear();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetIsoWeekOfYear_WhenCalled_ShouldReturnValueInRange1To53()
        {
            DateTime input = new DateTime(2024, 8, 15);
            int actual = input.GetIsoWeekOfYear();
            Assert.IsTrue(actual >= 1 && actual <= 53);
        }

        [TestMethod]
        public void GetIsoWeekOfYear_WhenTimeOfDayIsNonZero_ShouldReturnSameResultAsMidnight()
        {
            DateTime morning = new DateTime(2024, 1, 4, 0, 0, 0);
            DateTime evening = new DateTime(2024, 1, 4, 23, 59, 59);
            Assert.AreEqual(morning.GetIsoWeekOfYear(), evening.GetIsoWeekOfYear());
        }

        [TestMethod]
        public void GetIsoWeekOfYear_WhenCalled_ShouldMatchIsoWeekCalculatorForLateDecember()
        {
            DateTime input = new DateTime(2024, 12, 30); // Monday → start of ISO week 1 of 2025.
            int expected = ISOWeek.GetWeekOfYear(input);
            int actual = input.GetIsoWeekOfYear();
            Assert.AreEqual(expected, actual);
        }
    }
}
