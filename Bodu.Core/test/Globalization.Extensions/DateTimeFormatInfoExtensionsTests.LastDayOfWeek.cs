// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeFormatInfoExtensionsTests.LastDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Globalization.Extensions
{
    public partial class DateTimeFormatInfoExtensionsTests
    {
        /// <summary>
        /// Provides (FirstDayOfWeek, expected LastDayOfWeek) pairs for the cyclic derivation
        /// <c>(FirstDayOfWeek + 6) mod 7</c>.
        /// </summary>
        public static IEnumerable<object[]> LastDayOfWeekTestData()
        {
            yield return new object[] { DayOfWeek.Sunday, DayOfWeek.Saturday };
            yield return new object[] { DayOfWeek.Monday, DayOfWeek.Sunday };
            yield return new object[] { DayOfWeek.Tuesday, DayOfWeek.Monday };
            yield return new object[] { DayOfWeek.Wednesday, DayOfWeek.Tuesday };
            yield return new object[] { DayOfWeek.Thursday, DayOfWeek.Wednesday };
            yield return new object[] { DayOfWeek.Friday, DayOfWeek.Thursday };
            yield return new object[] { DayOfWeek.Saturday, DayOfWeek.Friday };
        }

        /// <summary>
        /// Verifies that <see cref="DateTimeFormatInfoExtensions.LastDayOfWeek" />, when FirstDayOfWeekIsSpecified, returns the expected value.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(LastDayOfWeekTestData), DynamicDataSourceType.Method)]
        public void LastDayOfWeek_WhenFirstDayOfWeekIsSpecified_ShouldReturnDaySixPositionsLater(
            DayOfWeek firstDayOfWeek, DayOfWeek expected)
        {
            DateTimeFormatInfo info = (DateTimeFormatInfo)CultureInfo.InvariantCulture.DateTimeFormat.Clone();
            info.FirstDayOfWeek = firstDayOfWeek;

            DayOfWeek actual = info.LastDayOfWeek();

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DateTimeFormatInfoExtensions.LastDayOfWeek" />, when UsingEnGbCulture, returns the expected value.
        /// </summary>
        [TestMethod]
        public void LastDayOfWeek_WhenUsingEnGbCulture_ShouldReturnSunday()
        {
            DateTimeFormatInfo info = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat;

            DayOfWeek actual = info.LastDayOfWeek();

            Assert.AreEqual(DayOfWeek.Sunday, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DateTimeFormatInfoExtensions.LastDayOfWeek" />, when UsingEnUsCulture, returns the expected value.
        /// </summary>
        [TestMethod]
        public void LastDayOfWeek_WhenUsingEnUsCulture_ShouldReturnSaturday()
        {
            DateTimeFormatInfo info = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;

            DayOfWeek actual = info.LastDayOfWeek();

            Assert.AreEqual(DayOfWeek.Saturday, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DateTimeFormatInfoExtensions.LastDayOfWeek" />, when InfoIsNull, throws <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void LastDayOfWeek_WhenInfoIsNull_ShouldThrowArgumentNullException()
        {
            DateTimeFormatInfo? info = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = info!.LastDayOfWeek();
            });
        }
    }
}
