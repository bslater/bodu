namespace Bodu.Extensions
{
    public partial class DateOnlyExtensionsTests
    {
        /// <summary>
        /// Verifies that <see cref="DateTimeExtensions.IsFirstDayOfMonth(DateOnly)" /> returns <c>true</c> when the date represents the
        /// first day of the month.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsFirstDayOfMonthTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsFirstDayOfMonth_WhenDateIsFirstDay_ShouldReturnTrue(DateTime inputDateTime)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            Assert.IsTrue(input.IsFirstDayOfMonth());
        }

        /// <summary>
        /// Verifies that <see cref="DateTimeExtensions.IsFirstDayOfMonth(DateTime)" /> returns <c>false</c> when the date does not
        /// represent the first day of the month.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DateTimeExtensionsTests.IsNotFirstDayOfMonthTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
        public void IsFirstDayOfMonth_WhenDateIsNotFirstDay_ShouldReturnFalse(DateTime inputDateTime)
        {
            var input = DateOnly.FromDateTime(inputDateTime);
            Assert.IsFalse(input.IsFirstDayOfMonth());
        }
    }
}