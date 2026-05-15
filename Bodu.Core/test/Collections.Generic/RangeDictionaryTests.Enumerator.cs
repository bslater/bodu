// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{

    /// <summary>
    /// Verifies that mutating the dictionary via <see cref="RangeDictionary{TKey, TValue}.Clear" />
    /// invalidates an enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDictionaryClearedAfterCreation_ShouldThrowOnReset()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"));
        RangeDictionary<int, string>.Enumerator enumerator = sut.GetEnumerator();

        sut.Clear();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            enumerator.Reset();
        });
    }

    // --------------------------------------------------------
    // Versioning / invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that mutating the dictionary after creating an enumerator causes subsequent
    /// <see cref="RangeDictionary{TKey, TValue}.Enumerator.MoveNext" /> calls to throw
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDictionaryMutatedByAddAfterCreation_ShouldThrowOnMoveNext()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"));
        RangeDictionary<int, string>.Enumerator enumerator = sut.GetEnumerator();

        sut.Add(10, 15, "B");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that mutating the dictionary via <see cref="RangeDictionary{TKey, TValue}.Remove(TKey, TKey)" />
    /// invalidates an enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDictionaryMutatedByRemoveAfterCreation_ShouldThrowOnMoveNext()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"));
        RangeDictionary<int, string>.Enumerator enumerator = sut.GetEnumerator();

        sut.Remove(0, 5);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.Enumerator.MoveNext" /> returns
    /// <see langword="false" /> after the last element.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEndReached_ShouldReturnFalseFromMoveNext()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"), (10, 15, "B"));

        RangeDictionary<int, string>.Enumerator enumerator = sut.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.Enumerator.Reset" /> restarts iteration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetCalled_ShouldRestartIteration()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"), (10, 15, "B"));

        RangeDictionary<int, string>.Enumerator enumerator = sut.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("A", enumerator.Current.Value);

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("A", enumerator.Current.Value);
    }

    /// <summary>
    /// Verifies that the enumerator on an empty dictionary yields no elements.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryIsEmpty_ShouldYieldNoElements()
    {
        var sut = new RangeDictionary<int, string>();

        RangeDictionary<int, string>.Enumerator enumerator = sut.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }
    // --------------------------------------------------------
    // GetEnumerator — typed struct
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.GetEnumerator" /> yields entries in ascending
    /// start order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryPopulated_ShouldYieldEntriesInOrder()
    {
        var sut = new RangeDictionary<int, string>();
        sut.Add(20, 25, "C");
        sut.Add(0, 5, "A");
        sut.Add(10, 15, "B");

        var seen = new List<string>();

        foreach (ValueRange<int, string> entry in sut)
            seen.Add(entry.Value);

        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, seen);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> implementation yields the same
    /// sequence boxed as <see cref="object" />.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenIterated_ShouldYieldEntriesInOrder()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"), (10, 15, "B"));
        IEnumerable untyped = sut;
        var seen = new List<string>();

        foreach (var item in untyped)
            seen.Add(((ValueRange<int, string>)item).Value);

        CollectionAssert.AreEqual(new[] { "A", "B" }, seen);
    }

    // --------------------------------------------------------
    // Explicit interface implementations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IEnumerable{T}.GetEnumerator" /> on the typed interface yields the same
    /// sequence as the public struct enumerator.
    /// </summary>
    [TestMethod]
    public void IEnumerableT_WhenIterated_ShouldYieldEntriesInOrder()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"), (10, 15, "B"));
        IEnumerable<ValueRange<int, string>> typed = sut;
        var seen = new List<string>();

        foreach (ValueRange<int, string> entry in typed)
            seen.Add(entry.Value);

        CollectionAssert.AreEqual(new[] { "A", "B" }, seen);
    }

}
