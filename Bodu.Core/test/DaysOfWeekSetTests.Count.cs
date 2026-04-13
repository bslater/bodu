// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Count.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Count" /> returns zero for an empty set.
        /// </summary>
        [TestMethod]
        public void Count_WhenEmpty_ShouldBeZero()
        {
            var set = DaysOfWeekSet.Empty;
            Assert.AreEqual(0, set.Count);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Count" /> returns seven when all days are selected.
        /// </summary>
        [TestMethod]
        public void Count_WhenAllDaysSelected_ShouldBeSeven()
        {
            var set = DaysOfWeekSet.FromByte(127);
            Assert.AreEqual(7, set.Count);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Count" /> accurately reflects the number of days selected
        /// when only a subset of days has been set.
        /// </summary>
        [TestMethod]
        public void Count_WhenSomeDaysSelected_ShouldReflectSelection()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            Assert.AreEqual(2, set.Count);
        }
    }
}