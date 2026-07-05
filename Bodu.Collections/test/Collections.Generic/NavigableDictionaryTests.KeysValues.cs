// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.KeysValues.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Keys" /> enumerates the keys in ascending comparer
    /// order.
    /// </summary>
    [TestMethod]
    public void Keys_WhenDictionaryHasEntries_ShouldEnumerateKeysAscending()
    {
        var sut = CreateDictionary(30, 10, 20);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, sut.Keys.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Values" /> enumerates the values in ascending key
    /// order.
    /// </summary>
    [TestMethod]
    public void Values_WhenDictionaryHasEntries_ShouldEnumerateValuesInKeyOrder()
    {
        var sut = CreateDictionary(30, 10, 20);

        CollectionAssert.AreEqual(new[] { 1000, 2000, 3000 }, sut.Values.ToList());
    }

    /// <summary>
    /// Verifies that the key and value views are live: mutations to the dictionary are visible through a previously
    /// obtained view.
    /// </summary>
    [TestMethod]
    public void KeysValues_WhenDictionaryMutated_ShouldReflectCurrentState()
    {
        var sut = CreateDictionary(10, 30);
        ICollection<int> keys = sut.Keys;
        ICollection<int> values = sut.Values;

        sut.Add(20, 2000);

        Assert.AreEqual(3, keys.Count);
        Assert.AreEqual(3, values.Count);
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, keys.ToList());
        CollectionAssert.AreEqual(new[] { 1000, 2000, 3000 }, values.ToList());
    }

    /// <summary>
    /// Verifies that the key and value views are cached per instance: repeated property reads return the same
    /// collection object.
    /// </summary>
    [TestMethod]
    public void KeysValues_WhenReadRepeatedly_ShouldReturnCachedInstances()
    {
        var sut = CreateDictionary(1, 2);

        Assert.AreSame(sut.Keys, sut.Keys);
        Assert.AreSame(sut.Values, sut.Values);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.KeyCollection.Contains" /> answers key membership
    /// through the dictionary's comparer.
    /// </summary>
    [TestMethod]
    public void Keys_WhenContainsQueried_ShouldAnswerMembership()
    {
        var sut = CreateDictionary(10, 20, 30);
        ICollection<int> keys = sut.Keys;

        Assert.IsTrue(keys.Contains(20));
        Assert.IsFalse(keys.Contains(25));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ValueCollection.Contains" /> answers value
    /// membership via the linear <see cref="NavigableDictionary{TKey, TValue}.ContainsValue" /> walk.
    /// </summary>
    [TestMethod]
    public void Values_WhenContainsQueried_ShouldAnswerMembership()
    {
        var sut = CreateDictionary(10, 20, 30);
        ICollection<int> values = sut.Values;

        Assert.IsTrue(values.Contains(2000));
        Assert.IsFalse(values.Contains(999));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.KeyCollection.CopyTo" /> copies the keys in
    /// ascending order starting at the requested index.
    /// </summary>
    [TestMethod]
    public void Keys_WhenCopiedToArray_ShouldCopyAscendingFromIndex()
    {
        var sut = CreateDictionary(30, 10, 20);
        int[] destination = new int[5];

        sut.Keys.CopyTo(destination, 1);

        CollectionAssert.AreEqual(new[] { 0, 10, 20, 30, 0 }, destination);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.ValueCollection.CopyTo" /> copies the values in
    /// ascending key order starting at the requested index.
    /// </summary>
    [TestMethod]
    public void Values_WhenCopiedToArray_ShouldCopyInKeyOrderFromIndex()
    {
        var sut = CreateDictionary(30, 10, 20);
        int[] destination = new int[5];

        sut.Values.CopyTo(destination, 1);

        CollectionAssert.AreEqual(new[] { 0, 1000, 2000, 3000, 0 }, destination);
    }

    /// <summary>
    /// Verifies that the key view is read-only: <see cref="ICollection{T}.Add" />,
    /// <see cref="ICollection{T}.Clear" />, and <see cref="ICollection{T}.Remove" /> throw
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Keys_WhenMutatedDirectly_ShouldThrowNotSupportedException()
    {
        var sut = CreateDictionary(1, 2, 3);
        ICollection<int> keys = sut.Keys;

        Assert.IsTrue(keys.IsReadOnly);
        Assert.ThrowsExactly<NotSupportedException>(() => { keys.Add(4); });
        Assert.ThrowsExactly<NotSupportedException>(() => { keys.Clear(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { keys.Remove(1); });
    }

    /// <summary>
    /// Verifies that the value view is read-only: <see cref="ICollection{T}.Add" />,
    /// <see cref="ICollection{T}.Clear" />, and <see cref="ICollection{T}.Remove" /> throw
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Values_WhenMutatedDirectly_ShouldThrowNotSupportedException()
    {
        var sut = CreateDictionary(1, 2, 3);
        ICollection<int> values = sut.Values;

        Assert.IsTrue(values.IsReadOnly);
        Assert.ThrowsExactly<NotSupportedException>(() => { values.Add(400); });
        Assert.ThrowsExactly<NotSupportedException>(() => { values.Clear(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { values.Remove(100); });
    }

    /// <summary>
    /// Verifies that structurally mutating the dictionary mid-enumeration of a key or value view throws
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void KeysValues_WhenMutatedMidEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateDictionary(1, 2, 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int key in sut.Keys)
            {
                if (key == 2)
                    sut.Remove(3);
            }
        });

        var sut2 = CreateDictionary(1, 2, 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int value in sut2.Values)
            {
                if (value == 200)
                    sut2.Remove(3);
            }
        });
    }

    /// <summary>
    /// Verifies that the <see cref="IReadOnlyDictionary{TKey, TValue}" /> key and value projections surface the same
    /// sorted views.
    /// </summary>
    [TestMethod]
    public void KeysValues_WhenAccessedThroughIReadOnlyDictionary_ShouldSurfaceSortedViews()
    {
        var sut = CreateDictionary(30, 10, 20);
        IReadOnlyDictionary<int, int> boxed = sut;

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, boxed.Keys.ToList());
        CollectionAssert.AreEqual(new[] { 1000, 2000, 3000 }, boxed.Values.ToList());
    }
}
