// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Enumerator.MoveNext" /> returns <see langword="false" /> once
    /// every element has been yielded.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEndReached_ShouldReturnFalseFromMoveNext()
    {
        IndexedSet<int> sut = CreateSet([1, 2]);

        IndexedSet<int>.Enumerator enumerator = sut.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that replacing an element via the indexer setter invalidates an enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenIndexerSetterCalledAfterCreation_ShouldThrowExactly()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);
        IndexedSet<int>.Enumerator enumerator = sut.GetEnumerator();

        sut[0] = 99;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that positional mutation (<see cref="IndexedSet{T}.Move(int, int)" />) invalidates an enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenMoveCalledAfterCreation_ShouldThrowExactly()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);
        IndexedSet<int>.Enumerator enumerator = sut.GetEnumerator();

        sut.Move(0, 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Enumerator.Reset" /> restarts iteration from the first element.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetCalled_ShouldRestartIteration()
    {
        IndexedSet<int> sut = CreateSet([1, 2]);

        IndexedSet<int>.Enumerator enumerator = sut.GetEnumerator();
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
    /// <see cref="IndexedSet{T}.Enumerator.MoveNext" /> calls to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenSetMutatedAfterCreation_ShouldThrowExactly()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);
        IndexedSet<int>.Enumerator enumerator = sut.GetEnumerator();

        sut.Add(99);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that mutating the set after creating an enumerator causes
    /// <see cref="IndexedSet{T}.Enumerator.Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenSetMutatedAfterCreation_ForReset_ShouldThrowExactly()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);
        IndexedSet<int>.Enumerator enumerator = sut.GetEnumerator();

        sut.RemoveAt(1);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            enumerator.Reset();
        });
    }

    /// <summary>
    /// Verifies that the enumerator on an empty set yields no elements.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetIsEmpty_ShouldYieldNoElements()
    {
        var sut = new IndexedSet<int>();

        IndexedSet<int>.Enumerator enumerator = sut.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }
    // --------------------------------------------------------
    // GetEnumerator — typed struct
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.GetEnumerator" /> yields elements in insertion order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetPopulated_ShouldYieldAllItemsInOrder()
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30]);
        var seen = new List<int>();

        foreach (var item in sut)
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
        IndexedSet<int> sut = CreateSet([10, 20, 30]);
        IEnumerable untyped = sut;
        var seen = new List<int>();

        foreach (var item in untyped)
            seen.Add((int)item);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, seen);
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
        IndexedSet<int> sut = CreateSet([10, 20, 30]);
        IEnumerable<int> typed = sut;
        var seen = new List<int>();

        foreach (var item in typed)
            seen.Add(item);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, seen);
    }

}
