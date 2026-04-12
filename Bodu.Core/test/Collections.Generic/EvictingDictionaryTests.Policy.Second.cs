namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that Add evicts the first item without second chance when capacity is exceeded
        /// using SecondChance policy.
        /// </summary>
        [TestMethod]
        [TestCategory("SecondChance")]
        public void Add_WhenPolicyIsSCAndCapacityExceeded_ShouldEvictItemWithoutSecondChance()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Touch("A"); // Give A a second chance
            dictionary.Add("C", 3); // B should be evicted

            Assert.IsTrue(dictionary.ContainsKey("A"));
            Assert.IsFalse(dictionary.ContainsKey("B"));
            Assert.IsTrue(dictionary.ContainsKey("C"));
        }

        /// <summary>
        /// Verifies that Touch grants a second chance to an item under SecondChance policy.
        /// </summary>
        [TestMethod]
        [TestCategory("SecondChance")]
        public void Touch_WhenPolicyIsSC_ShouldGrantSecondChance()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
            dictionary.Add("X", 10);
            dictionary.Add("Y", 20);

            dictionary.Touch("X"); // X gets second chance
            dictionary.Add("Z", 30); // Y evicted

            Assert.IsTrue(dictionary.ContainsKey("X"));
            Assert.IsFalse(dictionary.ContainsKey("Y"));
            Assert.IsTrue(dictionary.ContainsKey("Z"));
        }

        /// <summary>
        /// Verifies that Touch returns true for existing keys under SecondChance policy.
        /// </summary>
        [TestMethod]
        public void Touch_WhenPolicyIsSCAndKeyExists_ShouldReturnTrue()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
            dictionary.Add("key", 42);

            var actual = dictionary.Touch("key");

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Verifies that TouchOrThrow does not throw and sets second chance flag under SecondChance policy.
        /// </summary>
        [TestMethod]
        public void TouchOrThrow_WhenPolicyIsSCAndKeyExists_ShouldSetSecondChance()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
            dictionary.Add("key", 100);

            dictionary.TouchOrThrow("key");

            Assert.IsTrue(dictionary.ContainsKey("key"));
        }

        /// <summary>
        /// Verifies that PeekEvictionCandidate skips items with second chance and returns the
        /// correct candidate.
        /// </summary>
        [TestMethod]
        public void PeekEvictionCandidate_WhenPolicyIsSC_ShouldSkipSecondChanceItems()
        {
            var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.SecondChance);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Add("c", 3);
            dictionary.Touch("a");

            var candidate = dictionary.PeekEvictionCandidate();

            Assert.AreEqual("b", candidate);
        }

        /// <summary>
        /// Verifies that Clear resets second chance tracking in SecondChance policy.
        /// </summary>
        [TestMethod]
        public void Clear_WhenPolicyIsSC_ShouldResetSecondChanceFlags()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
            dictionary.Add("A", 1);
            dictionary.Touch("A");

            dictionary.Clear();
            dictionary.Add("B", 2);
            dictionary.Add("C", 3);

            Assert.IsTrue(dictionary.ContainsKey("B"));
            Assert.IsTrue(dictionary.ContainsKey("C"));
        }

        /// <summary>
        /// Verifies that items with second chance are cycled to the back of the queue during eviction.
        /// </summary>
        [TestMethod]
        public void Add_WhenPolicyIsSCAndAllHaveSecondChance_ShouldEvictAfterClearingFlags()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Touch("A");
            dictionary.Touch("B");
            dictionary.Add("C", 3); // A and B cycle, A evicted

            Assert.IsFalse(dictionary.ContainsKey("A"));
            Assert.IsTrue(dictionary.ContainsKey("B"));
            Assert.IsTrue(dictionary.ContainsKey("C"));
        }

        /// <summary>
        /// Verifies that iteration reflects correct item order under SecondChance policy.
        /// </summary>
        [TestMethod]
        public void IEnumerable_GetEnumerator_WhenPolicyIsSC_ShouldRespectInsertionAndRotation()
        {
            var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Touch("a");
            dictionary.Add("c", 3); // b should be evicted next

            var expectedKeys = new[] { "a", "c" };
            CollectionAssert.AreEquivalent(expectedKeys, dictionary.Keys.ToList());
        }
    }
}