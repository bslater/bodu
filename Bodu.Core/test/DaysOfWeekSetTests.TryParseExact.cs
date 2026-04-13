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

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParseExact" /> returns <see langword="true" /> and sets the correct result
        /// for a valid Sunday-first input with the <c>'S'</c> format specifier.
        /// </summary>
        [TestMethod]
        public void TryParseExact_WhenFormatIsSundayFirstAndInputIsValid_ShouldReturnTrueAndSetCorrectDays()
        {
            bool success = DaysOfWeekSet.TryParseExact("_M_W_F_", "S", out DaysOfWeekSet result);

            Assert.IsTrue(success);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.AreEqual(3, result.Count);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParseExact" /> returns <see langword="false" /> and sets the result to
        /// <see cref="DaysOfWeekSet.Empty" /> when the format is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TryParseExact_WhenFormatIsNull_ShouldReturnFalseAndSetEmpty()
        {
            bool success = DaysOfWeekSet.TryParseExact("_M_W_F_", null, out DaysOfWeekSet result);

            Assert.IsFalse(success);
            Assert.AreEqual(DaysOfWeekSet.Empty, result);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParseExact" /> returns <see langword="false" /> and sets the result to
        /// <see cref="DaysOfWeekSet.Empty" /> when the format is unrecognised.
        /// </summary>
        [TestMethod]
        public void TryParseExact_WhenFormatIsUnrecognised_ShouldReturnFalseAndSetEmpty()
        {
            bool success = DaysOfWeekSet.TryParseExact("_M_W_F_", "Z", out DaysOfWeekSet result);

            Assert.IsFalse(success);
            Assert.AreEqual(DaysOfWeekSet.Empty, result);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParseExact" /> returns <see langword="false" /> and sets the result to
        /// <see cref="DaysOfWeekSet.Empty" /> when the input is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TryParseExact_WhenInputIsNull_ShouldReturnFalseAndSetEmpty()
        {
            bool success = DaysOfWeekSet.TryParseExact(null, "S", out DaysOfWeekSet result);

            Assert.IsFalse(success);
            Assert.AreEqual(DaysOfWeekSet.Empty, result);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParseExact" /> returns <see langword="true" /> and correctly parses a binary
        /// string when the format specifier is <c>'0'</c>. This is a regression test for a defect where this documented specifier
        /// was not recognised, causing the method to incorrectly return <see langword="false" />.
        /// </summary>
        [TestMethod]
        public void TryParseExact_WhenBinaryFormatSpecifierIs0_ShouldReturnTrueAndSetCorrectDays()
        {
            bool success = DaysOfWeekSet.TryParseExact("0111110", "0", out DaysOfWeekSet result);

            Assert.IsTrue(success, "Format specifier '0' should be recognised as binary.");
            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParseExact" /> returns <see langword="true" /> and correctly parses a binary
        /// string when the format specifier is <c>'1'</c>. This is a regression test for the same defect as
        /// <see cref="TryParseExact_WhenBinaryFormatSpecifierIs0_ShouldReturnTrueAndSetCorrectDays" />.
        /// </summary>
        [TestMethod]
        public void TryParseExact_WhenBinaryFormatSpecifierIs1_ShouldReturnTrueAndSetCorrectDays()
        {
            bool success = DaysOfWeekSet.TryParseExact("0111110", "1", out DaysOfWeekSet result);

            Assert.IsTrue(success, "Format specifier '1' should be recognised as binary.");
            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParseExact" /> returns <see langword="true" /> and correctly parses a binary
        /// string when the format specifier is <c>"01"</c>. This is a regression test for the same defect as
        /// <see cref="TryParseExact_WhenBinaryFormatSpecifierIs0_ShouldReturnTrueAndSetCorrectDays" />.
        /// </summary>
        [TestMethod]
        public void TryParseExact_WhenBinaryFormatSpecifierIs01_ShouldReturnTrueAndSetCorrectDays()
        {
            bool success = DaysOfWeekSet.TryParseExact("0111110", "01", out DaysOfWeekSet result);

            Assert.IsTrue(success, "Format specifier \"01\" should be recognised as binary.");
            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParseExact" /> returns <see langword="false" /> when the binary input
        /// contains an invalid character.
        /// </summary>
        [TestMethod]
        public void TryParseExact_WhenBinaryInputContainsInvalidCharacter_ShouldReturnFalseAndSetEmpty()
        {
            bool success = DaysOfWeekSet.TryParseExact("0111X10", "0", out DaysOfWeekSet result);

            Assert.IsFalse(success);
            Assert.AreEqual(DaysOfWeekSet.Empty, result);
        }
    }
}