namespace Bodu.Extensions
{
    public partial class DateTimeExtensionsTests
    {
        /// <summary>
        /// Verifies that <see cref="DateTimeExtensions.IsFirstDayOfMonth(DateTime)" /> returns <c>true</c> when the date represents the
        /// first day of the month.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(IsFirstDayOfMonthTestData), DynamicDataSourceType.Method)]
        public void IsFirstDayOfMonth_WhenDateIsFirstDay_ShouldReturnTrue(DateTime input)
        {
            var actual = input.IsFirstDayOfMonth();

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Verifies that <see cref="DateTimeExtensions.IsFirstDayOfMonth(DateTime)" /> returns <c>false</c> when the date does not
        /// represent the first day of the month.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(IsNotFirstDayOfMonthTestData), DynamicDataSourceType.Method)]
        public void IsFirstDayOfMonth_WhenDateIsNotFirstDay_ShouldReturnFalse(DateTime input)
        {
            var actual = input.IsFirstDayOfMonth();

            Assert.IsFalse(actual);
        }
    }
}