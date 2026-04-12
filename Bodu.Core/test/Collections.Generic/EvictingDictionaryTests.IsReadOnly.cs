namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that IsReadOnly returns false when accessed through the ICollection interface.
        /// </summary>
        [TestMethod]
        public void IsReadOnly_ShouldReturnFalse()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            Assert.IsFalse(dictionary.IsReadOnly);
        }
    }
}