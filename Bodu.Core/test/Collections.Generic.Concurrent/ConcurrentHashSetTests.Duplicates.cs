// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Duplicates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.UnionWith" /> adds each distinct element of <c>other</c> once,
    /// even when <c>other</c> repeats elements.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherHasDuplicates_ShouldAddEachDistinctElementOnce()
    {
        var set = new ConcurrentHashSet<int>([1]);

        set.UnionWith([2, 2, 3, 3, 3]);

        AssertContainsExactly(set, 1, 2, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IntersectWith" /> treats a duplicate-laden <c>other</c> as the set
    /// of its distinct elements.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherHasDuplicates_ShouldTreatOtherAsSet()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3, 4]);

        set.IntersectWith([2, 2, 3, 3]);

        AssertContainsExactly(set, 2, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ExceptWith" /> removes each distinct element of a
    /// duplicate-laden <c>other</c>.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherHasDuplicates_ShouldRemoveEachDistinctElement()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3, 4]);

        set.ExceptWith([2, 2, 3, 3]);

        AssertContainsExactly(set, 1, 4);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SymmetricExceptWith" /> toggles each distinct element of a
    /// duplicate-laden <c>other</c> exactly once.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherHasDuplicates_ShouldTreatOtherAsSet()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.SymmetricExceptWith([2, 2, 4, 4]);

        AssertContainsExactly(set, 1, 3, 4);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsProperSubsetOf" /> compares against the distinct element count
    /// of <c>other</c>, so a duplicate-inflated <c>other</c> of equal membership is not reported as a strict superset.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_WhenOtherHasDuplicates_ShouldUseDistinctCount()
    {
        var set = new ConcurrentHashSet<int>([1, 2]);

        Assert.IsFalse(set.IsProperSubsetOf([1, 2, 2, 2, 2]));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsProperSupersetOf" /> compares against the distinct element
    /// count of <c>other</c>, so a duplicate-laden <c>other</c> is judged by its membership rather than its length.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_WhenOtherHasDuplicates_ShouldUseDistinctCount()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        Assert.IsTrue(set.IsProperSupersetOf([1, 1, 1]));
    }
}
