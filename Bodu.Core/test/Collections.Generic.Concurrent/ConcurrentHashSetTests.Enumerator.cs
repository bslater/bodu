// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3, 4, 5 });

        var observed = new List<int>();
        foreach (int item in set)
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
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
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
        var set = new ConcurrentHashSet<int>(new[] { 10, 20, 30 });
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
}
