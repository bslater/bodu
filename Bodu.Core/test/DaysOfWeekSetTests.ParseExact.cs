namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses Sunday-first formatted symbol strings (e.g., "SMTWTFS").
        /// </summary>
        /// <param name="input">A Sunday-first symbol string representing selected days.</param>
        /// <param name="_">Ignored binary string from data source.</param>
        /// <param name="expected">The expected <see cref="DaysOfWeekSet" /> bitmask value.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DaysOfWeekSetTests))]
        public void ParseExact_WhenValidSundaySymbolInput_ShouldReturnExpected(byte expected, string input, string _)
        {
            var actual = DaysOfWeekSet.ParseExact(input, "S");
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses Monday-first formatted symbol strings (e.g., "MTWTFSS").
        /// </summary>
        /// <param name="input">A Monday-first symbol string representing selected days.</param>
        /// <param name="_">Ignored binary string from data source.</param>
        /// <param name="expected">The expected <see cref="DaysOfWeekSet" /> bitmask value.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationWithMondaySymbolsTestData), typeof(DaysOfWeekSetTests))]
        public void ParseExact_WhenValidMondaySymbolInput_ShouldReturnExpected(byte expected, string input, string _)
        {
            var actual = DaysOfWeekSet.ParseExact(input, "M");
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses binary-formatted input strings (e.g., "1010101").
        /// </summary>
        /// <param name="_">Ignored Sunday-first symbol string from data source.</param>
        /// <param name="input">A binary string representing selected days.</param>
        /// <param name="expected">The expected <see cref="DaysOfWeekSet" /> bitmask value.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DaysOfWeekSetTests))]
        public void ParseExact_WhenValidBinaryInput_ShouldReturnExpected(byte expected, string _, string input)
        {
            var actual = DaysOfWeekSet.ParseExact(input, "B");
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> throws a <see cref="FormatException" /> when the format
        /// string is invalid or unsupported.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetInvalidFormatSpecifierTestData), typeof(DaysOfWeekSetTests))]
        public void ParseExact_WhenInvalidFormatSpecifier_ShouldThrowExactly(string input, string format)
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                DaysOfWeekSet.ParseExact(input, format);
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> throws a <see cref="ArgumentNullException" /> when the
        /// input is <c>null</c>.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenInputIsNull_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = DaysOfWeekSet.ParseExact(null!, "S");
            });
        }
    }
}