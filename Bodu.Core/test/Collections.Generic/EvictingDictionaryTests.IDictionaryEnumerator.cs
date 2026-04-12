using System.Collections;

namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that MoveNext iterates over all key-value pairs in insertion order.
        /// </summary>
        [TestMethod]
        public void IDictionaryEnumerator_MoveNext_WhenIterating_ShouldEnumerateAllEntriesInInsertionOrder()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Add("c", 3);

            IDictionaryEnumerator enumerator = ((IDictionary)dictionary).GetEnumerator();

            var results = new List<string>();
            while (enumerator.MoveNext())
            {
                results.Add($"{enumerator.Key}:{enumerator.Value}");
            }

            CollectionAssert.AreEqual(new[] { "a:1", "b:2", "c:3" }, results);
        }

        /// <summary>
        /// Verifies that Entry returns a DictionaryEntry matching the current key-value pair.
        /// </summary>
        [TestMethod]
        public void IDictionaryEnumerator_Entry_WhenAccessed_ShouldReturnMatchingKeyValue()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("x", 10);
            dictionary.Add("y", 20);

            IDictionaryEnumerator enumerator = ((IDictionary)dictionary).GetEnumerator();
            enumerator.MoveNext();

            DictionaryEntry entry = enumerator.Entry;

            Assert.AreEqual(enumerator.Key, entry.Key);
            Assert.AreEqual(enumerator.Value, entry.Value);
        }

        /// <summary>
        /// Verifies that Reset repositions the enumerator to the beginning.
        /// </summary>
        [TestMethod]
        public void IDictionaryEnumerator_Reset_WhenCalled_ShouldRestartEnumerationFromBeginning()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);

            IDictionaryEnumerator enumerator = ((IDictionary)dictionary).GetEnumerator();
            enumerator.MoveNext(); // advance
            enumerator.Reset();    // reset to start

            var entries = new List<string>();
            while (enumerator.MoveNext())
            {
                entries.Add($"{enumerator.Key}:{enumerator.Value}");
            }

            CollectionAssert.AreEqual(new[] { "a:1", "b:2" }, entries);
        }
    }
}