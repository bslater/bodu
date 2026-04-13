// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Weekend.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Weekend" /> contains exactly Saturday and Sunday, with all
        /// weekday days unselected, and reports a count of two.
        /// </summary>
        [TestMethod]
        public void Weekend_WhenAccessed_ShouldContainWeekendDays()
        {
            var set = DaysOfWeekSet.Weekend;

            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set[DayOfWeek.Saturday]);
            Assert.IsTrue(set[DayOfWeek.Sunday]);
            Assert.IsFalse(set[DayOfWeek.Monday]);
            Assert.IsFalse(set[DayOfWeek.Tuesday]);
            Assert.IsFalse(set[DayOfWeek.Wednesday]);
            Assert.IsFalse(set[DayOfWeek.Thursday]);
            Assert.IsFalse(set[DayOfWeek.Friday]);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Weekend" /> returns a consistent value across multiple
        /// accesses.
        /// </summary>
        [TestMethod]
        public void Weekend_WhenAccessedMultipleTimes_ShouldReturnConsistentValue()
        {
            var first = DaysOfWeekSet.Weekend;
            var second = DaysOfWeekSet.Weekend;
            Assert.AreEqual(first, second);
        }
    }
}