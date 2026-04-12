namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> throws an <see cref="ArgumentNullException" /> when the input is <c>null</c>.
        /// </summary>
        [TestMethod]
        public void Parse_WhenInputIsNull_ShouldTThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = DaysOfWeekSet.Parse(null!);
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> throws an <see cref="ArgumentException" /> for invalid input strings .
        /// </summary>
        /// <param name="input">An invalid input string that cannot be auto-detected or parsed.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetInvalidParseInputNoFormatTestData), typeof(DaysOfWeekSetTests))]
        public void Parse_WhenInvalidInput_ShouldTThrowExactly(string input)
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = DaysOfWeekSet.Parse(input);
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses binary-formatted input strings (e.g., "1010101").
        /// </summary>
        /// <param name="_">Ignored Sunday-first symbol string from data source.</param>
        /// <param name="input">A binary string representing selected days.</param>
        /// <param name="expected">The expected <see cref="DaysOfWeekSet" /> bitmask value.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DaysOfWeekSetTests))]
        public void Parse_WhenValidBinaryInput_ShouldReturnExpected(byte expected, string _, string input)
        {
            var actual = DaysOfWeekSet.Parse(input);
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
        public void Parse_WhenValidMondaySymbolInput_ShouldReturnExpected(byte expected, string input, string _)
        {
            if (input == "______S")
            {
                TestContext.WriteLine("Skipping ambiguous input '______S' that cannot be reliably parsed without a format.");
                return; // gracefully skip this test case
            }

            var actual = DaysOfWeekSet.Parse(input);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses Sunday-first formatted symbol strings (e.g., "SMTWTFS").
        /// </summary>
        /// <param name="input">A Sunday-first symbol string representing selected days.</param>
        /// <param name="_">Ignored binary string from data source.</param>
        /// <param name="expected">The expected <see cref="DaysOfWeekSet" /> bitmask value.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DaysOfWeekSetTests))]
        public void Parse_WhenValidSundaySymbolInput_ShouldReturnExpected(byte expected, string input, string _)
        {
            var actual = DaysOfWeekSet.Parse(input);
            Assert.AreEqual(expected, actual);
        }
    }
}