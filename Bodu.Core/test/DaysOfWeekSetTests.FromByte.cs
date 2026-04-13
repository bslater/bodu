// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.FromByte.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.FromByte" /> creates a set with the expected
        /// <see cref="DaysOfWeekSet.Count" /> for representative valid bitmask values.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0, 0)]
        [DataRow((byte)1, 1)]
        [DataRow((byte)127, 7)]
        public void FromByte_WhenValidValue_ShouldCreateExpectedSet(byte input, int expectedCount)
        {
            var set = DaysOfWeekSet.FromByte(input);
            Assert.AreEqual(expectedCount, set.Count);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.FromByte" /> correctly maps each bit position of a known
        /// bitmask to the corresponding <see cref="DayOfWeek" />, using Sunday-first ordering.
        /// </summary>
        [TestMethod]
        public void FromByte_WhenKnownBitmask_ShouldSelectCorrectDays()
        {
            // 0b0111110 = Monday through Friday in Sunday-first bit order
            var set = DaysOfWeekSet.FromByte(0b0111110);

            Assert.IsFalse(set[DayOfWeek.Sunday], "Sunday should not be selected.");
            Assert.IsTrue(set[DayOfWeek.Monday], "Monday should be selected.");
            Assert.IsTrue(set[DayOfWeek.Tuesday], "Tuesday should be selected.");
            Assert.IsTrue(set[DayOfWeek.Wednesday], "Wednesday should be selected.");
            Assert.IsTrue(set[DayOfWeek.Thursday], "Thursday should be selected.");
            Assert.IsTrue(set[DayOfWeek.Friday], "Friday should be selected.");
            Assert.IsFalse(set[DayOfWeek.Saturday], "Saturday should not be selected.");
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.FromByte" /> throws <see cref="ArgumentOutOfRangeException" />
        /// when the supplied value exceeds the maximum valid bitmask of 127.
        /// </summary>
        [TestMethod]
        public void FromByte_WhenValueGreaterThanMax_ShouldThrowArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DaysOfWeekSet.FromByte(128));
        }
    }
}