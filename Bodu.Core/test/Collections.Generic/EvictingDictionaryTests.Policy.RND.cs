// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Policy.RND.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that Add randomly evicts an item when capacity is exceeded using
        /// RandomReplacement policy.
        /// </summary>
        [TestMethod]
        [TestCategory("Random")]
        public void Add_WhenPolicyIsRandomAndCapacityExceeded_ShouldEvictRandomItem()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.RandomReplacement);
            dictionary.Add("one", 1);
            dictionary.Add("two", 2);
            dictionary.Add("three", 3);

            Assert.AreEqual(2, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey("three")); // Always present
        }

        /// <summary>
        /// Verifies that Clear removes all items in a RandomReplacement dictionary.
        /// </summary>
        [TestMethod]
        [TestCategory("Random")]
        public void Clear_WhenPolicyIsRandom_ShouldRemoveAllItems()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.RandomReplacement);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Count);
        }

        /// <summary>
        /// Verifies that ItemEvicted is triggered when eviction occurs using RandomReplacement policy.
        /// </summary>
        [TestMethod]
        [TestCategory("Random")]
        public void ItemEvicted_WhenPolicyIsRandom_ShouldBeTriggeredOnEviction()
        {
            var evicted = new List<string>();
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.RandomReplacement);
            dictionary.ItemEvicted += (key, _) => evicted.Add(key);

            dictionary.Add("X", 1);
            dictionary.Add("Y", 2);
            dictionary.Add("Z", 3);

            Assert.IsTrue(evicted.Count > 0);
        }

        /// <summary>
        /// Verifies that eviction does not throw when candidate was removed before eviction under
        /// RandomReplacement policy.
        /// </summary>
        [TestMethod]
        [TestCategory("Random")]
        public void EvictionEvents_WhenPolicyIsRandomAndCandidateRemovedBeforeEviction_ShouldNotThrow()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.RandomReplacement);
            dictionary.Add("X", 1);
            dictionary.Add("Y", 2);
            dictionary.Remove("X");

            dictionary.Add("Z", 3);

            Assert.AreEqual(2, dictionary.Count);
        }

        /// <summary>
        /// Verifies that PeekEvictionCandidate returns a valid key when using RandomReplacement.
        /// </summary>
        [TestMethod]
        [TestCategory("Random")]
        public void PeekEvictionCandidate_WhenPolicyIsRandom_ShouldReturnSomeKey()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.RandomReplacement);
            dictionary.Add("alpha", 1);
            dictionary.Add("beta", 2);

            var candidate = dictionary.PeekEvictionCandidate();
            Assert.IsTrue(dictionary.ContainsKey(candidate));
        }

        /// <summary>
        /// Verifies that ItemEvicting is triggered before ItemEvicted when eviction occurs using RandomReplacement.
        /// </summary>
        [TestMethod]
        [TestCategory("Random")]
        public void ItemEvicted_WhenPolicyIsRandom_ShouldFireAfterItemEvicting()
        {
            var sequence = new List<string>();
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.RandomReplacement);

            dictionary.ItemEvicting += (key, value) => sequence.Add($"Evicting:{key}:{value}");
            dictionary.ItemEvicted += (key, value) => sequence.Add($"Evicted:{key}:{value}");

            dictionary.Add("X", 1);
            dictionary.Add("Y", 2);
            dictionary.Add("Z", 3); // Trigger eviction

            Assert.AreEqual("Evicting", sequence[0].Split(':')[0]);
            Assert.AreEqual("Evicted", sequence[1].Split(':')[0]);
        }

        /// <summary>
        /// Verifies that dictionary Keys reflects current state after evictions under
        /// RandomReplacement policy.
        /// </summary>
        [TestMethod]
        [TestCategory("Random")]
        public void Keys_WhenPolicyIsRandom_ShouldReflectEvictions()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.RandomReplacement);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Add("c", 3);

            Assert.AreEqual(2, dictionary.Keys.Count);
            Assert.IsTrue(dictionary.ContainsKey("c"));
        }

        /// <summary>
        /// Verifies that RandomReplacement actually selects different victims across many independent runs,
        /// rather than deterministically evicting the same key each time.
        /// </summary>
        [TestMethod]
        [TestCategory("Random")]
        public void Add_WhenPolicyIsRandomAndCapacityExceededAcrossManyRuns_ShouldEvictDifferentKeysOverTime()
        {
            const int trials = 200;
            var evictedKeys = new HashSet<string>();

            for (int i = 0; i < trials; i++)
            {
                var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.RandomReplacement);
                dictionary.Add("A", 1);
                dictionary.Add("B", 2);
                dictionary.Add("C", 3);

                string? evicted = null;
                dictionary.ItemEvicted += (key, _) => evicted = key;

                dictionary.Add("D", 4);

                Assert.IsNotNull(evicted);
                evictedKeys.Add(evicted!);

                // Once we have observed at least two distinct victims we are confident the selection is non-deterministic.
                if (evictedKeys.Count >= 2)
                    return;
            }

            Assert.Fail(
                $"RandomReplacement evicted the same key in all {trials} trials (observed: {string.Join(',', evictedKeys)}); " +
                "expected non-deterministic victim selection.");
        }
    }
}