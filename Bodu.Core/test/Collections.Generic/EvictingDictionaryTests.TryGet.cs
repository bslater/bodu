namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that TryGetValue returns true and the correct value when the key exists.
        /// </summary>
        [TestMethod]
        public void TryGetValue_WhenKeyExists_ShouldReturnTrueAndCorrectValue()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("A", 42);

            var result = dictionary.TryGetValue("A", out var value);

            Assert.IsTrue(result);
            Assert.AreEqual(42, value);
        }

        /// <summary>
        /// Verifies that TryGetValue returns false and sets the out parameter to the default value
        /// when the key does not exist.
        /// </summary>
        [TestMethod]
        public void TryGetValue_WhenKeyDoesNotExist_ShouldReturnFalseAndSetValueToDefault()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("A", 1);

            var result = dictionary.TryGetValue("B", out var value);

            Assert.IsFalse(result);
            Assert.AreEqual(default, value);
        }

        /// <summary>
        /// Verifies that TryGetValue updates recency for LeastRecentlyUsed, preventing eviction of the accessed key.
        /// </summary>
        [TestMethod]
        public void TryGetValue_WhenPolicyIsLRUAndKeyAccessed_ShouldUpdateRecencyAndPreventEviction()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            _ = dictionary.TryGetValue("A", out _); // accesses A, making B the LRU

            dictionary.Add("C", 3); // should evict B

            Assert.IsTrue(dictionary.ContainsKey("A"));
            Assert.IsFalse(dictionary.ContainsKey("B"));
            Assert.IsTrue(dictionary.ContainsKey("C"));
        }

        /// <summary>
        /// Verifies that TryGetValue updates access frequency for LeastFrequentlyUsed, preventing eviction of the accessed key.
        /// </summary>
        [TestMethod]
        public void TryGetValue_WhenPolicyIsLFUAndKeyAccessed_ShouldUpdateFrequencyAndPreventEviction()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            _ = dictionary.TryGetValue("A", out _); // A now has frequency 2, B remains at 1

            dictionary.Add("C", 3); // should evict B (lower frequency)

            Assert.IsTrue(dictionary.ContainsKey("A"));
            Assert.IsFalse(dictionary.ContainsKey("B"));
            Assert.IsTrue(dictionary.ContainsKey("C"));
        }

        /// <summary>
        /// Verifies that TryGetValue does not affect insertion order when using FirstInFirstOut policy.
        /// </summary>
        [TestMethod]
        public void TryGetValue_WhenPolicyIsFIFOAndKeyAccessed_ShouldNotAffectEvictionOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            _ = dictionary.TryGetValue("A", out _); // access A; FIFO ignores this

            var evicted = new List<string>();
            dictionary.ItemEvicted += (key, _) => evicted.Add(key);

            dictionary.Add("C", 3); // should still evict A (oldest insertion)

            CollectionAssert.Contains(evicted, "A");
            Assert.IsFalse(dictionary.ContainsKey("A"));
        }

        /// <summary>
        /// Verifies that TryGetValue increments TotalTouches when the key is found.
        /// </summary>
        [TestMethod]
        public void TryGetValue_WhenKeyExists_ShouldIncrementTotalTouches()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("x", 99);
            var before = dictionary.TotalTouches;

            dictionary.TryGetValue("x", out _);

            Assert.AreEqual(before + 1, dictionary.TotalTouches);
        }

        /// <summary>
        /// Verifies that TryGetValue does not increment TotalTouches when the key is not found.
        /// </summary>
        [TestMethod]
        public void TryGetValue_WhenKeyDoesNotExist_ShouldNotIncrementTotalTouches()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            var before = dictionary.TotalTouches;

            dictionary.TryGetValue("missing", out _);

            Assert.AreEqual(before, dictionary.TotalTouches);
        }

        /// <summary>
        /// Verifies that TryGetValue returns the default value for a reference type when the key is absent.
        /// </summary>
        [TestMethod]
        public void TryGetValue_WhenKeyDoesNotExist_ForReferenceTypeValue_ShouldSetValueToNull()
        {
            var dictionary = new EvictingDictionary<string, string>(3);
            dictionary.Add("present", "here");

            var result = dictionary.TryGetValue("absent", out var value);

            Assert.IsFalse(result);
            Assert.IsNull(value);
        }
    }
}