// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Empty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class WeekPatternTests
    {
        /// <summary>
        /// Verifies that <see cref="WeekPattern.Empty" /> reports a count of zero and has no days selected.
        /// </summary>
        [TestMethod]
        public void Empty_WhenAccessed_ShouldBeEmpty()
        {
            Assert.AreEqual(0, WeekPattern.Empty.Count);
        }

        /// <summary>
        /// Verifies that two accesses to <see cref="WeekPattern.Empty" /> return equal values.
        /// </summary>
        [TestMethod]
        public void Empty_WhenAccessedMultipleTimes_ShouldReturnConsistentValue()
        {
            Assert.AreEqual(WeekPattern.Empty, WeekPattern.Empty);
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.Empty" /> reports <see langword="false" /> for every
        /// individual day.
        /// </summary>
        [TestMethod]
        public void Empty_WhenAccessed_ShouldHaveNoDaysSelected()
        {
            var pattern = WeekPattern.Empty;
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                Assert.IsFalse(pattern.Contains(day), $"{day} should not be selected in Empty.");
        }
    }
}