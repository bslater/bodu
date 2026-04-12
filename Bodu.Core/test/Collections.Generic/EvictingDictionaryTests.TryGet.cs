namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that TryGetValue returns false and sets the out parameter to the default value
        /// when the key does not exist.
        /// </summary>
        [TestMethod]
        public void TryGetValue_WhenKeyDoesNotExist_ShouldReturnFalseAndSetValueToDefault()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("A", 1);

            var actual = dictionary.TryGetValue("B", out var value);

            Assert.IsFalse(actual);
            Assert.AreEqual(default, value);
        }
    }
}