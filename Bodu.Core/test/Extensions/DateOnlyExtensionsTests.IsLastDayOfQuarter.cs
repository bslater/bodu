namespace Bodu.Extensions
{
    public partial class DateOnlyExtensionsTests
    {
        /// <summary>
        /// Verifies that <see cref="DateOnlyExtensions.IsFirstDayOfQuarter(DateOnly)" /> returns <c>true</c> only when the date is the
        /// first day of a quarter based on the <see cref="CalendarQuarterDefinition.JanuaryToDecember" /> structure.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsLastDayOfQuarterJanuaryDecemberTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsLastDayOfQuarter_WhenDateIsQuarterStartAndDefaultDefinition_ShouldReturnTrue(DateTime inputDateTime, bool expected)
        {
            var input = DateOnly.FromDateTime(inputDateTime);

            bool actual = input.IsLastDayOfQuarter();

            Assert.AreEqual(actual, expected);
        }

        /// <summary>
        /// Verifies that <see cref="DateOnlyExtensions.IsLastDayOfQuarter(DateOnly, CalendarQuarterDefinition)" /> returns <c>true</c> only
        /// when the input date equals the computed start of the quarter.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsLastDayOfQuarterTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsLastDayOfQuarter_WhenComparedToExpectedStart_ShouldReturnExpectedResult(DateTime inputDateTime, CalendarQuarterDefinition definition)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            bool actual = input.IsLastDayOfQuarter(definition);

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Verifies that <see cref="DateOnlyExtensions.IsLastDayOfQuarter(DateOnly, CalendarQuarterDefinition)" /> returns <c>false</c>
        /// when the input date is not the first day of the quarter.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsNotLastDayOfQuarterTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsLastDayOfQuarter_WhenDateIsNotStartOfQuarter_ShouldReturnFalse(DateTime inputDateTime, CalendarQuarterDefinition definition)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            bool actual = input.IsLastDayOfQuarter(definition);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void IsLastDayOfQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
        {
            var input = new DateOnly(2024, 4, 20);
            var definition = (CalendarQuarterDefinition)999;

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.IsLastDayOfQuarter(definition);
            });
        }

        [TestMethod]
        public void IsLastDayOfQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
        {
            var input = new DateOnly(2024, 4, 20);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _ = input.IsLastDayOfQuarter(CalendarQuarterDefinition.Custom);
            });
        }

        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.ValidQuarterProvider.IsLastDayOfQuarterTestData), typeof(DateTimeExtensionsTests.ValidQuarterProvider), DynamicDataSourceType.Method)]
        public void IsLastDayOfQuarter_WhenUsingValidQuarterProvider_ShouldReturnExpectedDate(DateTime inputDateTime, bool expected)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            var provider = new DateTimeExtensionsTests.ValidQuarterProvider();

            var actual = input.IsLastDayOfQuarter(provider);

            Assert.AreEqual(expected, actual);
        }
    }
}