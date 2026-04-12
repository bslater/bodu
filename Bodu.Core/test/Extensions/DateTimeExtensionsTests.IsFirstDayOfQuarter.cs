namespace Bodu.Extensions
{
    public partial class DateTimeExtensionsTests
    {
        [TestMethod]
        [DynamicData(nameof(IsFirstDayOfQuarterJanuaryDecemberTestData), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenDateIsQuarterStartAndDefaultDefinition_ShouldReturnTrue(DateTime input)
        {
            bool actual = input.IsFirstDayOfQuarter();

            Assert.IsTrue(actual);
        }

        [TestMethod]
        [DynamicData(nameof(IsFirstDayOfQuarterTestData), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenDateMatchesStartOfQuarterDefinition_ShouldReturnTrue(DateTime inputDate, CalendarQuarterDefinition definition)
        {
            bool actual = inputDate.IsFirstDayOfQuarter(definition);

            Assert.IsTrue(actual);
        }

        [TestMethod]
        [DynamicData(nameof(IsNotFirstDayOfQuarterTestData), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenDateIsNotStartOfQuarterDefinition_ShouldReturnFalse(DateTime inputDate, CalendarQuarterDefinition definition)
        {
            bool actual = inputDate.IsFirstDayOfQuarter(definition);
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void IsFirstDayOfQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
        {
            var input = new DateTime(2024, 4, 20);
            var definition = (CalendarQuarterDefinition)999;

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = input.IsFirstDayOfQuarter(definition);
            });
        }

        [TestMethod]
        public void IsFirstDayOfQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
        {
            var input = new DateTime(2024, 4, 20);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _ = input.IsFirstDayOfQuarter(CalendarQuarterDefinition.Custom);
            });
        }

        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.ValidQuarterProvider.IsFirstDayOfQuarterTestData), typeof(DateTimeExtensionsTests.ValidQuarterProvider), DynamicDataSourceType.Method)]
        public void IsFirstDayOfQuarter_WhenUsingValidQuarterProvider_ShouldReturnExpectedDate(DateTime input, bool expected)
        {
            var provider = new ValidQuarterProvider();

            var actual = input.IsFirstDayOfQuarter(provider);

            Assert.AreEqual(expected, actual);
        }
    }
}