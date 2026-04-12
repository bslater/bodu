namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void Constructor_WhenNoDaysProvided_ShouldBeEmpty()
        {
            var set = new DaysOfWeekSet();
            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void Constructor_WhenNullArrayProvided_ShouldBeEmpty()
        {
            var set = new DaysOfWeekSet((DayOfWeek[])null);
            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void Constructor_WhenNullStringProvided_ShouldThrowExactly()
        {
            string input = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new DaysOfWeekSet(input);
            });
        }

        [TestMethod]
        [DynamicData(nameof(GetValidParseInputTestData), typeof(DaysOfWeekSetTests))]
        public void Constructor_WhenValidStringProvided_ShouldReturnExpected(string input, string _, byte expected)
        {
            var actual = new DaysOfWeekSet(input);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(DayOfWeek.Sunday)]
        [DataRow(DayOfWeek.Monday)]
        [DataRow(DayOfWeek.Tuesday)]
        [DataRow(DayOfWeek.Wednesday)]
        [DataRow(DayOfWeek.Thursday)]
        [DataRow(DayOfWeek.Friday)]
        [DataRow(DayOfWeek.Saturday)]
        public void Constructor_WhenSingleDayProvided_ShouldContainDay(DayOfWeek day)
        {
            var set = new DaysOfWeekSet(day);
            Assert.IsTrue(set[day]);
            Assert.AreEqual(1, set.Count);
        }
    }
}