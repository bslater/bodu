// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsFirstDayOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using static Bodu.Extensions.DateTimeExtensionsTests;

namespace Bodu.Extensions
{
    public partial class DateOnlyExtensionsTests
    {
        /// <summary>
        /// Verifies that Is First Day Of Quarter, when Date Is Quarter Start And Default Definition, returns True.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsFirstDayOfQuarterJanuaryDecemberTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenDateIsQuarterStartAndDefaultDefinition_ShouldReturnTrue(DateTime inputDateTime)
        {
            var input = DateOnly.FromDateTime(inputDateTime);

            bool actual = input.IsFirstDayOfQuarter();

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Verifies that Is First Day Of Quarter, when Date Matches Start Of Quarter Definition, returns True.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsFirstDayOfQuarterTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenDateMatchesStartOfQuarterDefinition_ShouldReturnTrue(DateTime inputDateTime, CalendarQuarterDefinition definition)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            bool actual = input.IsFirstDayOfQuarter(definition);

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Verifies that Is First Day Of Quarter, when Date Is Not Start Of Quarter Definition, returns False.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsNotFirstDayOfQuarterTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenDateIsNotStartOfQuarterDefinition_ShouldReturnFalse(DateTime inputDateTime, CalendarQuarterDefinition definition)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            bool actual = input.IsFirstDayOfQuarter(definition);

            Assert.IsFalse(actual);
        }

        /// <summary>
        /// Verifies that Is First Day Of Quarter, when Definition Is Invalid, throws Exactly.
        /// </summary>
        [TestMethod]
        public void IsFirstDayOfQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
        {
            var input = new DateOnly(2024, 4, 20);
            var definition = (CalendarQuarterDefinition)999;

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.IsFirstDayOfQuarter(definition);
            });
        }

        /// <summary>
        /// Verifies that Is First Day Of Quarter, when Definition Is Custom, throws Exactly.
        /// </summary>
        [TestMethod]
        public void IsFirstDayOfQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
        {
            var input = new DateOnly(2024, 4, 20);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _ = input.IsFirstDayOfQuarter(CalendarQuarterDefinition.Custom);
            });
        }

        /// <summary>
        /// Verifies that Is First Day Of Quarter, when Using Valid Quarter Provider, returns Expected Date.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.ValidQuarterProvider.IsFirstDayOfQuarterTestData), typeof(DateTimeExtensionsTests.ValidQuarterProvider), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenUsingValidQuarterProvider_ShouldReturnExpectedDate(DateTime inputDateTime, bool expected)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            var provider = new ValidQuarterProvider();

            var actual = input.IsFirstDayOfQuarter(provider);

            Assert.AreEqual(expected, actual);
        }
    }
}