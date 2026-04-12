namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void Weekdays_WhenAccessed_ShouldContainWeekdays()
        {
            var set = DaysOfWeekSet.Weekdays;
            Assert.AreEqual(5, set.Count);
            Assert.IsTrue(set[DayOfWeek.Monday]);
            Assert.IsTrue(set[DayOfWeek.Tuesday]);
            Assert.IsTrue(set[DayOfWeek.Wednesday]);
            Assert.IsTrue(set[DayOfWeek.Thursday]);
            Assert.IsTrue(set[DayOfWeek.Friday]);
            Assert.IsFalse(set[DayOfWeek.Saturday]);
            Assert.IsFalse(set[DayOfWeek.Sunday]);
        }
    }
}