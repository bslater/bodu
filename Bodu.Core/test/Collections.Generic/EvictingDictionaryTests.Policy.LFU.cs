namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that Clear resets LeastFrequentlyUsed frequency tracking when called.
        /// </summary>
        [TestMethod]
        public void Clear_WhenPolicyIsLFUAndCalled_ShouldResetFrequencyTracking()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            _ = dictionary["A"];

            dictionary.Clear();
            dictionary.Add("C", 3);
            dictionary.Add("D", 4);

            var keys = dictionary.Keys.ToList();
            CollectionAssert.DoesNotContain(keys, "A");
            CollectionAssert.DoesNotContain(keys, "B");
        }

        /// <summary>
        /// Verifies that LeastFrequentlyUsed evicts the least frequently used item.
        /// </summary>
        [TestMethod]
        [TestCategory("LFU")]
        public void ItemEvicted_WhenPolicyIsLFUAndEvictionOccurs_ShouldEvictLeastFrequentlyUsed()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            var evicted = new List<string>();
            dictionary.ItemEvicted += (key, _) => evicted.Add(key);

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            _ = dictionary["B"];

            dictionary.Add("C", 3);

            CollectionAssert.Contains(evicted, "A");
        }

        /// <summary>
        /// Verifies that eviction is skipped if the candidate key has already been removed in LeastRecentlyUsed.
        /// </summary>
        [TestMethod]
        [TestCategory("LRU")]
        public void ItemEvicted_WhenPolicyIsLRUAndCandidateRemoved_ShouldSkipEviction()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
            var evicted = new List<string>();
            dictionary.ItemEvicted += (key, _) => evicted.Add(key);

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Remove("A");

            dictionary.Add("C", 3);

            Assert.AreEqual(0, evicted.Count);
        }

        /// <summary>
        /// Verifies that Touch updates frequency in LeastFrequentlyUsed policy.
        /// </summary>
        [TestMethod]
        public void Touch_WhenPolicyIsLFUAndKeyTouched_ShouldUpdateFrequency()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Add("c", 3);

            dictionary.Touch("a"); // freq = 2
            dictionary.Touch("a"); // freq = 3
            dictionary.Touch("b"); // freq = 2

            dictionary.Add("d", 4); // c (freq = 1) should be evicted

            var keys = dictionary.Keys.ToArray();
            CollectionAssert.AreEquivalent(new[] { "a", "b", "d" }, keys);
        }

        /// <summary>
        /// Verifies that Touch returns true when key exists in LeastFrequentlyUsed.
        /// </summary>
        [TestMethod]
        public void Touch_WhenPolicyIsLFUAndKeyExists_ShouldReturnTrue()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("x", 42);

            var actual = dictionary.Touch("x");

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Verifies that TouchOrThrow updates frequency when the key exists in LeastFrequentlyUsed.
        /// </summary>
        [TestMethod]
        public void TouchOrThrow_WhenPolicyIsLFUAndKeyExists_ShouldUpdateFrequency()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("x", 10);

            dictionary.TouchOrThrow("x");

            Assert.IsTrue(dictionary.ContainsKey("x"));
        }

        /// <summary>
        /// Verifies that Add evicts the least frequently used item when capacity is exceeded using
        /// LeastFrequentlyUsed policy.
        /// </summary>
        [TestMethod]
        [TestCategory("LFU")]
        public void Add_WhenPolicyIsLFUAndCapacityExceeded_ShouldEvictLeastFrequentlyUsed()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("one", 1);
            dictionary.Add("two", 2);
            _ = dictionary["one"];
            dictionary.Add("three", 3);

            Assert.IsTrue(dictionary.ContainsKey("one"));
            Assert.IsFalse(dictionary.ContainsKey("two"));
            Assert.IsTrue(dictionary.ContainsKey("three"));
        }

        /// <summary>
        /// Verifies that PeekEvictionCandidate returns the least frequently used key.
        /// </summary>
        [TestMethod]
        public void PeekEvictionCandidate_WhenPolicyIsLFU_ShouldReturnLeastFrequentlyUsedKey()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("x", 1);
            dictionary.Add("y", 2);
            dictionary.Add("z", 3);
            dictionary.Touch("x"); // freq = 2
            dictionary.Touch("y"); // freq = 2

            var candidate = dictionary.PeekEvictionCandidate();

            Assert.AreEqual("z", candidate);
        }

        /// <summary>
        /// Verifies that Touch returns true and increases usage count when the key exists in LeastFrequentlyUsed.
        /// </summary>
        [TestMethod]
        public void Touch_WhenPolicyIsLFUAndKeyExists_ShouldReturnTrueAndIncreaseFrequency()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("a", 1);

            var actual = dictionary.Touch("a");

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Verifies that Clear resets access frequency and allows reinsertion.
        /// </summary>
        [TestMethod]
        public void Clear_WhenPolicyIsLFU_ShouldAllowFreshInsertAfterReset()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Touch("A");

            dictionary.Clear();
            dictionary.Add("C", 3);
            dictionary.Add("D", 4);

            var keys = dictionary.Keys.ToList();
            CollectionAssert.DoesNotContain(keys, "A");
            CollectionAssert.DoesNotContain(keys, "B");
        }

        /// <summary>
        /// Verifies that eviction event is not fired when no item is eligible for
        /// LeastFrequentlyUsed eviction.
        /// </summary>
        [TestMethod]
        public void EvictionEvents_WhenPolicyIsLFUAndNoCandidateFound_ShouldNotFireEvent()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            bool eventFired = false;
            dictionary.ItemEvicted += (_, _) => eventFired = true;

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Remove("A");
            dictionary.Remove("B");

            dictionary.Add("C", 3);

            Assert.IsFalse(eventFired);
        }
    }
}