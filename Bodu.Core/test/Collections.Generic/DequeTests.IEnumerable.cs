// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.IEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that the deque supports iteration via a <see langword="foreach"/> loop.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenUsedInForeachLoop_ShouldEnumerateInHeadToTailOrder()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);

        var results = new List<int>();
        foreach (var item in deque)
            results.Add(item);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, results);
    }

    /// <summary>
    /// Verifies that the generic enumerator produces the expected sequence.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenUsingGenericEnumerator_ShouldEnumerateCorrectly()
    {
        var deque = new Deque<string>(2);
        deque.AddLast("x");
        deque.AddLast("y");

        var list = deque.ToList();
        CollectionAssert.AreEqual(new[] { "x", "y" }, list);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator"/> yields items in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenUsingNonGenericEnumerator_ShouldReturnItemsAsObjects()
    {
        IEnumerable deque = new Deque<int>(3);
        ((Deque<int>)deque).AddLast(1);
        ((Deque<int>)deque).AddLast(2);
        ((Deque<int>)deque).AddLast(3);

        var actual = new List<int>();
        foreach (var item in deque)
            actual.Add((int)item);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, actual);
    }

    /// <summary>
    /// Verifies that enumerating an empty deque yields no results.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenDequeIsEmpty_ShouldYieldNoElements()
    {
        var deque = new Deque<int>();
        var actual = deque.ToList();

        CollectionAssert.AreEqual(Array.Empty<int>(), actual);
    }

    /// <summary>
    /// Verifies that enumeration and CopyTo yield the same results when the deque is contiguous.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenContiguous_ShouldMatchResultsFromCopyTo()
    {
        var deque = new Deque<int>(8);
        deque.AddLast(10);
        deque.AddLast(20);
        deque.AddLast(30);

        var copyResult = new int[deque.Count];
        ((ICollection)deque).CopyTo(copyResult, 0);
        var enumResult = deque.ToArray();

        CollectionAssert.AreEqual(copyResult, enumResult);
    }

    /// <summary>
    /// Verifies that enumeration and CopyTo yield the same results across an auto-grow event.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenAutoGrew_ShouldMatchResultsFromCopyTo()
    {
        var deque = new Deque<int>(2);
        for (int i = 0; i < 12; i++)
            deque.AddLast(i);

        var copyResult = new int[deque.Count];
        ((ICollection)deque).CopyTo(copyResult, 0);
        var enumResult = deque.ToArray();

        CollectionAssert.AreEqual(copyResult, enumResult);
    }
}
