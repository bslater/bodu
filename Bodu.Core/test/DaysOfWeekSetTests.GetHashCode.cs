namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void GetHashCode_WhenSameDays_ShouldReturnSameHash()
        {
            var set1 = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            var set2 = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);

            Assert.AreEqual(set1.GetHashCode(), set2.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_WhenDifferentDays_ShouldReturnDifferentHash()
        {
            var set1 = new DaysOfWeekSet(DayOfWeek.Monday);
            var set2 = new DaysOfWeekSet(DayOfWeek.Tuesday);

            Assert.AreNotEqual(set1.GetHashCode(), set2.GetHashCode());
        }

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