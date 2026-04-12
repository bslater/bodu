namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that TouchOrThrow throws KeyNotFoundException when the key does not exist.
        /// </summary>
        [TestMethod]
        public void TouchOrThrow_WhenKeyIsMissing_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, int>(3);

            Assert.ThrowsExactly<KeyNotFoundException>(() => dictionary.TouchOrThrow("not-found"));
        }

        /// <summary>
        /// Verifies that TouchOrThrow increments TotalTouches when the key exists.
        /// </summary>
        [TestMethod]
        public void TouchOrThrow_WhenKeyExists_ShouldIncrementTotalTouches()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("a", 1);
            var before = dictionary.TotalTouches;

            dictionary.TouchOrThrow("a");

            Assert.AreEqual(before + 1, dictionary.TotalTouches);
        }

        /// <summary>
        /// Verifies that TouchOrThrow does not increment TotalTouches when the key is missing and the exception is thrown.
        /// </summary>
        [TestMethod]
        public void TouchOrThrow_WhenKeyIsMissing_ShouldNotIncrementTotalTouches()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            var before = dictionary.TotalTouches;

            Assert.ThrowsExactly<KeyNotFoundException>(() => dictionary.TouchOrThrow("ghost"));

            Assert.AreEqual(before, dictionary.TotalTouches);
        }
    }
}