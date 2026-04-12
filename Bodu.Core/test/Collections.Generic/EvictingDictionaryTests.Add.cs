namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        // Add

        /// <summary>
        /// Verifies that a new key-value pair is added when the key does not already exist.
        /// </summary>
        [TestMethod]
        public void Add_WhenKeyIsNew_ShouldAddItem()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("one", 1);

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual(1, dictionary["one"]);
        }

        /// <summary>
        /// Verifies that the value for an existing key is replaced when added again.
        /// </summary>
        [TestMethod]
        public void Add_WhenKeyAlreadyExists_ShouldReplaceValue()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("one", 1);
            dictionary.Add("one", 99);

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual(99, dictionary["one"]);
        }

        /// <summary>
        /// Verifies that an ArgumentNullException is thrown when adding a null key.
        /// </summary>
        [TestMethod]
        public void Add_WhenKeyIsNull_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, string>(2);

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                dictionary.Add(null, "value");
            });
        }

        /// <summary>
        /// Verifies that case-sensitive keys are treated as distinct using the default comparer.
        /// </summary>
        [TestMethod]
        public void Add_WhenKeysDifferOnlyByCase_ShouldTreatAsDistinctKeys()
        {
            var dictionary = new EvictingDictionary<string, int>(5);
            dictionary.Add("Key", 1);
            dictionary.Add("key", 2);

            Assert.AreEqual(2, dictionary.Count);
            Assert.AreEqual(1, dictionary["Key"]);
            Assert.AreEqual(2, dictionary["key"]);
        }

        /// <summary>
        /// Verifies that the previous item is always evicted when capacity is one.
        /// </summary>
        [TestMethod]
        public void Add_WhenCapacityIsOne_ShouldAlwaysEvictPreviousItem()
        {
            var dictionary = new EvictingDictionary<string, int>(1);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            Assert.IsFalse(dictionary.ContainsKey("A"));
            Assert.IsTrue(dictionary.ContainsKey("B"));

            dictionary.Add("C", 3);

            Assert.IsFalse(dictionary.ContainsKey("B"));
            Assert.IsTrue(dictionary.ContainsKey("C"));
        }

        /// <summary>
        /// Verifies that re-inserting an item during eviction does not cause an infinite loop.
        /// </summary>
        [TestMethod]
        public void Add_WhenEvictionHandlerReinsertsEvictedItem_ShouldNotCauseInfiniteLoop()
        {
            var dictionary = new EvictingDictionary<string, int>(1);

            dictionary.ItemEvicting += (key, value) =>
            {
                if (!dictionary.ContainsKey(key))
                    dictionary[key] = value + 1;
            };

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            Assert.AreEqual(1, dictionary.Count);
        }

        // Add (KeyValuePair)

        /// <summary>
        /// Verifies that a KeyValuePair is added when the key does not exist.
        /// </summary>
        [TestMethod]
        public void AddKeyValuePair_WhenKeyIsNew_ShouldAddItem()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add(new KeyValuePair<string, int>("a", 1));

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual(1, dictionary["a"]);
        }

        /// <summary>
        /// Verifies that an existing key is updated when added using KeyValuePair.
        /// </summary>
        [TestMethod]
        public void AddKeyValuePair_WhenKeyAlreadyExists_ShouldOverwriteValue()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add(new KeyValuePair<string, int>("x", 1));
            dictionary.Add(new KeyValuePair<string, int>("x", 5));

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual(5, dictionary["x"]);
        }

        /// <summary>
        /// Verifies that the oldest item is evicted when adding a KeyValuePair that exceeds capacity.
        /// </summary>
        [TestMethod]
        public void AddKeyValuePair_WhenCapacityExceeded_ShouldEvictOldestItem()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add(new KeyValuePair<string, int>("a", 1));
            dictionary.Add(new KeyValuePair<string, int>("b", 2));
            dictionary.Add(new KeyValuePair<string, int>("c", 3));

            Assert.AreEqual(2, dictionary.Count);
            Assert.IsFalse(dictionary.ContainsKey("a"));
            Assert.IsTrue(dictionary.ContainsKey("b"));
            Assert.IsTrue(dictionary.ContainsKey("c"));
        }

        /// <summary>
        /// Verifies that an ArgumentNullException is thrown when adding a null KeyValuePair key.
        /// </summary>
        [TestMethod]
        public void AddKeyValuePair_WhenKeyIsNull_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, int>(3);

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                dictionary.Add(new KeyValuePair<string, int>(null!, 1));
            });
        }
    }
}