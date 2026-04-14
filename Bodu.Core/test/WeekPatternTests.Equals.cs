// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Equals.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class WeekPatternTests
    {
        /// <summary>
        /// Verifies that <see cref="WeekPattern.Equals(object)" /> returns <see langword="false" /> when the
        /// argument is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void EqualsObject_WhenNull_ShouldReturnFalse()
        {
            Assert.IsFalse(new WeekPattern(DayOfWeek.Monday).Equals(null));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.Equals(object)" /> returns <see langword="false" /> when the
        /// argument is of an incompatible type.
        /// </summary>
        [TestMethod]
        public void EqualsObject_WhenDifferentType_ShouldReturnFalse()
        {
            Assert.IsFalse(new WeekPattern(DayOfWeek.Monday).Equals("not a WeekPattern"));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.Equals(object)" /> returns the expected result when the
        /// argument is a boxed <see cref="WeekPattern" />.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0, (byte)0, true)]
        [DataRow((byte)1, (byte)1, true)]
        [DataRow((byte)0, (byte)1, false)]
        [DataRow((byte)5, (byte)10, false)]
        public void EqualsObject_WhenComparingWeekPattern_ShouldReturnExpectedResult(byte first, byte second, bool expected)
        {
            var p1 = WeekPattern.FromByte(first);
            var p2 = WeekPattern.FromByte(second);
            Assert.AreEqual(expected, p1.Equals((object)p2));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.Equals(object)" /> returns the expected result when the
        /// argument is a boxed <see cref="byte" />, exercising the byte-dispatch branch added to the
        /// <c>Equals(object)</c> overload.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0, (byte)0, true)]
        [DataRow((byte)1, (byte)1, true)]
        [DataRow((byte)127, (byte)127, true)]
        [DataRow((byte)7, (byte)5, false)]
        [DataRow((byte)0, (byte)1, false)]
        public void EqualsObject_WhenBoxedByte_ShouldReturnExpectedResult(byte patternMask, byte compareMask, bool expected)
        {
            var pattern = WeekPattern.FromByte(patternMask);
            Assert.AreEqual(expected, pattern.Equals((object)compareMask));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.Equals(WeekPattern)" /> returns the expected result when
        /// comparing two instances by value.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0, (byte)0, true)]
        [DataRow((byte)1, (byte)1, true)]
        [DataRow((byte)0, (byte)1, false)]
        [DataRow((byte)7, (byte)5, false)]
        public void EqualsWeekPattern_WhenComparing_ShouldReturnExpectedResult(byte first, byte second, bool expected)
        {
            Assert.AreEqual(expected, WeekPattern.FromByte(first).Equals(WeekPattern.FromByte(second)));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.Equals(byte)" /> returns the expected result when comparing
        /// against a raw bitmask using the typed overload directly.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0, (byte)0, true)]
        [DataRow((byte)1, (byte)1, true)]
        [DataRow((byte)127, (byte)127, true)]
        [DataRow((byte)3, (byte)7, false)]
        [DataRow((byte)5, (byte)10, false)]
        public void EqualsByte_WhenComparing_ShouldReturnExpectedResult(byte first, byte second, bool expected)
        {
            Assert.AreEqual(expected, WeekPattern.FromByte(first).Equals(second));
        }
    }
}