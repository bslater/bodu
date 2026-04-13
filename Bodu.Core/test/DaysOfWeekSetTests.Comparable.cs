// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Comparable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.CompareTo(object)" /> returns a positive value when the
        /// argument is <see langword="null" />, consistent with the convention that any instance is greater
        /// than null.
        /// </summary>
        [TestMethod]
        public void CompareToObject_WhenNull_ShouldReturnGreaterThanZero()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            int actual = set.CompareTo(null);
            Assert.IsTrue(actual > 0);
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.CompareTo(object)" /> returns a value whose sign correctly
        /// reflects the ordinal relationship between the two instances when the argument is a boxed
        /// <see cref="DaysOfWeekSet" />.
        /// </summary>
        [DataTestMethod]
        [DataRow((byte)0, (byte)0, 0)]
        [DataRow((byte)1, (byte)0, 1)]
        [DataRow((byte)0, (byte)1, -1)]
        [DataRow((byte)5, (byte)5, 0)]
        [DataRow((byte)3, (byte)7, -1)]
        [DataRow((byte)7, (byte)3, 1)]
        public void CompareToObject_WhenValidDaysOfWeekSet_ShouldCompareCorrectly(byte first, byte second, int expectedSign)
        {
            var set1 = DaysOfWeekSet.FromByte(first);
            var set2 = DaysOfWeekSet.FromByte(second);

            int actual = set1.CompareTo((object)set2);

            Assert.AreEqual(Math.Sign(expectedSign), Math.Sign(actual));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.CompareTo(object)" /> returns a value whose sign correctly
        /// reflects the ordinal relationship when the argument is a boxed <see cref="byte" />.
        /// </summary>
        [DataTestMethod]
        [DataRow((byte)0, (byte)0, 0)]
        [DataRow((byte)1, (byte)0, 1)]
        [DataRow((byte)0, (byte)1, -1)]
        [DataRow((byte)5, (byte)5, 0)]
        public void CompareToObject_WhenByte_ShouldCompareCorrectly(byte first, byte second, int expectedSign)
        {
            var set = DaysOfWeekSet.FromByte(first);

            int actual = set.CompareTo((object)second);

            Assert.AreEqual(Math.Sign(expectedSign), Math.Sign(actual));
        }

        /// <summary>
        /// Provides object values of types that are incompatible with <see cref="DaysOfWeekSet.CompareTo(object)" />.
        /// </summary>
        public static IEnumerable<object[]> InvalidTypesForCompareTo => new[]
        {
            new object[] { "invalid" },
            new object[] { DateTime.Now },
            new object[] { new() }
        };

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.CompareTo(object)" /> throws <see cref="ArgumentException" />
        /// when the argument is neither a <see cref="DaysOfWeekSet" /> nor a <see cref="byte" />.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.InvalidTypesForCompareTo), typeof(DaysOfWeekSetTests))]
        public void CompareToObject_WhenInvalidType_ShouldThrowArgumentException(object value)
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            Assert.ThrowsExactly<ArgumentException>(() => set.CompareTo(value));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.CompareTo(DaysOfWeekSet)" /> returns a value whose sign
        /// correctly reflects the ordinal relationship between two <see cref="DaysOfWeekSet" /> instances.
        /// </summary>
        [DataTestMethod]
        [DataRow((byte)0, (byte)0, 0)]
        [DataRow((byte)1, (byte)0, 1)]
        [DataRow((byte)0, (byte)1, -1)]
        [DataRow((byte)7, (byte)7, 0)]
        public void CompareToDaysOfWeekSet_WhenComparing_ShouldReturnCorrectSign(byte first, byte second, int expectedSign)
        {
            var set1 = DaysOfWeekSet.FromByte(first);
            var set2 = DaysOfWeekSet.FromByte(second);

            int actual = set1.CompareTo(set2);

            Assert.AreEqual(Math.Sign(expectedSign), Math.Sign(actual));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.CompareTo(byte)" /> returns a value whose sign correctly
        /// reflects the ordinal relationship between the instance and a raw <see cref="byte" /> bitmask.
        /// </summary>
        [DataTestMethod]
        [DataRow((byte)0, (byte)0, 0)]
        [DataRow((byte)1, (byte)0, 1)]
        [DataRow((byte)0, (byte)1, -1)]
        [DataRow((byte)10, (byte)10, 0)]
        [DataRow((byte)127, (byte)5, 1)]
        [DataRow((byte)5, (byte)127, -1)]
        public void CompareToByte_WhenComparing_ShouldReturnCorrectSign(byte first, byte second, int expectedSign)
        {
            var set = DaysOfWeekSet.FromByte(first);

            int actual = set.CompareTo(second);

            Assert.AreEqual(Math.Sign(expectedSign), Math.Sign(actual));
        }
    }
}