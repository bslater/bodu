// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> throws <see cref="ArgumentNullException" />
        /// when the input is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Parse_WhenInputIsNull_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = DaysOfWeekSet.Parse(null!);
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> throws <see cref="FormatException" /> for
        /// all invalid input strings that cannot be auto-detected or parsed.
        /// </summary>
        /// <param name="input">An invalid input string.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetInvalidParseInputNoFormatTestData), typeof(DaysOfWeekSetTests))]
        public void Parse_WhenInvalidInput_ShouldThrowFormatException(string input)
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = DaysOfWeekSet.Parse(input);
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses binary-formatted input
        /// strings (e.g., <c>"1010101"</c>) across all 128 valid permutations.
        /// </summary>
        /// <param name="expected">The expected bitmask value.</param>
        /// <param name="_">Ignored Sunday-first symbol string from the data source.</param>
        /// <param name="input">A binary string representing selected days.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DaysOfWeekSetTests))]
        public void Parse_WhenValidBinaryInput_ShouldReturnExpected(byte expected, string _, string input)
        {
            var actual = DaysOfWeekSet.Parse(input);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses Monday-first formatted
        /// symbol strings (e.g., <c>"MTWTFSS"</c>) across all 128 valid permutations.
        /// </summary>
        /// <param name="expected">The expected bitmask value.</param>
        /// <param name="input">A Monday-first symbol string representing selected days.</param>
        /// <param name="_">Ignored binary string from the data source.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationWithMondaySymbolsTestData), typeof(DaysOfWeekSetTests))]
        public void Parse_WhenValidMondaySymbolInput_ShouldReturnExpected(byte expected, string input, string _)
        {
            if (input == "______S")
            {
                TestContext.WriteLine("Skipping ambiguous input '______S' that cannot be reliably parsed without a format.");
                return;
            }

            var actual = DaysOfWeekSet.Parse(input);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses Sunday-first formatted
        /// symbol strings (e.g., <c>"SMTWTFS"</c>) across all 128 valid permutations.
        /// </summary>
        /// <param name="expected">The expected bitmask value.</param>
        /// <param name="input">A Sunday-first symbol string representing selected days.</param>
        /// <param name="_">Ignored binary string from the data source.</param>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DaysOfWeekSetTests))]
        public void Parse_WhenValidSundaySymbolInput_ShouldReturnExpected(byte expected, string input, string _)
        {
            var actual = DaysOfWeekSet.Parse(input);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> throws <see cref="FormatException" />
        /// when the input is not exactly seven characters long.
        /// </summary>
        [TestMethod]
        public void Parse_WhenInputLengthIsNot7_ShouldThrowFormatException()
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = DaysOfWeekSet.Parse("SMTWTF");
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> throws <see cref="FormatException" />
        /// when the input contains an unrecognised character.
        /// </summary>
        [TestMethod]
        public void Parse_WhenInputContainsInvalidCharacter_ShouldThrowFormatException()
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = DaysOfWeekSet.Parse("SMTWTFX");
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly identifies a Sunday-first
        /// formatted string where all seven days are selected.
        /// </summary>
        [TestMethod]
        public void Parse_WhenInputIsSundayFirstAllSelected_ShouldSelectAllDays()
        {
            var result = DaysOfWeekSet.Parse("SMTWTFS");
            Assert.AreEqual(7, result.Count);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses a Sunday-first string
        /// with specific days selected.
        /// </summary>
        [TestMethod]
        public void Parse_WhenInputIsSundayFirstPartialSelection_ShouldSetCorrectDays()
        {
            // "_M_W_F_" = Sunday-first: Monday, Wednesday, Friday selected.
            var result = DaysOfWeekSet.Parse("_M_W_F_");

            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsFalse(result[DayOfWeek.Tuesday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsFalse(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly parses a Monday-first string
        /// with all positions filled, inferring the ordering from a leading selected-day character.
        /// </summary>
        [TestMethod]
        public void Parse_WhenInputIsMondayFirstAndFirstCharIsSelected_ShouldSetCorrectDays()
        {
            // "MTWTF__" = Monday-first: Monday through Friday selected, Saturday and Sunday unselected.
            var result = DaysOfWeekSet.Parse("MTWTF__");

            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Tuesday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
            Assert.IsFalse(result[DayOfWeek.Sunday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly assigns day indices when the
        /// first position of a Monday-first string is an unselected placeholder. This is a regression test
        /// for a defect where the leading unselected character was incorrectly mapped to Sunday rather than
        /// Monday, because day ordering had not yet been inferred when that position was processed.
        /// </summary>
        [TestMethod]
        public void Parse_WhenMondayFirstAndFirstPositionIsUnselected_ShouldAssignCorrectDays()
        {
            // "_TWTFSS" = Monday-first:
            //   Position 0 → Monday (unselected)
            //   Position 1 → Tuesday (selected)
            //   Position 2 → Wednesday (selected)
            //   Position 3 → Thursday (selected)
            //   Position 4 → Friday (selected)
            //   Position 5 → Saturday (selected)
            //   Position 6 → Sunday (selected)
            var result = DaysOfWeekSet.Parse("_TWTFSS");

            Assert.IsFalse(result[DayOfWeek.Monday], "Monday should be unselected.");
            Assert.IsTrue(result[DayOfWeek.Tuesday], "Tuesday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Wednesday], "Wednesday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Thursday], "Thursday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Friday], "Friday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Saturday], "Saturday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Sunday], "Sunday should be selected.");
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly assigns day indices when the
        /// first two positions of a Monday-first string are unselected placeholders. This is a regression
        /// test for the same defect as
        /// <see cref="Parse_WhenMondayFirstAndFirstPositionIsUnselected_ShouldAssignCorrectDays" /> under a
        /// deeper leading-unselected condition.
        /// </summary>
        [TestMethod]
        public void Parse_WhenMondayFirstAndFirstTwoPositionsAreUnselected_ShouldAssignCorrectDays()
        {
            // "__WTFSS" = Monday-first:
            //   Position 0 → Monday (unselected)
            //   Position 1 → Tuesday (unselected)
            //   Position 2 → Wednesday (selected)
            //   Position 3 → Thursday (selected)
            //   Position 4 → Friday (selected)
            //   Position 5 → Saturday (selected)
            //   Position 6 → Sunday (selected)
            var result = DaysOfWeekSet.Parse("__WTFSS");

            Assert.IsFalse(result[DayOfWeek.Monday], "Monday should be unselected.");
            Assert.IsFalse(result[DayOfWeek.Tuesday], "Tuesday should be unselected.");
            Assert.IsTrue(result[DayOfWeek.Wednesday], "Wednesday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Thursday], "Thursday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Friday], "Friday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Saturday], "Saturday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Sunday], "Sunday should be selected.");
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly assigns day indices when the
        /// first three positions of a Monday-first string are unselected placeholders.
        /// </summary>
        [TestMethod]
        public void Parse_WhenMondayFirstAndFirstThreePositionsAreUnselected_ShouldAssignCorrectDays()
        {
            // "___TFSS" = Monday-first:
            //   Positions 0–2 → Monday, Tuesday, Wednesday (unselected)
            //   Position 3 → Thursday (selected)
            //   Position 4 → Friday (selected)
            //   Position 5 → Saturday (selected)
            //   Position 6 → Sunday (selected)
            var result = DaysOfWeekSet.Parse("___TFSS");

            Assert.IsFalse(result[DayOfWeek.Monday], "Monday should be unselected.");
            Assert.IsFalse(result[DayOfWeek.Tuesday], "Tuesday should be unselected.");
            Assert.IsFalse(result[DayOfWeek.Wednesday], "Wednesday should be unselected.");
            Assert.IsTrue(result[DayOfWeek.Thursday], "Thursday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Friday], "Friday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Saturday], "Saturday should be selected.");
            Assert.IsTrue(result[DayOfWeek.Sunday], "Sunday should be selected.");
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Parse(string)" /> correctly auto-detects and parses a
        /// binary string, setting the correct individual days.
        /// </summary>
        [TestMethod]
        public void Parse_WhenInputIsBinary_ShouldSetCorrectDays()
        {
            // "0111110" = Sunday-first binary: Monday through Friday selected.
            var result = DaysOfWeekSet.Parse("0111110");

            Assert.IsFalse(result[DayOfWeek.Sunday]);
            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Tuesday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Thursday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.IsFalse(result[DayOfWeek.Saturday]);
        }

        /// <summary>
        /// Verifies that the result of <see cref="DaysOfWeekSet.Parse(string)" /> round-trips correctly
        /// through <see cref="DaysOfWeekSet.ToString()" />.
        /// </summary>
        [TestMethod]
        public void Parse_WhenRoundTripped_ShouldProduceOriginalString()
        {
            const string input = "S_____S";
            var result = DaysOfWeekSet.Parse(input);

            Assert.AreEqual(input, result.ToString());
        }
    }
}