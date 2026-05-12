// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.KeyCollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.KeyCollection.Contains" /> returns <see langword="true" /> for keys
    /// present in the dictionary and <see langword="false" /> otherwise.
    /// </summary>
    [TestMethod]
    public void KeyCollection_Contains_ShouldReflectDictionaryMembership()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        var keys = dictionary.Keys;
        Assert.IsTrue(keys.Contains("A"));
        Assert.IsFalse(keys.Contains("Z"));
    }

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.IsReadOnly" /> property on the keys view reports <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void KeyCollection_IsReadOnly_ShouldReturnTrue()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        ICollection<string> keys = dictionary.Keys;
        Assert.IsTrue(keys.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.Clear" /> implementation on the keys view throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void KeyCollection_Clear_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        ICollection<string> keys = dictionary.Keys;
        Assert.ThrowsExactly<NotSupportedException>(() => keys.Clear());
    }

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.Remove" /> implementation on the keys view throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void KeyCollection_Remove_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        ICollection<string> keys = dictionary.Keys;
        Assert.ThrowsExactly<NotSupportedException>(() => keys.Remove("A"));
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.IsSynchronized" /> on the keys view reports <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void KeyCollection_IsSynchronized_ShouldReturnFalse()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        ICollection keys = (ICollection)dictionary.Keys;
        Assert.IsFalse(keys.IsSynchronized);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.SyncRoot" /> on the keys view returns the dictionary's sync root.
    /// </summary>
    [TestMethod]
    public void KeyCollection_SyncRoot_ShouldReturnDictionarySyncRoot()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        var syncRoot = ((ICollection)dictionary.Keys).SyncRoot;
        Assert.IsNotNull(syncRoot);
        Assert.AreSame(((ICollection)dictionary).SyncRoot, syncRoot);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> on the keys view returns the same elements as the generic
    /// enumerator.
    /// </summary>
    [TestMethod]
    public void KeyCollection_NonGenericGetEnumerator_ShouldYieldSameElements()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        IEnumerable keys = dictionary.Keys;
        var observed = new List<string>();
        foreach (object key in keys)
            observed.Add((string)key);

        CollectionAssert.AreEquivalent(new[] { "A", "B", "C" }, observed);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo(Array, int)" /> on the keys view copies every key to the destination array starting
    /// at the supplied offset.
    /// </summary>
    [TestMethod]
    public void KeyCollection_ICollectionCopyTo_ShouldCopyAllKeysToArray()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        Array array = new string[5];
        ((ICollection)dictionary.Keys).CopyTo(array, 1);

        Assert.IsNull(array.GetValue(0));
        CollectionAssert.AreEquivalent(
            new[] { "A", "B", "C" },
            new[] { (string?)array.GetValue(1), (string?)array.GetValue(2), (string?)array.GetValue(3) });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo(Array, int)" /> on the keys view throws <see cref="ArgumentNullException" /> when the
    /// destination array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void KeyCollection_ICollectionCopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ((ICollection)dictionary.Keys).CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo(Array, int)" /> on the keys view throws <see cref="ArgumentException" /> when the
    /// destination array is multidimensional.
    /// </summary>
    [TestMethod]
    public void KeyCollection_ICollectionCopyTo_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        Array array = new string[2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)dictionary.Keys).CopyTo(array, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo(Array, int)" /> on the keys view throws <see cref="ArgumentOutOfRangeException" />
    /// when the destination index is negative.
    /// </summary>
    [TestMethod]
    public void KeyCollection_ICollectionCopyTo_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        Array array = new string[3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((ICollection)dictionary.Keys).CopyTo(array, -1);
        });
    }
}
