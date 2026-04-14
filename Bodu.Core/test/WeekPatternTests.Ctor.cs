// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class WeekPatternTests
    {
        /// <summary>
        /// Verifies that the default parameterless constructor produces an empty pattern with no days selected.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenNoDaysProvided_ShouldBeEmpty()
        {
            var pattern = new WeekPattern();
            Assert.AreEqual(0, pattern.Count);
        }

        /// <summary>
        /// Verifies that passing a <see langword="null" /> array to the constructor produces an empty pattern
        /// rather than throwing.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenNullArrayProvided_ShouldBeEmpty()
        {
            var pattern = new WeekPattern((DayOfWeek[])null);
            Assert.AreEqual(0, pattern.Count);
        }

        /// <summary>
        /// Verifies that passing an empty array to the constructor produces an empty pattern.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenEmptyArrayProvided_ShouldBeEmpty()
        {
            var pattern = new WeekPattern(new DayOfWeek[0]);
            Assert.AreEqual(0, pattern.Count);
        }

        /// <summary>
        /// Verifies that the string constructor throws <see cref="ArgumentNullException" /> when a
        /// <see langword="null" /> string is provided.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenNullStringProvided_ShouldThrowArgumentNullException()
        {
            string input = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new WeekPattern(input);
            });
        }

        /// <summary>
        /// Verifies that the string constructor correctly parses all valid input formats and produces the
        /// expected bitmask value.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetValidParseInputTestData), typeof(WeekPatternTests))]
        public void Constructor_WhenValidStringProvided_ShouldReturnExpected(string input, string _, byte expected)
        {
            var actual = new WeekPattern(input);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that the constructor selects exactly one day and reports a count of one when a single
        /// <see cref="DayOfWeek" /> value is provided.
        /// </summary>
        [TestMethod]
        [DataRow(DayOfWeek.Sunday)]
        [DataRow(DayOfWeek.Monday)]
        [DataRow(DayOfWeek.Tuesday)]
        [DataRow(DayOfWeek.Wednesday)]
        [DataRow(DayOfWeek.Thursday)]
        [DataRow(DayOfWeek.Friday)]
        [DataRow(DayOfWeek.Saturday)]
        public void Constructor_WhenSingleDayProvided_ShouldContainDay(DayOfWeek day)
        {
            var pattern = new WeekPattern(day);
            Assert.IsTrue(pattern.Contains(day));
            Assert.AreEqual(1, pattern.Count);
        }

        /// <summary>
        /// Verifies that the constructor correctly selects all seven days when all valid
        /// <see cref="DayOfWeek" /> values are provided.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenAllSevenDaysProvided_ShouldSelectAllDays()
        {
            var pattern = new WeekPattern(
                DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
                DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday);

            Assert.AreEqual(7, pattern.Count);

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                Assert.IsTrue(pattern.Contains(day), $"{day} should be selected.");
        }

        /// <summary>
        /// Verifies that passing duplicate <see cref="DayOfWeek" /> values to the constructor does not
        /// inflate <see cref="WeekPattern.Count" /> beyond the number of distinct days supplied.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenDuplicateDaysProvided_ShouldDeduplicateDays()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Monday, DayOfWeek.Wednesday);

            Assert.AreEqual(2, pattern.Count);
            Assert.IsTrue(pattern.Contains(DayOfWeek.Monday));
            Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday));
        }

        /// <summary>
        /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when a
        /// <see cref="DayOfWeek" /> value outside the valid range is provided.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(7)]
        [DataRow(99)]
        public void Constructor_WhenInvalidDayOfWeekProvided_ShouldThrowArgumentOutOfRangeException(int invalidDay)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = new WeekPattern((DayOfWeek)invalidDay);
            });
        }
    }
}