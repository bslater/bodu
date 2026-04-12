using System.Collections;

namespace Bodu.Collections.Generic
{
    public partial class EvictingDictionaryTests
    {
        /// <summary>
        /// Verifies that SyncRoot consistently returns the same non-null object across calls.
        /// </summary>
        [TestMethod]
        public void ICollection_SyncRoot_WhenCalledMultipleTimes_ShouldReturnSameInstance()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            var sync1 = ((ICollection)dictionary).SyncRoot;
            var sync2 = ((ICollection)dictionary).SyncRoot;

            Assert.IsNotNull(sync1);
            Assert.AreSame(sync1, sync2);
        }

        /// <summary>
        /// Verifies that IsSynchronized returns false to indicate the dictionary is not thread-safe.
        /// </summary>
        [TestMethod]
        public void ICollection_IsSynchronized_WhenAccessed_ShouldReturnFalse()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            Assert.IsFalse(((ICollection)dictionary).IsSynchronized);
        }

        /// <summary>
        /// Verifies that CopyTo successfully populates a DictionaryEntry array when inputs are valid.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenValidArray_ShouldCopyItemsToTarget()
        {
            var dictionary = new EvictingDictionary<string, int>(3);
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);

            var array = new DictionaryEntry[2];
            ((ICollection)dictionary).CopyTo(array, 0);

            Assert.AreEqual("a", array[0].Key);
            Assert.AreEqual(1, array[0].Value);
            Assert.AreEqual("b", array[1].Key);
            Assert.AreEqual(2, array[1].Value);
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentNullException when the array is null.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenArrayIsNull_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, int>(1);
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ((ICollection)dictionary).CopyTo(null!, 0);
            });
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentException when the array is multidimensional.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenArrayIsMultidimensional_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, int>(1);
            var array = new DictionaryEntry[2, 2];

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ((ICollection)dictionary).CopyTo(array, 0);
            });
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentException when the array has a non-zero lower bound.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenArrayHasNonZeroLowerBound_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, int>(1);
            Array array = Array.CreateInstance(typeof(DictionaryEntry), new[] { 2 }, new[] { 1 });

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ((ICollection)dictionary).CopyTo(array, 0);
            });
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentOutOfRangeException when the index is negative.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenIndexIsNegative_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, int>(1);
            var array = new DictionaryEntry[2];

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ((ICollection)dictionary).CopyTo(array, -1);
            });
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentException when the array is too small to hold items.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenArrayIsTooSmall_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, int>(2);
            dictionary.Add("x", 1);
            dictionary.Add("y", 2);

            var array = new DictionaryEntry[1];

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ((ICollection)dictionary).CopyTo(array, 0);
            });
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentException when the object array is multidimensional.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenObjectArrayIsMultidimensional_ShouldThrowExactly()
        {
            var dictionary = new EvictingDictionary<string, int>(1);
            dictionary["A"] = 1;

            var array = new object[1, 1];

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ((ICollection)dictionary).CopyTo(array, 0);
            });
        }
    }
}