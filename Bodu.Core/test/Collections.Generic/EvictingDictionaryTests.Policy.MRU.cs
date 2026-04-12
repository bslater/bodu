namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that Add evicts the most recently used item when capacity is exceeded using
        /// MostRecentlyUsed policy.
        /// </summary>
        [TestMethod]
        [TestCategory("MRU")]
        public void Add_WhenPolicyIsMRUAndCapacityExceeded_ShouldEvictMostRecentlyUsed()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.MostRecentlyUsed);
            dictionary.Add("one", 1);
            dictionary.Add("two", 2);
            dictionary["two"] = 22;
            dictionary.Add("three", 3); // "two" should be evicted

            Assert.IsTrue(dictionary.ContainsKey("one"));
            Assert.IsFalse(dictionary.ContainsKey("two"));
            Assert.IsTrue(dictionary.ContainsKey("three"));
        }

        /// <summary>
        /// Verifies that Touch updates recency in a MostRecentlyUsed dictionary.
        /// </summary>
        [TestMethod]
        public void Touch_WhenPolicyIsMRUAndKeyTouched_ShouldUpdateRecency()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.MostRecentlyUsed);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Add("c", 3);

            dictionary.Touch("c"); // becomes MRU
            dictionary.Add("d", 4); // "c" should be evicted

            var expected = new Dictionary<string, int>
            {
                ["a"] = 1,
                ["b"] = 2,
                ["d"] = 4
            };

            CollectionAssert.AreEquivalent(expected, dictionary.ToArray());
        }

        /// <summary>
        /// Verifies that PeekEvictionCandidate returns the most recently used key using
        /// MostRecentlyUsed policy.
        /// </summary>
        [TestMethod]
        public void PeekEvictionCandidate_WhenPolicyIsMRU_ShouldReturnMostRecentlyUsedKey()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.MostRecentlyUsed);
            dictionary.Add("x", 1);
            dictionary.Add("y", 2);
            dictionary.Add("z", 3);
            dictionary.Touch("y");

            Assert.AreEqual("y", dictionary.PeekEvictionCandidate());
        }

        /// <summary>
        /// Verifies that ItemEvicted is triggered with the correct key and value when an item is
        /// evicted using MostRecentlyUsed policy.
        /// </summary>
        [TestMethod]
        [TestCategory("MRU")]
        public void ItemEvicted_WhenPolicyIsMRUAndItemEvicted_ShouldBeCalledWithCorrectKeyValue()
        {
            var evicted = new List<string>();
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.MostRecentlyUsed);
            dictionary.ItemEvicted += (key, value) => evicted.Add($"{key}:{value}");

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Touch("B");
            dictionary.Add("C", 3); // "B" should be evicted

            CollectionAssert.AreEqual(new[] { "B:2" }, evicted);
        }

        /// <summary>
        /// Verifies that Clear resets access order in a MostRecentlyUsed dictionary.
        /// </summary>
        [TestMethod]
        public void Clear_WhenPolicyIsMRUAndCalled_ShouldResetAccessOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.MostRecentlyUsed);
            dictionary.Add("X", 1);
            dictionary.Add("Y", 2);
            dictionary.Touch("X");

            dictionary.Clear();
            dictionary.Add("A", 3);
            dictionary.Add("B", 4);

            var keys = dictionary.Keys.ToList();
            CollectionAssert.DoesNotContain(keys, "X");
            CollectionAssert.DoesNotContain(keys, "Y");
        }

        /// <summary>
        /// Verifies that iteration respects insertion and access order under MostRecentlyUsed policy.
        /// </summary>
        [TestMethod]
        [TestCategory("MRU")]
        public void IEnumerable_GetEnumerator_WhenPolicyIsMRU_ShouldRespectRecencyOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.MostRecentlyUsed);
            dictionary.Add("a", 1); // oldest
            dictionary.Add("b", 2);
            dictionary.Add("c", 3); // newest
            dictionary.Touch("a");  // now "a" is most recent

            var actual = dictionary.ToArray();
            var expected = new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("c", 3),
                new KeyValuePair<string, int>("b", 2),
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that Keys are returned in recency order under MostRecentlyUsed policy.
        /// </summary>
        [TestMethod]
        [TestCategory("MRU")]
        public void Keys_WhenPolicyIsMRU_ShouldReturnKeysInRecencyOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.MostRecentlyUsed);
            dictionary.Add("1", 1);
            dictionary.Add("2", 2);
            dictionary.Add("3", 3);
            dictionary.Touch("2");

            var expected = new[] { "2", "3", "1" };
            CollectionAssert.AreEqual(expected, dictionary.Keys.ToList());
        }
    }
}