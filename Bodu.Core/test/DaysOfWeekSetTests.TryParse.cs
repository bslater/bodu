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

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParse" /> returns <see langword="true" /> and sets the correct result for a
        /// valid Sunday-first input string.
        /// </summary>
        [TestMethod]
        public void TryParse_WhenInputIsValidSundayFirst_ShouldReturnTrueAndSetCorrectDays()
        {
            bool success = DaysOfWeekSet.TryParse("_M_W_F_", out DaysOfWeekSet result);

            Assert.IsTrue(success);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.AreEqual(3, result.Count);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParse" /> returns <see langword="false" /> and sets the result to
        /// <see cref="DaysOfWeekSet.Empty" /> when the input is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TryParse_WhenInputIsNull_ShouldReturnFalseAndSetEmpty()
        {
            bool success = DaysOfWeekSet.TryParse(null, out DaysOfWeekSet result);

            Assert.IsFalse(success);
            Assert.AreEqual(DaysOfWeekSet.Empty, result);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParse" /> returns <see langword="false" /> and sets the result to
        /// <see cref="DaysOfWeekSet.Empty" /> when the input has an invalid length.
        /// </summary>
        [TestMethod]
        public void TryParse_WhenInputHasInvalidLength_ShouldReturnFalseAndSetEmpty()
        {
            bool success = DaysOfWeekSet.TryParse("SMTWTF", out DaysOfWeekSet result);

            Assert.IsFalse(success);
            Assert.AreEqual(DaysOfWeekSet.Empty, result);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParse" /> returns <see langword="false" /> and sets the result to
        /// <see cref="DaysOfWeekSet.Empty" /> when the input contains an unrecognised character.
        /// </summary>
        [TestMethod]
        public void TryParse_WhenInputContainsInvalidCharacter_ShouldReturnFalseAndSetEmpty()
        {
            bool success = DaysOfWeekSet.TryParse("SMTWTFX", out DaysOfWeekSet result);

            Assert.IsFalse(success);
            Assert.AreEqual(DaysOfWeekSet.Empty, result);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParse" /> correctly auto-detects and parses a binary input string.
        /// </summary>
        [TestMethod]
        public void TryParse_WhenInputIsBinary_ShouldReturnTrueAndSetCorrectDays()
        {
            // "0111110" = binary: Monday through Friday selected
            bool success = DaysOfWeekSet.TryParse("0111110", out DaysOfWeekSet result);

            Assert.IsTrue(success);
            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.TryParse" /> returns <see langword="true" /> and sets an empty result for an
        /// all-unselected Sunday-first string.
        /// </summary>
        [TestMethod]
        public void TryParse_WhenInputRepresentsNoDaysSelected_ShouldReturnTrueAndSetEmpty()
        {
            bool success = DaysOfWeekSet.TryParse("_______", out DaysOfWeekSet result);

            Assert.IsTrue(success);
            Assert.AreEqual(0, result.Count);
        }
    }
}