namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        [DataRow((byte)0, 0)]
        [DataRow((byte)1, 1)]
        [DataRow((byte)127, 7)]
        public void FromByte_WhenValidValue_ShouldCreateExpectedSet(byte input, int expectedCount)
        {
            var set = DaysOfWeekSet.FromByte(input);
            Assert.AreEqual(expectedCount, set.Count);
        }

        [TestMethod]
        public void FromByte_WhenValueGreaterThanMax_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DaysOfWeekSet.FromByte(128));
        }
    }
}