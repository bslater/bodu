namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that GetEnumerator supports foreach iteration over all entries.
        /// </summary>
        [TestMethod]
        public void IEnumerable_GenericGetEnumerator_WhenUsedInForeach_ShouldEnumerateAllEntries()
        {
            var dictionary = new EvictingDictionary<int, string>(3);
            dictionary.Add(1, "A");
            dictionary.Add(2, "B");
            dictionary.Add(3, "C");

            var actual = new List<KeyValuePair<int, string>>();
            foreach (var kvp in dictionary)
            {
                actual.Add(kvp);
            }

            CollectionAssert.AreEqual(dictionary.ToArray(), actual);
        }

        /// <summary>
        /// Verifies that the non-generic GetEnumerator returns the same entries as the generic one.
        /// </summary>
        [TestMethod]
        public void IEnumerable_NonGenericGetEnumerator_WhenUsed_ShouldReturnSameEntriesAsGenericEnumerator()
        {
            var dictionary = new EvictingDictionary<string, string>(3);
            dictionary.Add("x", "1");
            dictionary.Add("y", "2");

            var actual = new List<KeyValuePair<string, string>>();
            var enumerator = ((System.Collections.IEnumerable)dictionary).GetEnumerator();

            while (enumerator.MoveNext())
            {
                Assert.IsInstanceOfType(enumerator.Current, typeof(KeyValuePair<string, string>));
                actual.Add((KeyValuePair<string, string>)enumerator.Current);
            }

            CollectionAssert.AreEqual(dictionary.ToArray(), actual);
        }

        /// <summary>
        /// Verifies that GetEnumerator returns an empty enumerator when the dictionary is empty.
        /// </summary>
        [TestMethod]
        public void IEnumerable_GenericGetEnumerator_WhenDictionaryIsEmpty_ShouldReturnNoElements()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            var enumerator = dictionary.GetEnumerator();

            Assert.IsFalse(enumerator.MoveNext());
        }
    }
}