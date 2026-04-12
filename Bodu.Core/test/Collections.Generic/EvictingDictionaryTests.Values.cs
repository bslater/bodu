namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that the Values collection returns an empty collection when the dictionary has
        /// no entries.
        /// </summary>
        [TestMethod]
        public void Values_WhenDictionaryIsEmpty_ShouldReturnEmptyCollection()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            Assert.AreEqual(0, dictionary.Values.Count);
        }
    }
}