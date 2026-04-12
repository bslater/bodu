namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that the ItemEvicting event is not triggered when the capacity is not exceeded.
        /// </summary>
        [TestMethod]
        public void ItemEvicting_WhenCapacityNotExceeded_ShouldNotTriggerEvent()
        {
            bool anyEventFired = false;
            var dictionary = new EvictingDictionary<string, int>(3);

            dictionary.ItemEvicting += (_, _) => anyEventFired = true;
            dictionary.ItemEvicted += (_, _) => anyEventFired = true;

            dictionary.Add("one", 1);
            dictionary.Add("two", 2);

            Assert.IsFalse(anyEventFired);
        }

        /// <summary>
        /// Verifies that the ItemEvicting event is not triggered by Remove or Clear operations.
        /// </summary>
        [TestMethod]
        public void ItemEvicting_WhenUsingRemoveOrClear_ShouldNotTriggerEvent()
        {
            bool eventFired = false;
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("A", 1);

            dictionary.ItemEvicted += (_, _) => eventFired = true;
            dictionary.ItemEvicting += (_, _) => eventFired = true;

            dictionary.Remove("A");
            Assert.IsFalse(eventFired);

            dictionary.Add("B", 2);
            dictionary.Add("C", 3);
            dictionary.Clear();
            Assert.IsFalse(eventFired);
        }
    }
}