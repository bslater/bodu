// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.IEnumerable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that the generic <see cref="IEnumerable{T}.GetEnumerator" /> explicit implementation yields every
    /// element.
    /// </summary>
    [TestMethod]
    public void GenericGetEnumerator_WhenAccessedThroughInterface_ShouldYieldEveryElement()
    {
        IEnumerable<int> set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        var observed = new List<int>();
        foreach (int item in set)
            observed.Add(item);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, observed);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> explicit implementation yields every
    /// element.
    /// </summary>
    [TestMethod]
    public void NonGenericGetEnumerator_WhenAccessedThroughInterface_ShouldYieldEveryElement()
    {
        IEnumerable set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        var observed = new List<int>();
        foreach (object? item in set)
            observed.Add((int)item!);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, observed);
    }

    /// <summary>
    /// Verifies that the non-generic enumerator of an empty set reports end-of-sequence on the first
    /// <see cref="IEnumerator.MoveNext" />.
    /// </summary>
    [TestMethod]
    public void NonGenericGetEnumerator_WhenSetIsEmpty_ShouldBeImmediatelyExhausted()
    {
        IEnumerable set = new ConcurrentHashSet<int>();

        IEnumerator enumerator = set.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the set composes with LINQ operators through its <see cref="IEnumerable{T}" /> implementation.
    /// </summary>
    [TestMethod]
    public void Enumerable_WhenUsedWithLinq_ShouldProjectElements()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3, 4 });

        Assert.AreEqual(10, set.Sum());
        Assert.AreEqual(2, set.Count(value => value % 2 == 0));
    }
}
