namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that Keys returns an empty collection when the dictionary is empty.
        /// </summary>
        [TestMethod]
        public void Keys_Get_WhenEmpty_ShouldReturnEmptyCollection()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            Assert.AreEqual(0, dictionary.Keys.Count);
        }
    }
}