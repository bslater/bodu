// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.Enumerator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Enumerator.MoveNext" /> returns <see langword="false" /> after
    /// the last element.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEndReached_ShouldReturnFalseFromMoveNext()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        RangeSet<int>.Enumerator enumerator = sut.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.Enumerator.Reset" /> restarts iteration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetCalled_ShouldRestartIteration()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));

        RangeSet<int>.Enumerator enumerator = sut.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(new Range<int>(0, 5), enumerator.Current);

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(new Range<int>(0, 5), enumerator.Current);
    }

    /// <summary>
    /// Verifies that mutating the set via <see cref="RangeSet{T}.Clear" /> after creating an enumerator
    /// invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenSetClearedAfterCreation_ShouldThrowExactly()
    {
        RangeSet<int> sut = CreateSet((0, 5));
        RangeSet<int>.Enumerator enumerator = sut.GetEnumerator();

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
    /// Verifies that mutating the set after creating an enumerator causes subsequent
    /// <see cref="RangeSet{T}.Enumerator.MoveNext" /> calls to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenSetMutatedByAddAfterCreation_ShouldThrowExactly()
    {
        RangeSet<int> sut = CreateSet((0, 5));
        RangeSet<int>.Enumerator enumerator = sut.GetEnumerator();

        sut.Add(10, 15);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that mutating the set via <see cref="RangeSet{T}.Remove(T, T)" /> after creating an enumerator
    /// invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenSetMutatedByRemoveAfterCreation_ShouldThrowExactly()
    {
        RangeSet<int> sut = CreateSet((0, 10));
        RangeSet<int>.Enumerator enumerator = sut.GetEnumerator();

        sut.Remove(3, 5);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that the enumerator on an empty set yields no elements.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetIsEmpty_ShouldYieldNoElements()
    {
        var sut = new RangeSet<int>();

        RangeSet<int>.Enumerator enumerator = sut.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }
    // --------------------------------------------------------
    // GetEnumerator — typed struct
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.GetEnumerator" /> yields ranges in ascending order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetPopulated_ShouldYieldRangesInOrder()
    {
        RangeSet<int> sut = CreateSet((20, 25), (0, 5), (10, 15));
        var seen = new List<Range<int>>();

        foreach (Range<int> range in sut)
            seen.Add(range);

        CollectionAssert.AreEqual(
            new[] { new Range<int>(0, 5), new Range<int>(10, 15), new Range<int>(20, 25) },
            seen);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> implementation yields the same
    /// sequence boxed as <see cref="object" />.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenIterated_ShouldYieldRangesInOrder()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));
        IEnumerable untyped = sut;
        var seen = new List<Range<int>>();

        foreach (var item in untyped)
            seen.Add((Range<int>)item);

        Assert.HasCount(2, seen);
        Assert.AreEqual(new Range<int>(0, 5), seen[0]);
        Assert.AreEqual(new Range<int>(10, 15), seen[1]);
    }

    // --------------------------------------------------------
    // Explicit interface implementations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IEnumerable{T}.GetEnumerator" /> on the typed interface yields the same
    /// sequence as the public struct enumerator.
    /// </summary>
    [TestMethod]
    public void IEnumerableT_WhenIterated_ShouldYieldRangesInOrder()
    {
        RangeSet<int> sut = CreateSet((0, 5), (10, 15));
        IEnumerable<Range<int>> typed = sut;
        var seen = new List<Range<int>>();

        foreach (Range<int> range in typed)
            seen.Add(range);

        Assert.HasCount(2, seen);
        Assert.AreEqual(new Range<int>(0, 5), seen[0]);
        Assert.AreEqual(new Range<int>(10, 15), seen[1]);
    }

}
