namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Evicts the oldest item when capacity is exceeded using FirstInFirstOut policy.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void Add_WhenPolicyIsFIFOAndCapacityExceeded_ShouldEvictOldest()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("one", 1);
            dictionary.Add("two", 2);
            dictionary.Add("three", 3);

            Assert.IsFalse(dictionary.ContainsKey("one"));
            Assert.IsTrue(dictionary.ContainsKey("two"));
            Assert.IsTrue(dictionary.ContainsKey("three"));
        }

        /// <summary>
        /// Triggers ItemEvicted with the correct key and value when an item is evicted using FirstInFirstOut.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void ItemEvicted_WhenPolicyIsFIFOAndItemEvicted_ShouldBeCalledWithCorrectKeyValue()
        {
            var evictedItems = new List<string>();
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.ItemEvicted += (key, value) => evictedItems.Add($"{key}:{value}");

            dictionary.Add("X", 10);
            dictionary.Add("Y", 20);
            dictionary.Add("Z", 30);

            CollectionAssert.AreEqual(new[] { "X:10" }, evictedItems);
        }

        /// <summary>
        /// Triggers multiple ItemEvicted events when multiple evictions occur.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void ItemEvicted_WhenPolicyIsFIFOAndMultipleEvictions_ShouldTriggerMultipleEvents()
        {
            var evicted = new List<string>();
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.ItemEvicted += (key, value) => evicted.Add($"{key}:{value}");

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Add("C", 3);
            dictionary.Add("D", 4);

            CollectionAssert.AreEqual(new[] { "A:1", "B:2" }, evicted);
        }

        /// <summary>
        /// Ensures that ItemEvicting is fired before ItemEvicted for FirstInFirstOut eviction.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void ItemEvicted_WhenPolicyIsFIFOAndEvictionOccurs_ShouldFireAfterItemEvicting()
        {
            var sequence = new List<string>();
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);

            dictionary.ItemEvicting += (key, value) => sequence.Add($"Evicting:{key}:{value}");
            dictionary.ItemEvicted += (key, value) => sequence.Add($"Evicted:{key}:{value}");

            dictionary.Add("one", 1);
            dictionary.Add("two", 2);
            dictionary.Add("three", 3);

            CollectionAssert.AreEqual(new[] { "Evicting:one:1", "Evicted:one:1" }, sequence);
        }

        /// <summary>
        /// Triggers ItemEvicting with the correct key and value before eviction occurs.
        /// </summary>
        [TestMethod]
        public void ItemEvicting_WhenPolicyIsFIFOAndEvictionOccurs_ShouldBeCalledWithCorrectKeyValue()
        {
            var evictedItems = new List<string>();
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.ItemEvicting += (key, value) => evictedItems.Add($"{key}:{value}");

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Add("C", 3);

            CollectionAssert.AreEqual(new[] { "A:1" }, evictedItems);
        }

        /// <summary>
        /// Ensures ItemEvicting is fired before ItemEvicted.
        /// </summary>
        [TestMethod]
        public void ItemEvicting_WhenPolicyIsFIFOAndEvictionOccurs_ShouldFireBeforeItemEvicted()
        {
            var sequence = new List<string>();
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);

            dictionary.ItemEvicting += (key, value) => sequence.Add($"Evicting:{key}:{value}");
            dictionary.ItemEvicted += (key, value) => sequence.Add($"Evicted:{key}:{value}");

            dictionary.Add("one", 1);
            dictionary.Add("two", 2);
            dictionary.Add("three", 3);

            CollectionAssert.AreEqual(new[] { "Evicting:one:1", "Evicted:one:1" }, sequence);
        }

        /// <summary>
        /// Enumerates entries in insertion or usage order depending on eviction policy.
        /// </summary>
        [TestMethod]
        public void Enumerator_WhenPolicyIsFIFO_ShouldReturnItemsInInsertionOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(5, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Add("c", 3);

            var expected = new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("c", 3)
            };

            CollectionAssert.AreEqual(expected, new List<KeyValuePair<string, int>>(dictionary));
        }

        /// <summary>
        /// Returns keys in insertion order when using FirstInFirstOut eviction policy.
        /// </summary>
        [TestMethod]
        public void Keys_WhenPolicyIsFIFO_ShouldReturnKeysInInsertionOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut)
            {
                ["one"] = 1,
                ["two"] = 2,
                ["three"] = 3
            };

            var expected = new[] { "one", "two", "three" };
            CollectionAssert.AreEqual(expected, dictionary.Keys.ToList());
        }

        /// <summary>
        /// Evicts the oldest item when FirstInFirstOut policy is used.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void ItemEvicted_WhenPolicyIsFIFO_ShouldEvictOldestItem()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
            var evicted = new List<string>();
            dictionary.ItemEvicted += (key, _) => evicted.Add(key);

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Add("C", 3);

            CollectionAssert.Contains(evicted, "A");
        }

        /// <summary>
        /// Verifies that the collection of items returned are in FirstInFirstOut insertion order
        /// when the eviction policy is FirstInFirstOut.
        /// </summary>
        [TestMethod]
        public void Values_WhenPolicyIsFIFO_ShouldReturnValuesInInsertionOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut)
            {
                ["a"] = 10,
                ["b"] = 20,
                ["c"] = 30
            };

            var expected = new[] { 10, 20, 30 };
            CollectionAssert.AreEqual(expected, dictionary.Values.ToList());
        }

        /// <summary>
        /// Verifies that the FirstInFirstOut insertion order is reset when Clear is called.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void Clear_WhenPolicyIsFIFOAndCalled_ShouldResetInsertionOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            dictionary.Clear();
            dictionary.Add("C", 3);
            dictionary.Add("D", 4);

            var keys = dictionary.Keys.ToList();
            CollectionAssert.DoesNotContain(keys, "A");
            CollectionAssert.DoesNotContain(keys, "B");
        }

        /// <summary>
        /// Verifies that the oldest inserted key is returned when using FirstInFirstOut policy.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void PeekEvictionCandidate_WhenPolicyIsFIFO_ShouldReturnOldestKey()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("first", 1);
            dictionary.Add("second", 2);
            dictionary.Add("third", 3);

            Assert.AreEqual("first", dictionary.PeekEvictionCandidate());
        }

        /// <summary>
        /// Verifies that no exception is thrown when the FirstInFirstOut eviction candidate has
        /// already been removed.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void EvictionEvents_WhenPolicyIsFIFOAndCandidateMissing_ShouldNotThrow()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Remove("A");

            dictionary.Add("C", 3); // Candidate "A" was removed

            Assert.IsTrue(dictionary.ContainsKey("B"));
            Assert.IsTrue(dictionary.ContainsKey("C"));
        }

        /// <summary>
        /// Verifies that Touch does not affect eviction order when using FirstInFirstOut policy.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void Touch_WhenPolicyIsFIFOAndKeyTouched_ShouldHaveNoEffect()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Add("c", 3);

            dictionary.Touch("a"); // no effect in FirstInFirstOut
            dictionary.Add("d", 4); // should still evict "a"
        }

        /// <summary>
        /// Verifies that Touch increments total touches when using FirstInFirstOut policy.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void Touch_WhenPolicyIsFIFO_ShouldIncrementTTotalTouches()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Add("c", 3);
            var count = dictionary.TotalTouches;

            dictionary.Touch("a");
            Assert.AreEqual(count + 1, dictionary.TotalTouches);
        }

        /// <summary>
        /// Verifies that TouchOrThrow does not alter order when using FirstInFirstOut policy.
        /// </summary>
        [TestMethod]
        [TestCategory("FIFO")]
        public void TouchOrThrow_WhenPolicyIsFIFOAndKeyExists_ShouldHaveNoEffect()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut);
            dictionary.Add("item", 99);

            dictionary.TouchOrThrow("item"); // no effect in FirstInFirstOut

            Assert.IsTrue(dictionary.ContainsKey("item"));
        }
    }
}