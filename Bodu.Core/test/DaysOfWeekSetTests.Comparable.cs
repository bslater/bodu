namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void CompareToObject_WhenNull_ShouldReturnGreaterThanZero()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            int actual = set.CompareTo(null);
            Assert.IsTrue(actual > 0);
        }

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

        public static IEnumerable<object[]> InvalidTypesForCompareTo => new[]
        {
            new object[] { "invalid" },
            new object[] { DateTime.Now },
            new object[] { new() }
        };

        [TestMethod]
        [DynamicData(nameof(DaysOfWeekSetTests.InvalidTypesForCompareTo), typeof(DaysOfWeekSetTests))]
        public void CompareToObject_WhenInvalidType_ShouldThrowExactly(object value)
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            Assert.ThrowsExactly<ArgumentException>(() => set.CompareTo(value));
        }

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