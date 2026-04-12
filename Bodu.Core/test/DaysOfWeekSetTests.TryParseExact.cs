namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParseExact(string, string, out DaysOfWeekSet)" /> returns the expected success flag
        /// and value for various valid and invalid combinations of input and format.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetTryParseExactTestData), typeof(DaysOfWeekSetTests))]
        public void TryParseExact_WhenGivenInputAndFormat_ShouldReturnExpectedResultAndParsedValue(string input, string format, byte expected, bool isValid)
        {
            bool success = DaysOfWeekSet.TryParseExact(input, format, out var actual);
            Assert.AreEqual(isValid, success);

            if (success)
            {
                Assert.AreEqual(expected, actual);
            }
            else
            {
                Assert.AreEqual(DaysOfWeekSet.Empty, actual);
            }
        }
    }
}