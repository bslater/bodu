// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Clear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Clear" /> removes all selected days from a non-empty set,
        /// leaving <see cref="DaysOfWeekSet.Count" /> equal to zero.
        /// </summary>
        [TestMethod]
        public void Clear_WhenCalled_ShouldRemoveAllDays()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Tuesday);
            set.Clear();
            Assert.AreEqual(0, set.Count);
        }

        /// <summary>
        /// Verifies that calling <see cref="DaysOfWeekSet.Clear" /> on an already-empty set leaves it empty
        /// and does not throw.
        /// </summary>
        [TestMethod]
        public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
        {
            var set = DaysOfWeekSet.Empty;
            set.Clear();
            Assert.AreEqual(0, set.Count);
        }

        /// <summary>
        /// Verifies that after calling <see cref="DaysOfWeekSet.Clear" /> on a fully-populated set, every
        /// individual day indexer returns <see langword="false" />.
        /// </summary>
        [TestMethod]
        public void Clear_WhenCalled_ShouldUnsetAllIndividualDays()
        {
            var set = DaysOfWeekSet.FromByte(0b1111111);
            set.Clear();

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                Assert.IsFalse(set[day], $"{day} should be unselected after Clear.");
        }
    }
}