// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Weekdays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Weekdays" /> contains exactly Monday through Friday, with
        /// Saturday and Sunday unselected, and reports a count of five.
        /// </summary>
        [TestMethod]
        public void Weekdays_WhenAccessed_ShouldContainWeekdays()
        {
            var set = DaysOfWeekSet.Weekdays;

            Assert.AreEqual(5, set.Count);
            Assert.IsTrue(set[DayOfWeek.Monday]);
            Assert.IsTrue(set[DayOfWeek.Tuesday]);
            Assert.IsTrue(set[DayOfWeek.Wednesday]);
            Assert.IsTrue(set[DayOfWeek.Thursday]);
            Assert.IsTrue(set[DayOfWeek.Friday]);
            Assert.IsFalse(set[DayOfWeek.Saturday]);
            Assert.IsFalse(set[DayOfWeek.Sunday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Weekdays" /> returns a consistent value across multiple
        /// accesses.
        /// </summary>
        [TestMethod]
        public void Weekdays_WhenAccessedMultipleTimes_ShouldReturnConsistentValue()
        {
            var first = DaysOfWeekSet.Weekdays;
            var second = DaysOfWeekSet.Weekdays;
            Assert.AreEqual(first, second);
        }
    }
}