namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that ContainsKey returns true when the dictionary contains the specified key.
        /// </summary>
        [TestMethod]
        public void ContainsKey_WhenKeyExists_ShouldReturnTrue()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("A", 1);

            Assert.IsTrue(dictionary.ContainsKey("A"));
        }

        /// <summary>
        /// Verifies that ContainsKey correctly reflects the current state after a previously evicted key is removed and a new key is added.
        /// </summary>
        [TestMethod]
        public void ContainsKey_WhenEvictedKeyIsRemovedAndNewKeyAdded_ShouldReturnCorrectResults()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Remove("A");

            dictionary.Add("C", 3);

            Assert.IsTrue(dictionary.ContainsKey("B"));
            Assert.IsTrue(dictionary.ContainsKey("C"));
        }

        /// <summary>
        /// Verifies that ContainsKey returns false when the dictionary does not contain the specified key.
        /// </summary>
        [TestMethod]
        public void ContainsKey_WhenKeyDoesNotExist_ShouldReturnFalse()
        {
            var dictionary = new EvictingDictionary<string, int>(3);

            Assert.IsFalse(dictionary.ContainsKey("Z"));
        }

        /// <summary>
        /// Verifies that ContainsKey throws ArgumentNullException when a null key is provided and the key type is a reference type.
        /// </summary>
        [TestMethod]
        public void ContainsKey_WhenKeyIsNull_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, int>(3);

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = dictionary.ContainsKey(null!);
            });
        }

        /// <summary>
        /// Verifies that ContainsKey returns true when a matching key exists under a case-insensitive equality comparer.
        /// </summary>
        [TestMethod]
        public void ContainsKey_WhenComparerIgnoresCaseAndKeyMatches_ShouldReturnTrue()
        {
            var dictionary = new EvictingDictionary<string, int>(
                capacity: 5,
                source: new Dictionary<string, int> { { "KEY", 1 } },
                policy: EvictingDictionaryPolicy.LeastRecentlyUsed,
                comparer: StringComparer.OrdinalIgnoreCase);

            Assert.IsTrue(dictionary.ContainsKey("key"));
        }

        /// <summary>
        /// Verifies that ContainsKey returns false when the key exists but casing differs and the comparer is case-sensitive.
        /// </summary>
        [TestMethod]
        public void ContainsKey_WhenComparerIsCaseSensitiveAndCasingDiffers_ShouldReturnFalse()
        {
            var dictionary = new EvictingDictionary<string, int>(
                capacity: 5,
                source: new Dictionary<string, int> { { "KEY", 1 } },
                policy: EvictingDictionaryPolicy.LeastRecentlyUsed,
                comparer: StringComparer.Ordinal);

            Assert.IsFalse(dictionary.ContainsKey("key"));
        }
    }
}