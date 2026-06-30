// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.ValueCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.Add" /> implementation on the values view throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ValueCollection_Add_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        ICollection<int> values = dictionary.Values;
        Assert.ThrowsExactly<NotSupportedException>(() => values.Add(42));
    }

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.Clear" /> implementation on the values view throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ValueCollection_Clear_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        ICollection<int> values = dictionary.Values;
        Assert.ThrowsExactly<NotSupportedException>(values.Clear);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.ValueCollection.Contains" /> returns <see langword="true" /> for values
    /// present in the dictionary and <see langword="false" /> for absent values.
    /// </summary>
    [TestMethod]
    public void ValueCollection_Contains_ShouldReflectMembership()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        ICollection<int> values = dictionary.Values;
        Assert.Contains(1, values);
        Assert.Contains(2, values);
        Assert.DoesNotContain(99, values);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo(Array, int)" /> on the values view copies every value to the destination array
    /// starting at the supplied offset.
    /// </summary>
    [TestMethod]
    public void ValueCollection_ICollectionCopyTo_ShouldCopyAllValuesToArray()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        Array array = new int[5];
        ((ICollection)dictionary.Values).CopyTo(array, 1);

        Assert.AreEqual(0, (int)array.GetValue(0)!);
        CollectionAssert.AreEquivalent(
            new[] { 1, 2, 3 },
            new[] { (int)array.GetValue(1)!, (int)array.GetValue(2)!, (int)array.GetValue(3)! });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo(Array, int)" /> on the values view throws <see cref="ArgumentException" /> when the
    /// destination array is multidimensional.
    /// </summary>
    [TestMethod]
    public void ValueCollection_ICollectionCopyTo_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        Array array = new int[2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)dictionary.Values).CopyTo(array, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo(Array, int)" /> on the values view throws <see cref="ArgumentNullException" /> when
    /// the destination array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ValueCollection_ICollectionCopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ((ICollection)dictionary.Values).CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo(Array, int)" /> on the values view throws <see cref="ArgumentOutOfRangeException" />
    /// when the destination index is negative.
    /// </summary>
    [TestMethod]
    public void ValueCollection_ICollectionCopyTo_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        Array array = new int[3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((ICollection)dictionary.Values).CopyTo(array, -1);
        });
    }
    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.IsReadOnly" /> property on the values view reports <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void ValueCollection_IsReadOnly_ShouldReturnTrue()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        ICollection<int> values = dictionary.Values;
        Assert.IsTrue(values.IsReadOnly);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.IsSynchronized" /> on the values view reports <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void ValueCollection_IsSynchronized_ShouldReturnFalse()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        var values = (ICollection)dictionary.Values;
        Assert.IsFalse(values.IsSynchronized);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> on the values view returns the same elements as the
    /// generic enumerator.
    /// </summary>
    [TestMethod]
    public void ValueCollection_NonGenericGetEnumerator_ShouldYieldSameElements()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        IEnumerable values = dictionary.Values;
        var observed = new List<int>();
        foreach (object? value in values)
            observed.Add((int)value);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, observed);
    }

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.Remove" /> implementation on the values view throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ValueCollection_Remove_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        ICollection<int> values = dictionary.Values;
        Assert.ThrowsExactly<NotSupportedException>(() => values.Remove(1));
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.SyncRoot" /> on the values view returns the dictionary's sync root.
    /// </summary>
    [TestMethod]
    public void ValueCollection_SyncRoot_ShouldReturnDictionarySyncRoot()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        object syncRoot = ((ICollection)dictionary.Values).SyncRoot;
        Assert.IsNotNull(syncRoot);
        Assert.AreSame(((ICollection)dictionary).SyncRoot, syncRoot);
    }

}
