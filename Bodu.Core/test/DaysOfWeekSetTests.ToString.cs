namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void ToString_WhenCalledWithoutParameters_ShouldUseDefaultFormat()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday);
            string output = set.ToString();
            Assert.AreEqual("SM_____", output);
        }

        [TestMethod]
        [DataRow("S", "SM____S")] // Sunday-first
        [DataRow("s", "SM____S")]
        [DataRow("M", "M____SS")] // Monday-first
        [DataRow("m", "M____SS")]
        [DataRow("B", "1100001")] // Binary (Sunday + Monday + Saturday selected)
        [DataRow("b", "1100001")]
        public void ToString_WhenValidFormat_ShouldFormatCorrectly(string format, string expected)
        {
            var set = new DaysOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday);
            string output = set.ToString(format);
            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void ToString_WhenInvalidFormat_ShouldThrowExactly()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            Assert.ThrowsExactly<ArgumentException>(() => set.ToString("X"));
        }

        [TestMethod]
        [DataRow("S", "SM____S")] // Sunday-first
        [DataRow("M", "M____SS")] // Monday-first
        [DataRow("B", "1100001")] // Binary
        public void ToString_WhenFormatAndProviderProvided_ShouldFormatCorrectly(string format, string expected)
        {
            var set = new DaysOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday);
            string output = set.ToString(format, null);
            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void ToString_WhenOnlyProviderProvided_ShouldUseDefaultFormat()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday);
            string output = set.ToString(provider: null);
            Assert.AreEqual("SM_____", output);
        }

        [TestMethod]
        [DynamicData(nameof(GetAllBitmaskPermutationTestData), DynamicDataSourceType.Method)]
        public void ToString_WhenCalled_ShouldReturnUpperString(byte value, string expected, string _)
        {
            var set = DaysOfWeekSet.FromByte(value);
            Assert.AreEqual(expected, set.ToString());
        }

        [TestMethod]
        [DynamicData(nameof(GetAllBitmaskPermutationTestData), DynamicDataSourceType.Method)]
        public void ToString_WhenBinaryFormat_ShouldReturnUpperString(byte value, string _, string expected)
        {
            var set = DaysOfWeekSet.FromByte(value);
            Assert.AreEqual(expected, set.ToString("b"));
        }
    }
}