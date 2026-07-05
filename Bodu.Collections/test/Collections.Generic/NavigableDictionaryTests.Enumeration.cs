// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that the struct enumerator walks every entry in ascending key order with its paired value.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryHasEntries_ShouldWalkAscendingByKey()
    {
        var sut = CreateDictionary(3, 1, 4, 5, 9, 2, 6);

        List<KeyValuePair<int, int>> snapshot = SnapshotByEnumerator(sut);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 9 }, snapshot.Select(e => e.Key).ToList());
        CollectionAssert.AreEqual(new[] { 100, 200, 300, 400, 500, 600, 900 }, snapshot.Select(e => e.Value).ToList());
    }

    /// <summary>
    /// Verifies that enumerating an empty dictionary yields nothing.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryIsEmpty_ShouldYieldNothing()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.AreEqual(0, SnapshotByEnumerator(sut).Count);
    }

    /// <summary>
    /// Verifies that adding an entry mid-enumeration invalidates the enumerator, causing
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAddDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateDictionary(1, 2, 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> entry in sut)
            {
                if (entry.Key == 2)
                    sut.Add(99, 9900);
            }
        });
    }

    /// <summary>
    /// Verifies that removing an entry mid-enumeration invalidates the enumerator, causing
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenRemoveDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateDictionary(1, 2, 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> entry in sut)
            {
                if (entry.Key == 1)
                    sut.Remove(3);
            }
        });
    }

    /// <summary>
    /// Verifies that clearing the dictionary mid-enumeration invalidates the enumerator, causing
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenClearDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateDictionary(1, 2, 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> entry in sut)
                sut.Clear();
        });
    }

    /// <summary>
    /// Verifies that a rejected <see cref="NavigableDictionary{TKey, TValue}.TryAdd" /> (a duplicate key) does not
    /// invalidate an in-flight enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDuplicateTryAddDuringEnumeration_ShouldNotThrow()
    {
        var sut = CreateDictionary(1, 2, 3);
        var seenKeys = new List<int>();

        foreach (KeyValuePair<int, int> entry in sut)
        {
            sut.TryAdd(2, -1);
            seenKeys.Add(entry.Key);
        }

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, seenKeys);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.Enumerator.Reset" /> restarts the walk from the
    /// smallest key.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalledMidWalk_ShouldRestartFromSmallestKey()
    {
        var sut = CreateDictionary(1, 2, 3);
        NavigableDictionary<int, int>.Enumerator enumerator = sut.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current.Key);
        Assert.AreEqual(100, enumerator.Current.Value);
    }

    /// <summary>
    /// Verifies that the interface-typed enumerator surfaces the same ascending key order as the struct enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAccessedThroughIEnumerable_ShouldWalkAscendingByKey()
    {
        var sut = CreateDictionary(2, 1, 3);
        IEnumerable<KeyValuePair<int, int>> boxed = sut;

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, boxed.Select(e => e.Key).ToList());
    }
}
