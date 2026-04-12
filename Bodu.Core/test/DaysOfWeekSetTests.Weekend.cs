namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void Weekend_WhenAccessed_ShouldContainWeekendDays()
        {
            var set = DaysOfWeekSet.Weekend;
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set[DayOfWeek.Saturday]);
            Assert.IsTrue(set[DayOfWeek.Sunday]);
            Assert.IsFalse(set[DayOfWeek.Monday]);
        }
    }
}