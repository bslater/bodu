namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that Keys returns an empty collection when the dictionary is empty.
        /// </summary>
        [TestMethod]
        public void Keys_Get_WhenEmpty_ShouldReturnEmptyCollection()
        {
            var dictionary = new EvictingDictionary<string, int>(3);

            Assert.AreEqual(0, dictionary.Keys.Count);
        }

        /// <summary>
        /// Verifies that Keys contains all inserted keys when the dictionary has not exceeded capacity.
        /// </summary>
        [TestMethod]
        public void Keys_Get_WhenItemsAdded_ShouldContainAllInsertedKeys()
        {
            var dictionary = new EvictingDictionary<string, int>(5);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Add("C", 3);

            var keys = dictionary.Keys;

            Assert.AreEqual(3, keys.Count);
            CollectionAssert.Contains(keys.ToList(), "A");
            CollectionAssert.Contains(keys.ToList(), "B");
            CollectionAssert.Contains(keys.ToList(), "C");
        }

        /// <summary>
        /// Verifies that Keys does not contain a key that has been explicitly removed.
        /// </summary>
        [TestMethod]
        public void Keys_Get_WhenItemIsRemoved_ShouldNotContainRemovedKey()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            dictionary.Remove("A");

            CollectionAssert.DoesNotContain(dictionary.Keys.ToList(), "A");
            CollectionAssert.Contains(dictionary.Keys.ToList(), "B");
        }

        /// <summary>
        /// Verifies that Keys does not contain a key that was evicted when capacity was exceeded.
        /// </summary>
        [TestMethod]
        public void Keys_Get_WhenItemIsEvicted_ShouldNotContainEvictedKey()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Add("C", 3); // evicts A

            CollectionAssert.DoesNotContain(dictionary.Keys.ToList(), "A");
            CollectionAssert.Contains(dictionary.Keys.ToList(), "B");
            CollectionAssert.Contains(dictionary.Keys.ToList(), "C");
        }

        /// <summary>
        /// Verifies that Keys returns an empty collection after the dictionary is cleared.
        /// </summary>
        [TestMethod]
        public void Keys_Get_WhenDictionaryIsCleared_ShouldBeEmpty()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Keys.Count);
        }

        /// <summary>
        /// Verifies that Keys reflects the count of unique keys when a key is re-inserted.
        /// </summary>
        [TestMethod]
        public void Keys_Get_WhenKeyIsReinserted_ShouldNotContainDuplicates()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("A", 1);
            dictionary.Add("A", 2); // replace

            Assert.AreEqual(1, dictionary.Keys.Count);
        }
    }
}