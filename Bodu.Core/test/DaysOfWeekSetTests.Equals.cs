namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void EqualsObject_WhenNull_ShouldReturnFalse()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            Assert.IsFalse(set.Equals(null));
        }

        [TestMethod]
        public void EqualsObject_WhenDifferentType_ShouldReturnFalse()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            Assert.IsFalse(set.Equals("not a DaysOfWeekSet"));
        }

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