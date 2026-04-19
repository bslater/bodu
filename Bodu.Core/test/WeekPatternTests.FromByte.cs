// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.FromByte.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class WeekPatternTests
    {
        /// <summary>
        /// Verifies that <see cref="WeekPattern.FromByte" /> creates a pattern with the expected
        /// <see cref="WeekPattern.Count" /> for representative valid bitmask values.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0, 0)]
        [DataRow((byte)1, 1)]
        [DataRow((byte)127, 7)]
        public void FromByte_WhenValidValue_ShouldCreateExpectedPattern(byte input, int expectedCount)
        {
            Assert.AreEqual(expectedCount, WeekPattern.FromByte(input).Count);
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.FromByte" /> correctly maps each bit position of a known
        /// bitmask to the corresponding <see cref="DayOfWeek" />, using Sunday-first bit ordering.
        /// </summary>
        [TestMethod]
        public void FromByte_WhenKnownBitmask_ShouldSelectCorrectDays()
        {
            // 0b0111110 = Monday through Friday in Sunday-first bit order.
            var pattern = WeekPattern.FromByte(0b0111110);

            Assert.IsFalse(pattern.Contains(DayOfWeek.Sunday), "Sunday should not be selected.");
            Assert.IsTrue(pattern.Contains(DayOfWeek.Monday), "Monday should be selected.");
            Assert.IsTrue(pattern.Contains(DayOfWeek.Tuesday), "Tuesday should be selected.");
            Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday), "Wednesday should be selected.");
            Assert.IsTrue(pattern.Contains(DayOfWeek.Thursday), "Thursday should be selected.");
            Assert.IsTrue(pattern.Contains(DayOfWeek.Friday), "Friday should be selected.");
            Assert.IsFalse(pattern.Contains(DayOfWeek.Saturday), "Saturday should not be selected.");
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.FromByte" /> throws <see cref="ArgumentOutOfRangeException" />
        /// when the supplied value exceeds the maximum valid bitmask of 127.
        /// </summary>
        [TestMethod]
        public void FromByte_WhenValueGreaterThanMax_ShouldThrowException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WeekPattern.FromByte(128));
        }
    }
}