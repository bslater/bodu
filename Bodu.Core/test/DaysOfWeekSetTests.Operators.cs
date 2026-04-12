namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void OrOperator_WhenCombinedWithEmpty_ShouldReturnOriginal()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Tuesday);
            var actual = original | DaysOfWeekSet.Empty;
            Assert.AreEqual(original, actual);
        }

        [TestMethod]
        public void OrOperator_WhenCombinedWithItself_ShouldReturnSame()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Monday);
            var actual = original | original;
            Assert.AreEqual(original, actual);
        }

        [TestMethod]
        public void AndOperator_WhenCombinedWithEmpty_ShouldReturnEmpty()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Monday);
            var actual = original & DaysOfWeekSet.Empty;
            Assert.AreEqual(DaysOfWeekSet.Empty, actual);
        }

        [TestMethod]
        public void AndOperator_WhenCombinedWithItself_ShouldReturnSame()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Wednesday);
            var actual = original & original;
            Assert.AreEqual(original, actual);
        }

        [TestMethod]
        public void XorOperator_WhenCombinedWithEmpty_ShouldReturnOriginal()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Friday);
            var actual = original ^ DaysOfWeekSet.Empty;
            Assert.AreEqual(original, actual);
        }

        [TestMethod]
        public void XorOperator_WhenCombinedWithItself_ShouldReturnEmpty()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Thursday);
            var actual = original ^ original;
            Assert.AreEqual(DaysOfWeekSet.Empty, actual);
        }

        [TestMethod]
        public void ComplementOperator_WhenAppliedToEmpty_ShouldReturnFullSet()
        {
            var actual = ~DaysOfWeekSet.Empty;
            Assert.AreEqual(DaysOfWeekSet.FromByte(0b1111111), actual);
        }

        [TestMethod]
        public void ComplementOperator_WhenAppliedToFullSet_ShouldReturnEmpty()
        {
            var actual = ~DaysOfWeekSet.FromByte(0b1111111);
            Assert.AreEqual(DaysOfWeekSet.Empty, actual);
        }

        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        [DataRow((byte)5)]
        [DataRow((byte)127)]
        public void ImplicitConversion_WhenRoundTripped_ShouldMatchOriginal(byte original)
        {
            DaysOfWeekSet set = DaysOfWeekSet.FromByte(original);
            byte roundTripped = (byte)set;
            Assert.AreEqual(original, roundTripped);
        }
    }
}