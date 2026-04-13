// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.GetHashCode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that two <see cref="DaysOfWeekSet" /> instances with identical selected days return the
        /// same hash code.
        /// </summary>
        [TestMethod]
        public void GetHashCode_WhenSameDays_ShouldReturnSameHash()
        {
            var set1 = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            var set2 = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);

            Assert.AreEqual(set1.GetHashCode(), set2.GetHashCode());
        }

        /// <summary>
        /// Verifies that two <see cref="DaysOfWeekSet" /> instances with different selected days return
        /// different hash codes.
        /// </summary>
        [TestMethod]
        public void GetHashCode_WhenDifferentDays_ShouldReturnDifferentHash()
        {
            var set1 = new DaysOfWeekSet(DayOfWeek.Monday);
            var set2 = new DaysOfWeekSet(DayOfWeek.Tuesday);

            Assert.AreNotEqual(set1.GetHashCode(), set2.GetHashCode());
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.GetHashCode" /> returns the same value as the hash code of
        /// its underlying <see cref="byte" /> bitmask across all 128 valid permutations.
        /// </summary>
        [TestMethod]
        public void GetHashCode_WhenCalled_ShouldMatchInternalByteHashCode()
        {
            for (byte value = 0; value <= 127; value++)
            {
                var set = DaysOfWeekSet.FromByte(value);
                Assert.AreEqual(value.GetHashCode(), set.GetHashCode(), $"Hash mismatch for byte value {value}.");
            }
        }
    }
}