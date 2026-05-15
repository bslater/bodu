// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.CopyTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> writes items into the array in LeastRecentlyUsed order when space is sufficient.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayHasEnoughSpace_ShouldCopyItemsInLRUOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Touch("A"); // Access A last; order becomes B, A

        var array = new KeyValuePair<string, int>[2];
        dictionary.CopyTo(array, 0);

        Assert.AreEqual("B", array[0].Key);
        Assert.AreEqual(2, array[0].Value);
        Assert.AreEqual("A", array[1].Key);
        Assert.AreEqual(1, array[1].Value);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> throws ArgumentException when the array has a non-zero lower bound.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayHasNonZeroLowerBound_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(1);
        var array = Array.CreateInstance(typeof(KeyValuePair<string, int>), [1], [1]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((System.Collections.ICollection)dictionary).CopyTo(array, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> throws ArgumentException when the destination array is multidimensional.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(1);
        var array = new KeyValuePair<string, int>[1, 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((System.Collections.ICollection)dictionary).CopyTo(array, 0);
        });
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> throws ArgumentNullException when the destination array is null.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.CopyTo(null, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> throws ArgumentException when the destination array is too small.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsTooSmall_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        var array = new KeyValuePair<string, int>[1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.CopyTo(array, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> succeeds when the target array is exactly the size of the dictionary.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArraySizeMatchesDictionaryCount_ShouldCopySuccessfully()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        var array = new KeyValuePair<string, int>[2];
        dictionary.CopyTo(array, 0);

        CollectionAssert.Contains(array, new KeyValuePair<string, int>("A", 1));
        CollectionAssert.Contains(array, new KeyValuePair<string, int>("B", 2));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> performs no action when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDictionaryIsEmpty_ShouldNotCopyAnything()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        var target = Array.Empty<KeyValuePair<string, int>>();
        dictionary.CopyTo(target, 0);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> throws ArgumentOutOfRangeException when the specified index is negative.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(1);
        var array = new KeyValuePair<string, int>[1];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            dictionary.CopyTo(array, -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.CopyTo" /> writes data starting at the specified index offset.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsOffset_ShouldCopyStartingAtOffset()
    {
        var dictionary = new EvictingDictionary<string, int>(1);
        dictionary.Add("Z", 42);

        var array = new KeyValuePair<string, int>[3];
        array[0] = new KeyValuePair<string, int>("existing", 999);

        dictionary.CopyTo(array, 1);

        Assert.AreEqual("existing", array[0].Key);
        Assert.AreEqual("Z", array[1].Key);
        Assert.AreEqual(42, array[1].Value);
    }

}
