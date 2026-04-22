// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Touch.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> returns false when the key is not found in the dictionary.
        /// </summary>
        [TestMethod]
        public void Touch_WhenKeyIsMissing_ShouldReturnFalse()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("a", 1);

            var actual = dictionary.Touch("missing");

            Assert.IsFalse(actual);
        }

        /// <summary>
        /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> increments the TotalTouches counter when the key exists.
        /// </summary>
        [TestMethod]
        public void Touch_WhenKeyExists_ShouldIncrementTotalTouches()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
            dictionary.Add("key", 123);

            var before = dictionary.TotalTouches;
            dictionary.Touch("key");

            Assert.AreEqual(before + 1, dictionary.TotalTouches);
        }

        /// <summary>
        /// Verifies that TotalTouches is not incremented when the key does not exist.
        /// </summary>
        [TestMethod]
        public void Touch_WhenKeyDoesNotExist_ShouldNotAffectTotalTouches()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
            dictionary.Add("key", 123);

            var before = dictionary.TotalTouches;
            dictionary.Touch("nope");

            Assert.AreEqual(before, dictionary.TotalTouches);
        }

        /// <summary>
        /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> updates access order and affects LeastRecentlyUsed eviction behavior.
        /// </summary>
        [TestMethod]
        public void Touch_WhenKeyIsAccessed_ShouldAffectLRUEvictionOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Touch("A");

            dictionary.Add("C", 3); // Should evict B

            Assert.IsTrue(dictionary.ContainsKey("A"));
            Assert.IsTrue(dictionary.ContainsKey("C"));
            Assert.IsFalse(dictionary.ContainsKey("B"));
        }
    }
}