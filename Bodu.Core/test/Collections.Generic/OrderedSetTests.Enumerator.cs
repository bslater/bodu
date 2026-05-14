// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{
    // --------------------------------------------------------
    // GetEnumerator — typed struct
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the public <see cref="OrderedSet{T}.GetEnumerator" /> yields all elements in insertion order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetPopulated_ShouldYieldAllItemsInOrder()
    {
        OrderedSet<int> sut = CreateSet([10, 20, 30]);
        var seen = new List<int>();

        foreach (var item in sut)
            seen.Add(item);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, seen);
    }

    /// <summary>
    /// Verifies that the enumerator on an empty set yields no elements.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetIsEmpty_ShouldYieldNoElements()
    {
        var sut = new OrderedSet<int>();

        OrderedSet<int>.Enumerator enumerator = sut.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Enumerator.MoveNext" /> returns <see langword="false" /> once
    /// every element has been yielded.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEndReached_ShouldReturnFalseFromMoveNext()
    {
        OrderedSet<int> sut = CreateSet([1, 2]);

        OrderedSet<int>.Enumerator enumerator = sut.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Enumerator.Reset" /> restarts iteration from the first element.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetCalled_ShouldRestartIteration()
    {
        OrderedSet<int> sut = CreateSet([1, 2]);

        OrderedSet<int>.Enumerator enumerator = sut.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current);

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current);
    }

    // --------------------------------------------------------
    // Versioning / invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that mutating the set after creating an enumerator causes subsequent
    /// <see cref="OrderedSet{T}.Enumerator.MoveNext" /> calls to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenSetMutatedAfterCreation_ShouldThrowOnMoveNext()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);
        OrderedSet<int>.Enumerator enumerator = sut.GetEnumerator();

        sut.Add(99);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that mutating the set after creating an enumerator causes
    /// <see cref="OrderedSet{T}.Enumerator.Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenSetMutatedAfterCreation_ShouldThrowOnReset()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);
        OrderedSet<int>.Enumerator enumerator = sut.GetEnumerator();

        sut.Remove(2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            enumerator.Reset();
        });
    }

    // --------------------------------------------------------
    // Explicit interface implementations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the explicit <see cref="IEnumerable{T}.GetEnumerator" /> implementation yields elements
    /// in insertion order.
    /// </summary>
    [TestMethod]
    public void IEnumerableT_WhenIterated_ShouldYieldAllItemsInOrder()
    {
        OrderedSet<int> sut = CreateSet([10, 20, 30]);
        IEnumerable<int> typed = sut;
        var seen = new List<int>();

        foreach (var item in typed)
            seen.Add(item);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, seen);
    }

    /// <summary>
    /// Verifies that the explicit non-generic <see cref="IEnumerable.GetEnumerator" /> implementation yields
    /// elements in insertion order.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenIterated_ShouldYieldAllItemsInOrder()
    {
        OrderedSet<int> sut = CreateSet([10, 20, 30]);
        IEnumerable untyped = sut;
        var seen = new List<int>();

        foreach (var item in untyped)
            seen.Add((int)item);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, seen);
    }
}
