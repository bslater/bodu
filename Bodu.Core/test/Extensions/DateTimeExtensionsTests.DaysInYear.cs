// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.DaysInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions
{
    public partial class DateTimeExtensionsTests
    {
        /// <summary>
        /// Verifies that Days In Year, when Called, returns Correct Days.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DaysInYearGregorianCalendarTestData), DynamicDataSourceType.Method)]
        public void DaysInYear_WhenCalled_ShouldReturnCorrectDays(DateTime input, int expected)
        {
            var actual = input.DaysInYear();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that Days In Year, when Using Custom Calendar, matches Expected.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DaysInYearTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void DaysInYear_WhenUsingCustomCalendar_ShouldMatchExpected(int year, Calendar calendar, int expectedDays)
        {
            DateTime input = new(year, 1, 1);
            int actual = input.DaysInYear(calendar);
            Assert.AreEqual(expectedDays, actual, $"{calendar.GetType().Name} returned {actual} days for year {year}.");
        }

        /// <summary>
        /// Verifies that Days In Year, when Using Min Value, does not Throw.
        /// </summary>
        [TestMethod]
        public void DaysInYear_WhenUsingMinValue_ShouldNotThrow()
        {
            Assert.IsTrue(DateTime.MinValue.DaysInYear() > 0);
        }

        /// <summary>
        /// Verifies that Days In Year, when Using Max Value, does not Throw.
        /// </summary>
        [TestMethod]
        public void DaysInYear_WhenUsingMaxValue_ShouldNotThrow()
        {
            Assert.IsTrue(DateTime.MaxValue.DaysInYear() > 0);
        }

        /// <summary>
        /// Verifies that Days In Year, when No Calendar Provided, uses Current Culture Calendar.
        /// </summary>
        [TestMethod]
        public void DaysInYear_WhenNoCalendarProvided_ShouldUseCurrentCultureCalendar()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                // Set a known culture with a distinct calendar
                var customCulture = new CultureInfo("ar-SA");
                customCulture.DateTimeFormat.Calendar = new UmAlQuraCalendar(); // Has non-Gregorian year lengths
                CultureInfo.CurrentCulture = customCulture;

                DateTime input = new(1445, 1, 1); // 1445 AH (2023-07-19 Gregorian)
                int expected = customCulture.DateTimeFormat.Calendar.GetDaysInYear(1445);
                int actual = input.DaysInYear(); // Should use current culture calendar

                Assert.AreEqual(expected, actual);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}