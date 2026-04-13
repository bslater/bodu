// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.ParseExact.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> correctly parses Sunday-first
        /// formatted symbol strings (e.g., <c>"SMTWTFS"</c>) across all 128 valid permutations when the
        /// <c>'S'</c> format specifier is provided.
        /// </summary>
        /// <param name="expected">The expected bitmask value.</param>
        /// <param name="input">A Sunday-first symbol string representing selected days.</param>
        /// <param name="_">Ignored binary string from the data source.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DaysOfWeekSetTests))]
        public void ParseExact_WhenValidSundaySymbolInput_ShouldReturnExpected(byte expected, string input, string _)
        {
            var actual = DaysOfWeekSet.ParseExact(input, "S");
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> correctly parses Monday-first
        /// formatted symbol strings (e.g., <c>"MTWTFSS"</c>) across all 128 valid permutations when the
        /// <c>'M'</c> format specifier is provided.
        /// </summary>
        /// <param name="expected">The expected bitmask value.</param>
        /// <param name="input">A Monday-first symbol string representing selected days.</param>
        /// <param name="_">Ignored binary string from the data source.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationWithMondaySymbolsTestData), typeof(DaysOfWeekSetTests))]
        public void ParseExact_WhenValidMondaySymbolInput_ShouldReturnExpected(byte expected, string input, string _)
        {
            var actual = DaysOfWeekSet.ParseExact(input, "M");
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> correctly parses
        /// binary-formatted input strings (e.g., <c>"1010101"</c>) across all 128 valid permutations when
        /// the <c>'B'</c> format specifier is provided.
        /// </summary>
        /// <param name="expected">The expected bitmask value.</param>
        /// <param name="_">Ignored Sunday-first symbol string from the data source.</param>
        /// <param name="input">A binary string representing selected days.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DaysOfWeekSetTests))]
        public void ParseExact_WhenValidBinaryInput_ShouldReturnExpected(byte expected, string _, string input)
        {
            var actual = DaysOfWeekSet.ParseExact(input, "B");
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> throws
        /// <see cref="FormatException" /> when the format string is invalid or unsupported.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetInvalidFormatSpecifierTestData), typeof(DaysOfWeekSetTests))]
        public void ParseExact_WhenInvalidFormatSpecifier_ShouldThrowFormatException(string input, string format)
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = DaysOfWeekSet.ParseExact(input, format);
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> throws
        /// <see cref="ArgumentNullException" /> when the input is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenInputIsNull_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = DaysOfWeekSet.ParseExact(null!, "S");
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> throws
        /// <see cref="ArgumentNullException" /> when the format is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenFormatIsNull_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = DaysOfWeekSet.ParseExact("_M_W_F_", null);
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> throws
        /// <see cref="FormatException" /> when the format string is unrecognised.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenFormatIsUnrecognised_ShouldThrowFormatException()
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = DaysOfWeekSet.ParseExact("_M_W_F_", "Z");
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> throws
        /// <see cref="FormatException" /> when the format string is empty.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenFormatIsEmpty_ShouldThrowFormatException()
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = DaysOfWeekSet.ParseExact("_M_W_F_", string.Empty);
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> correctly parses a
        /// Sunday-first string using the <c>'S'</c> format specifier.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenFormatIsSundayFirst_ShouldSetCorrectDays()
        {
            var result = DaysOfWeekSet.ParseExact("_M_W_F_", "S");

            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsFalse(result[DayOfWeek.Tuesday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsFalse(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> correctly parses a
        /// Monday-first string using the <c>'M'</c> format specifier.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenFormatIsMondayFirst_ShouldSetCorrectDays()
        {
            // "MTWTF__" = Monday-first: Monday through Friday selected.
            var result = DaysOfWeekSet.ParseExact("MTWTF__", "M");

            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Tuesday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
            Assert.IsFalse(result[DayOfWeek.Sunday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> correctly parses a binary
        /// string when the format specifier is <c>'0'</c>. This is a regression test for a defect where this
        /// documented specifier was not recognised and caused a <see cref="FormatException" /> to be thrown.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenBinaryFormatSpecifierIs0_ShouldParseBinaryString()
        {
            // "0111110" = binary Sunday-first: Monday through Friday selected.
            var result = DaysOfWeekSet.ParseExact("0111110", "0");

            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Tuesday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> correctly parses a binary
        /// string when the format specifier is <c>'1'</c>. This is a regression test for the same defect as
        /// <see cref="ParseExact_WhenBinaryFormatSpecifierIs0_ShouldParseBinaryString" />.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenBinaryFormatSpecifierIs1_ShouldParseBinaryString()
        {
            var result = DaysOfWeekSet.ParseExact("0111110", "1");

            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Tuesday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> correctly parses a binary
        /// string when the format specifier is <c>"01"</c>. This is a regression test for the same defect as
        /// <see cref="ParseExact_WhenBinaryFormatSpecifierIs0_ShouldParseBinaryString" />.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenBinaryFormatSpecifierIs01_ShouldParseBinaryString()
        {
            var result = DaysOfWeekSet.ParseExact("0111110", "01");

            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> correctly parses a binary
        /// string when the format specifier is <c>'B'</c>.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenBinaryFormatSpecifierIsB_ShouldParseBinaryString()
        {
            var result = DaysOfWeekSet.ParseExact("0111110", "B");

            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Tuesday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that all recognised binary format specifiers (<c>'0'</c>, <c>'1'</c>, <c>'B'</c>,
        /// <c>"01"</c>) produce identical results when applied to the same input string.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenAllBinarySpecifiersAppliedToSameInput_ShouldProduceIdenticalResults()
        {
            const string input = "1010101"; // Sun, Tue, Thu, Sat selected.

            var r0 = DaysOfWeekSet.ParseExact(input, "0");
            var r1 = DaysOfWeekSet.ParseExact(input, "1");
            var rB = DaysOfWeekSet.ParseExact(input, "B");
            var r01 = DaysOfWeekSet.ParseExact(input, "01");

            Assert.AreEqual(r0, r1, "Formats '0' and '1' should yield the same result.");
            Assert.AreEqual(r0, rB, "Formats '0' and 'B' should yield the same result.");
            Assert.AreEqual(r0, r01, "Formats '0' and \"01\" should yield the same result.");
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> throws
        /// <see cref="FormatException" /> when a binary input contains an invalid character.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenBinaryInputContainsInvalidCharacter_ShouldThrowFormatException()
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = DaysOfWeekSet.ParseExact("0111X10", "0");
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ParseExact(string, string)" /> with the two-character dash
        /// format specifier (<c>"MD"</c>) correctly parses a Monday-first string using dash as the unselected
        /// placeholder.
        /// </summary>
        [TestMethod]
        public void ParseExact_WhenFormatIsMondayFirstWithDash_ShouldSetCorrectDays()
        {
            var result = DaysOfWeekSet.ParseExact("M-W-F--", "MD");

            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsFalse(result[DayOfWeek.Tuesday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsFalse(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
            Assert.IsFalse(result[DayOfWeek.Sunday]);
        }
    }
}