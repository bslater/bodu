namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParse(string, out DaysOfWeekSet)" /> returns the expected success flag and parsed
        /// value for valid and invalid inputs using auto-detected format.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetTryParseTestData), typeof(DaysOfWeekSetTests))]
        public void TryParse_WhenGivenInput_ShouldReturnExpectedResultAndParsedValue(string input, byte expected, bool isValid)
        {
            bool success = DaysOfWeekSet.TryParse(input, out var actual);
            Assert.AreEqual(isValid, success);

            if (success)
            {
                Assert.AreEqual(expected, (byte)actual);
            }
            else
            {
                Assert.AreEqual(DaysOfWeekSet.Empty, actual);
            }
        }
    }
}