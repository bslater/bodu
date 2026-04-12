namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void Count_WhenEmpty_ShouldBeZero()
        {
            var set = DaysOfWeekSet.Empty;
            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void Count_WhenAllDaysSelected_ShouldBeSeven()
        {
            var set = DaysOfWeekSet.FromByte(127);
            Assert.AreEqual(7, set.Count);
        }

        [TestMethod]
        public void Count_WhenSomeDaysSelected_ShouldReflectSelection()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            Assert.AreEqual(2, set.Count);
        }
    }
}