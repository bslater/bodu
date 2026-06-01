// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that a <c>foreach</c> over the set yields every element exactly once.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetHasElements_ShouldYieldEveryElement()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3, 4, 5]);

        var observed = new List<int>();
        foreach (var item in set)
            observed.Add(item);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, observed);
    }

    /// <summary>
    /// Verifies that the enumerator of an empty set reports end-of-sequence on the first <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetIsEmpty_ShouldYieldNothing()
    {
        var set = new ConcurrentHashSet<int>();

        ConcurrentHashSet<int>.Enumerator enumerator = set.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the enumerator iterates a point-in-time snapshot — additions and removals made after the
    /// enumerator is created are not observed.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetMutatedAfterCreation_ShouldYieldOriginalSnapshot()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);
        ConcurrentHashSet<int>.Enumerator enumerator = set.GetEnumerator();

        set.Add(4);
        set.Remove(1);

        var observed = new List<int>();
        while (enumerator.MoveNext())
            observed.Add(enumerator.Current);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, observed);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Enumerator.Reset" /> rewinds the enumerator so a second walk
    /// re-yields the same snapshot.
    /// </summary>
    [TestMethod]
    public void Enumerator_Reset_ShouldRewindToBeginning()
    {
        var set = new ConcurrentHashSet<int>([10, 20, 30]);
        ConcurrentHashSet<int>.Enumerator enumerator = set.GetEnumerator();

        var first = new List<int>();
        while (enumerator.MoveNext())
            first.Add(enumerator.Current);

        enumerator.Reset();

        var second = new List<int>();
        while (enumerator.MoveNext())
            second.Add(enumerator.Current);

        Assert.HasCount(3, first);
        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Enumerator.Current" /> is the type default before the first
    /// <c>MoveNext</c> call.
    /// </summary>
    [TestMethod]
    public void Enumerator_Current_BeforeFirstMoveNext_ShouldBeDefault()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        ConcurrentHashSet<int>.Enumerator enumerator = set.GetEnumerator();

        Assert.AreEqual(0, enumerator.Current);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Enumerator.Current" /> is the type default after enumeration has
    /// passed the end of the snapshot.
    /// </summary>
    [TestMethod]
    public void Enumerator_Current_AfterEnumerationEnds_ShouldBeDefault()
    {
        var set = new ConcurrentHashSet<int>([1]);
        ConcurrentHashSet<int>.Enumerator enumerator = set.GetEnumerator();

        while (enumerator.MoveNext())
        {
        }

        Assert.AreEqual(0, enumerator.Current);
    }

    /// <summary>
    /// Verifies that calling <c>MoveNext</c> again after it has returned <see langword="false" /> keeps returning
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Enumerator_MoveNext_AfterEnd_ShouldKeepReturningFalse()
    {
        var set = new ConcurrentHashSet<int>([1]);
        ConcurrentHashSet<int>.Enumerator enumerator = set.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="System.Collections.IEnumerator.Current" /> exposes the current
    /// element.
    /// </summary>
    [TestMethod]
    public void Enumerator_NonGenericCurrent_ShouldExposeCurrentElement()
    {
        var set = new ConcurrentHashSet<int>([42]);
        System.Collections.IEnumerator enumerator = set.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(42, enumerator.Current);
    }

    /// <summary>
    /// Verifies that two enumerators obtained from the same set advance independently.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenTwoEnumeratorsObtained_ShouldAdvanceIndependently()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        ConcurrentHashSet<int>.Enumerator first = set.GetEnumerator();
        ConcurrentHashSet<int>.Enumerator second = set.GetEnumerator();

        Assert.IsTrue(first.MoveNext());

        var secondObserved = new List<int>();
        while (second.MoveNext())
            secondObserved.Add(second.Current);

        Assert.HasCount(3, secondObserved);
    }

    /// <summary>
    /// Verifies that disposing the enumerator is safe and may be called more than once.
    /// </summary>
    [TestMethod]
    public void Enumerator_Dispose_ShouldBeSafeAndIdempotent()
    {
        var set = new ConcurrentHashSet<int>([1, 2]);
        ConcurrentHashSet<int>.Enumerator enumerator = set.GetEnumerator();

        enumerator.Dispose();
        enumerator.Dispose();
    }

    /// <summary>
    /// Verifies that <c>MoveNext</c> on a default-valued <see cref="ConcurrentHashSet{T}.Enumerator" /> reports
    /// end-of-sequence rather than throwing.
    /// </summary>
    [TestMethod]
    public void Enumerator_Default_MoveNext_ShouldReturnFalse()
    {
        ConcurrentHashSet<int>.Enumerator enumerator = default;

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <c>Current</c> on a default-valued <see cref="ConcurrentHashSet{T}.Enumerator" /> is the type
    /// default.
    /// </summary>
    [TestMethod]
    public void Enumerator_Default_Current_ShouldBeDefault()
    {
        ConcurrentHashSet<int>.Enumerator enumerator = default;

        Assert.AreEqual(0, enumerator.Current);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="System.Collections.IEnumerator.Current" /> on a default-valued
    /// <see cref="ConcurrentHashSet{T}.Enumerator" /> is the type default.
    /// </summary>
    [TestMethod]
    public void Enumerator_Default_NonGenericCurrent_ShouldBeDefault()
    {
        System.Collections.IEnumerator enumerator = default(ConcurrentHashSet<int>.Enumerator);

        Assert.AreEqual(0, enumerator.Current);
    }

    /// <summary>
    /// Verifies that <c>Reset</c> on a default-valued <see cref="ConcurrentHashSet{T}.Enumerator" /> is safe and that
    /// the enumerator still yields nothing afterward.
    /// </summary>
    [TestMethod]
    public void Enumerator_Default_Reset_ShouldBeSafe()
    {
        ConcurrentHashSet<int>.Enumerator enumerator = default;

        enumerator.Reset();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <c>Dispose</c> on a default-valued <see cref="ConcurrentHashSet{T}.Enumerator" /> is safe.
    /// </summary>
    [TestMethod]
    public void Enumerator_Default_Dispose_ShouldBeSafe()
    {
        ConcurrentHashSet<int>.Enumerator enumerator = default;

        enumerator.Dispose();
    }
}
