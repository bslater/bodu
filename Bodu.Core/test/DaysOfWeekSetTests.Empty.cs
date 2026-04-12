namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void Empty_WhenAccessed_ShouldBeEmpty()
        {
            var set = DaysOfWeekSet.Empty;
            Assert.AreEqual(0, set.Count);
        }
    }
}