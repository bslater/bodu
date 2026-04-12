using static Bodu.Extensions.DateTimeExtensionsTests;

namespace Bodu.Extensions
{
    public partial class DateOnlyExtensionsTests
    {
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsFirstDayOfQuarterJanuaryDecemberTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenDateIsQuarterStartAndDefaultDefinition_ShouldReturnTrue(DateTime inputDateTime)
        {
            var input = DateOnly.FromDateTime(inputDateTime);

            bool actual = input.IsFirstDayOfQuarter();

            Assert.IsTrue(actual);
        }

        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsFirstDayOfQuarterTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenDateMatchesStartOfQuarterDefinition_ShouldReturnTrue(DateTime inputDateTime, CalendarQuarterDefinition definition)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            bool actual = input.IsFirstDayOfQuarter(definition);

            Assert.IsTrue(actual);
        }

        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsNotFirstDayOfQuarterTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenDateIsNotStartOfQuarterDefinition_ShouldReturnFalse(DateTime inputDateTime, CalendarQuarterDefinition definition)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            bool actual = input.IsFirstDayOfQuarter(definition);

            Assert.IsFalse(actual);
        }

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

        [TestMethod]
        public void IsFirstDayOfQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
        {
            var input = new DateOnly(2024, 4, 20);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _ = input.IsFirstDayOfQuarter(CalendarQuarterDefinition.Custom);
            });
        }

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