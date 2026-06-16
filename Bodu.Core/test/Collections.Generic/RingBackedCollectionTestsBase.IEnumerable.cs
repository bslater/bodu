// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.IEnumerable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that enumerating an empty collection yields no results.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenCollectionIsEmpty_ShouldYieldNoElements()
    {
        TCollection collection = CreateCollection(3);

        int count = 0;
        foreach (int _ in collection)
            count++;

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that enumeration and <see cref="ICollection.CopyTo"/> yield the same results when the
    /// storage is contiguous.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenContiguous_ShouldMatchResultsFromCopyTo()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 10);
        AddToTail(collection, 20);
        AddToTail(collection, 30);

        int[] copyResult = new int[GetCount(collection)];
        ((ICollection)collection).CopyTo(copyResult, 0);
        int[] enumResult = ToArray(collection);

        CollectionAssert.AreEqual(copyResult, enumResult);
    }

    /// <summary>
    /// Verifies that enumeration and <see cref="ICollection.CopyTo"/> yield the same results when the
    /// storage is wrapped.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenStorageWrapped_ShouldMatchResultsFromCopyTo()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);
        AddToTail(collection, 4);
        AddToTail(collection, 5);
        _ = RemoveFromHead(collection);
        AddToTail(collection, 6);

        int[] copyResult = new int[GetCount(collection)];
        ((ICollection)collection).CopyTo(copyResult, 0);
        int[] enumResult = ToArray(collection);

        CollectionAssert.AreEqual(copyResult, enumResult);
    }
    /// <summary>
    /// Verifies that the collection supports iteration via a <see langword="foreach"/> loop in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenUsedInForeachLoop_ShouldEnumerateInHeadToTailOrder()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        var results = new List<int>();
        foreach (int item in collection)
            results.Add(item);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, results);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator"/> yields items as objects in
    /// head-to-tail order.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenUsingNonGenericEnumerator_ShouldReturnItemsAsObjects()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        var actual = new List<int>();
        foreach (object? item in (IEnumerable)collection)
            actual.Add((int)item);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, actual);
    }

}
