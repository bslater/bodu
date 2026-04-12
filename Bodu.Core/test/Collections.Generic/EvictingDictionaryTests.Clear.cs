namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that Clear removes all key-value pairs and resets Count.
        /// </summary>
        [TestMethod]
        public void Clear_WhenCalled_ShouldRemoveAllEntries()
        {
            var dictionary = new EvictingDictionary<string, int>(capacity: 3);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Count);
            Assert.IsFalse(dictionary.ContainsKey("A"));
            Assert.IsFalse(dictionary.ContainsKey("B"));
        }

        /// <summary>
        /// Verifies that Keys returns an empty sequence after Clear is called.
        /// </summary>
        [TestMethod]
        public void Clear_WhenCalled_ShouldResetOrderedKeys()
        {
            var dictionary = new EvictingDictionary<string, int>(capacity: 3);
            dictionary.Add("X", 10);
            dictionary.Add("Y", 20);

            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Keys.Count());
        }
    }
}