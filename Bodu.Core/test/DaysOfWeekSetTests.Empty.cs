// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Empty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Empty" /> reports a count of zero and has no days selected.
        /// </summary>
        [TestMethod]
        public void Empty_WhenAccessed_ShouldBeEmpty()
        {
            var set = DaysOfWeekSet.Empty;
            Assert.AreEqual(0, set.Count);
        }

        /// <summary>
        /// Verifies that two accesses to <see cref="DaysOfWeekSet.Empty" /> return equal values, confirming
        /// the field is stable and consistent across reads.
        /// </summary>
        [TestMethod]
        public void Empty_WhenAccessedMultipleTimes_ShouldReturnConsistentValue()
        {
            var first = DaysOfWeekSet.Empty;
            var second = DaysOfWeekSet.Empty;
            Assert.AreEqual(first, second);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Empty" /> reports <see langword="false" /> for every
        /// individual day indexer.
        /// </summary>
        [TestMethod]
        public void Empty_WhenAccessed_ShouldHaveNoDaysSelected()
        {
            var set = DaysOfWeekSet.Empty;

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                Assert.IsFalse(set[day], $"{day} should not be selected in Empty.");
        }
    }
}