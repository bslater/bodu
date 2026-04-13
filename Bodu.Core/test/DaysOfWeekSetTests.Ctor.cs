// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that the default parameterless constructor produces an empty set with no days selected.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenNoDaysProvided_ShouldBeEmpty()
        {
            var set = new DaysOfWeekSet();
            Assert.AreEqual(0, set.Count);
        }

        /// <summary>
        /// Verifies that passing a <see langword="null" /> array to the constructor produces an empty set rather than throwing.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenNullArrayProvided_ShouldBeEmpty()
        {
            var set = new DaysOfWeekSet((DayOfWeek[])null);
            Assert.AreEqual(0, set.Count);
        }

        /// <summary>
        /// Verifies that passing an empty array to the constructor produces an empty set.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenEmptyArrayProvided_ShouldBeEmpty()
        {
            var set = new DaysOfWeekSet(new DayOfWeek[0]);
            Assert.AreEqual(0, set.Count);
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
                _ = new DaysOfWeekSet(input);
            });
        }

        /// <summary>
        /// Verifies that the string constructor correctly parses all valid input formats and produces the
        /// expected bitmask value.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetValidParseInputTestData), typeof(DaysOfWeekSetTests))]
        public void Constructor_WhenValidStringProvided_ShouldReturnExpected(string input, string _, byte expected)
        {
            var actual = new DaysOfWeekSet(input);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that the constructor selects exactly one day when a single <see cref="DayOfWeek" /> value
        /// is provided, and that the resulting set reports a count of one.
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
            var set = new DaysOfWeekSet(day);
            Assert.IsTrue(set[day]);
            Assert.AreEqual(1, set.Count);
        }

        /// <summary>
        /// Verifies that the constructor correctly selects all seven days when all valid <see cref="DayOfWeek" />
        /// values are provided.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenAllSevenDaysProvided_ShouldSelectAllDays()
        {
            var set = new DaysOfWeekSet(
                DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
                DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday);

            Assert.AreEqual(7, set.Count);

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                Assert.IsTrue(set[day], $"{day} should be selected.");
        }

        /// <summary>
        /// Verifies that passing duplicate <see cref="DayOfWeek" /> values to the constructor does not inflate
        /// <see cref="DaysOfWeekSet.Count" /> beyond the number of distinct days supplied.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenDuplicateDaysProvided_ShouldDeduplicateDays()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Monday, DayOfWeek.Wednesday);

            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set[DayOfWeek.Monday]);
            Assert.IsTrue(set[DayOfWeek.Wednesday]);
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
                _ = new DaysOfWeekSet((DayOfWeek)invalidDay);
            });
        }
    }
}