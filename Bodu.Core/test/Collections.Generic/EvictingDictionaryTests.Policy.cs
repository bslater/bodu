namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that the Policy property reflects the value set via the constructor.
        /// </summary>
        [TestMethod]
        public void Policy_WhenConstructedWithDefault_ShouldBeLRU()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            Assert.AreEqual(EvictingDictionaryPolicy.LeastRecentlyUsed, dictionary.Policy);
        }

        /// <summary>
        /// Verifies that the Policy property is read-only and cannot be set after construction.
        /// </summary>
        [TestMethod]
        public void Policy_Property_ShouldBeReadOnly()
        {
            var prop = typeof(EvictingDictionary<string, int>).GetProperty(nameof(EvictingDictionary<string, int>.Policy));
            Assert.IsNotNull(prop);
            Assert.IsFalse(prop!.CanWrite, "The Policy property should be read-only.");
        }

        /// <summary>
        /// Verifies that setting the Policy to LeastFrequentlyUsed via constructor reflects the
        /// correct value.
        /// </summary>
        [TestMethod]
        public void Policy_WhenConstructedWithLFU_ShouldReflectLFU()
        {
            var dictionary = new EvictingDictionary<int, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
            Assert.AreEqual(EvictingDictionaryPolicy.LeastFrequentlyUsed, dictionary.Policy);
        }

        /// <summary>
        /// Verifies that setting the Policy to FirstInFirstOut via constructor reflects the correct value.
        /// </summary>
        [TestMethod]
        public void Policy_WhenConstructedWithFIFO_ShouldReflectFIFO()
        {
            var dictionary = new EvictingDictionary<int, int>(3, EvictingDictionaryPolicy.FirstInFirstOut);
            Assert.AreEqual(EvictingDictionaryPolicy.FirstInFirstOut, dictionary.Policy);
        }
    }
}