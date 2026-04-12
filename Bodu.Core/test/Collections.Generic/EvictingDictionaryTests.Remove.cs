namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that Remove returns true and deletes the key when it exists.
        /// </summary>
        [TestMethod]
        public void Remove_WhenKeyExists_ShouldRemoveSuccessfully()
        {
            var dictionary = new EvictingDictionary<string, int>(5);
            dictionary.Add("x", 42);

            var actual = dictionary.Remove("x");

            Assert.IsTrue(actual);
            Assert.IsFalse(dictionary.ContainsKey("x"));
            Assert.AreEqual(0, dictionary.Count);
        }

        /// <summary>
        /// Verifies that a removed key is excluded from future eviction operations.
        /// </summary>
        [TestMethod]
        public void Remove_WhenKeyIsRemoved_ShouldExcludeFromFutureEvictions()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            var evictedKeys = new List<string>();

            dictionary.ItemEvicting += (key, _) => evictedKeys.Add(key);

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Remove("A");

            dictionary.Add("C", 3);
            dictionary.Add("D", 4);

            Assert.IsFalse(evictedKeys.Contains("A"));
            CollectionAssert.Contains(evictedKeys, "B");
        }

        /// <summary>
        /// Verifies that Remove returns false when the key is not present.
        /// </summary>
        [TestMethod]
        public void Remove_WhenKeyDoesNotExist_ShouldReturnFalse()
        {
            var dictionary = new EvictingDictionary<string, int>(5);
            var actual = dictionary.Remove("missing");

            Assert.IsFalse(actual);
        }

        /// <summary>
        /// Verifies that Remove(KeyValuePair) removes the entry only if both key and value match.
        /// </summary>
        [TestMethod]
        public void Remove_WhenKeyValuePairMatches_ShouldRemoveSuccessfully()
        {
            var dictionary = new EvictingDictionary<string, int>(5);
            dictionary.Add("a", 1);

            var actual = dictionary.Remove(new KeyValuePair<string, int>("a", 1));

            Assert.IsTrue(actual);
            Assert.IsFalse(dictionary.ContainsKey("a"));
        }

        /// <summary>
        /// Verifies that Remove(KeyValuePair) returns false if key exists but value does not match.
        /// </summary>
        [TestMethod]
        public void Remove_WhenKeyExistsButValueDoesNotMatch_ShouldNotRemove()
        {
            var dictionary = new EvictingDictionary<string, int>(5);
            dictionary.Add("a", 1);

            var actual = dictionary.Remove(new KeyValuePair<string, int>("a", 2));

            Assert.IsFalse(actual);
            Assert.IsTrue(dictionary.ContainsKey("a"));
        }

        /// <summary>
        /// Verifies that removing an item updates the access order in LeastRecentlyUsed mode.
        /// </summary>
        [TestMethod]
        public void Remove_WhenLRUPolicy_ShouldUpdateAccessOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);

            var actual = dictionary.Remove("a");

            Assert.IsTrue(actual);
            Assert.IsFalse(dictionary.ContainsKey("a"));
            Assert.AreEqual(1, dictionary.Count);
        }

        /// <summary>
        /// Verifies that removing an item updates frequency tracking in LeastFrequentlyUsed mode.
        /// </summary>
        [TestMethod]
        public void Remove_WhenLFUPolicy_ShouldUpdateFrequencyTracking()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("x", 100);
            dictionary.Touch("x");

            var actual = dictionary.Remove("x");

            Assert.IsTrue(actual);
            Assert.IsFalse(dictionary.ContainsKey("x"));
            Assert.AreEqual(0, dictionary.Count);
        }

        /// <summary>
        /// Verifies that Remove only deletes the specified key when multiple keys share the same value.
        /// </summary>
        [TestMethod]
        public void Remove_WhenMultipleKeysWithSameValue_ShouldOnlyRemoveMatchingKey()
        {
            var dictionary = new EvictingDictionary<string, int>(5);
            dictionary.Add("a", 10);
            dictionary.Add("b", 10);

            var actual = dictionary.Remove(new KeyValuePair<string, int>("a", 10));

            Assert.IsTrue(actual);
            Assert.IsFalse(dictionary.ContainsKey("a"));
            Assert.IsTrue(dictionary.ContainsKey("b"));
        }
    }
}