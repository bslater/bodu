// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Equals.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Equals(object)" /> returns <see langword="false" /> when
        /// the argument is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void EqualsObject_WhenNull_ShouldReturnFalse()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            Assert.IsFalse(set.Equals(null));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Equals(object)" /> returns <see langword="false" /> when
        /// the argument is of an incompatible type.
        /// </summary>
        [TestMethod]
        public void EqualsObject_WhenDifferentType_ShouldReturnFalse()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            Assert.IsFalse(set.Equals("not a DaysOfWeekSet"));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Equals(object)" /> returns the expected result when the
        /// argument is a boxed <see cref="DaysOfWeekSet" />.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0, (byte)0, true)]
        [DataRow((byte)1, (byte)1, true)]
        [DataRow((byte)0, (byte)1, false)]
        [DataRow((byte)5, (byte)10, false)]
        public void EqualsObject_WhenComparingDaysOfWeekSet_ShouldReturnExpectedResult(byte first, byte second, bool expected)
        {
            var set1 = DaysOfWeekSet.FromByte(first);
            var set2 = DaysOfWeekSet.FromByte(second);

            Assert.AreEqual(expected, set1.Equals((object)set2));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Equals(object)" /> returns <see langword="false" /> when
        /// the argument is a boxed <see cref="byte" />. The <c>Equals(object)</c> overload only dispatches
        /// on the <see cref="DaysOfWeekSet" /> type and has no <see cref="byte" /> branch; a boxed byte
        /// therefore never compares equal via this path. Use <see cref="DaysOfWeekSet.Equals(byte)" />
        /// directly when comparing against a raw bitmask value.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        [DataRow((byte)127)]
        public void EqualsObject_WhenBoxedByte_ShouldReturnFalse(byte value)
        {
            var set = DaysOfWeekSet.FromByte(value);
            Assert.IsFalse(set.Equals((object)value),
                "Equals(object) must return false for a boxed byte — use Equals(byte) for typed comparison.");
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Equals(DaysOfWeekSet)" /> returns the expected result when
        /// comparing two <see cref="DaysOfWeekSet" /> instances by value.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0, (byte)0, true)]
        [DataRow((byte)1, (byte)1, true)]
        [DataRow((byte)0, (byte)1, false)]
        [DataRow((byte)7, (byte)5, false)]
        public void EqualsDaysOfWeekSet_WhenComparing_ShouldReturnExpectedResult(byte first, byte second, bool expected)
        {
            var set1 = DaysOfWeekSet.FromByte(first);
            var set2 = DaysOfWeekSet.FromByte(second);

            Assert.AreEqual(expected, set1.Equals(set2));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.Equals(byte)" /> returns the expected result when comparing
        /// the instance against a raw <see cref="byte" /> bitmask using the typed overload directly.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0, (byte)0, true)]
        [DataRow((byte)1, (byte)1, true)]
        [DataRow((byte)127, (byte)127, true)]
        [DataRow((byte)3, (byte)7, false)]
        [DataRow((byte)5, (byte)10, false)]
        public void EqualsByte_WhenComparing_ShouldReturnExpectedResult(byte first, byte second, bool expected)
        {
            var set = DaysOfWeekSet.FromByte(first);
            Assert.AreEqual(expected, set.Equals(second));
        }
    }
}