// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.SetComparisons.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsSubsetOf" /> reports the subset relationship correctly,
    /// including the empty-set and equal-set cases.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_ShouldReportSubsetRelationship()
    {
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1, 2 }).IsSubsetOf(new[] { 1, 2, 3 }));
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1, 2 }).IsSubsetOf(new[] { 1, 2 }));
        Assert.IsTrue(new ConcurrentHashSet<int>().IsSubsetOf(new[] { 1 }));
        Assert.IsFalse(new ConcurrentHashSet<int>(new[] { 1, 2, 3 }).IsSubsetOf(new[] { 1, 2 }));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsSupersetOf" /> reports the superset relationship correctly.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_ShouldReportSupersetRelationship()
    {
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1, 2, 3 }).IsSupersetOf(new[] { 1, 2 }));
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1, 2 }).IsSupersetOf(new[] { 1, 2 }));
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1 }).IsSupersetOf(Array.Empty<int>()));
        Assert.IsFalse(new ConcurrentHashSet<int>(new[] { 1, 2 }).IsSupersetOf(new[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsProperSubsetOf" /> requires a strict subset, rejecting equal
    /// sets.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_ShouldRequireStrictSubset()
    {
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1, 2 }).IsProperSubsetOf(new[] { 1, 2, 3 }));
        Assert.IsFalse(new ConcurrentHashSet<int>(new[] { 1, 2 }).IsProperSubsetOf(new[] { 1, 2 }));
        Assert.IsFalse(new ConcurrentHashSet<int>(new[] { 1, 2, 3 }).IsProperSubsetOf(new[] { 1, 2 }));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsProperSupersetOf" /> requires a strict superset, rejecting equal
    /// sets.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_ShouldRequireStrictSuperset()
    {
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1, 2, 3 }).IsProperSupersetOf(new[] { 1, 2 }));
        Assert.IsFalse(new ConcurrentHashSet<int>(new[] { 1, 2 }).IsProperSupersetOf(new[] { 1, 2 }));
        Assert.IsFalse(new ConcurrentHashSet<int>(new[] { 1, 2 }).IsProperSupersetOf(new[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Overlaps" /> detects whether the collections share any element.
    /// </summary>
    [TestMethod]
    public void Overlaps_ShouldReportSharedElements()
    {
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1, 2, 3 }).Overlaps(new[] { 3, 4, 5 }));
        Assert.IsFalse(new ConcurrentHashSet<int>(new[] { 1, 2, 3 }).Overlaps(new[] { 4, 5, 6 }));
        Assert.IsFalse(new ConcurrentHashSet<int>().Overlaps(new[] { 1 }));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SetEquals" /> compares element membership independently of order
    /// and duplicates.
    /// </summary>
    [TestMethod]
    public void SetEquals_ShouldCompareMembershipIgnoringOrderAndDuplicates()
    {
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1, 2, 3 }).SetEquals(new[] { 3, 2, 1 }));
        Assert.IsTrue(new ConcurrentHashSet<int>(new[] { 1, 2, 3 }).SetEquals(new[] { 1, 1, 2, 3, 3 }));
        Assert.IsFalse(new ConcurrentHashSet<int>(new[] { 1, 2, 3 }).SetEquals(new[] { 1, 2 }));
        Assert.IsFalse(new ConcurrentHashSet<int>(new[] { 1, 2 }).SetEquals(new[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies that every set-comparison predicate throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void SetComparisons_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() => set.IsSubsetOf(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => set.IsSupersetOf(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => set.IsProperSubsetOf(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => set.IsProperSupersetOf(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => set.Overlaps(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => set.SetEquals(null!));
    }

    /// <summary>
    /// Verifies that the set-comparison predicates report the expected reflexive results when a set is compared
    /// against itself.
    /// </summary>
    [TestMethod]
    public void SetComparisons_WhenComparedWithSelf_ShouldReportReflexiveResults()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.IsTrue(set.IsSubsetOf(set));
        Assert.IsTrue(set.IsSupersetOf(set));
        Assert.IsTrue(set.SetEquals(set));
        Assert.IsTrue(set.Overlaps(set));
        Assert.IsFalse(set.IsProperSubsetOf(set));
        Assert.IsFalse(set.IsProperSupersetOf(set));
    }

    /// <summary>
    /// Verifies that an empty set is a subset of every collection, is never a proper subset of an empty collection,
    /// and overlaps nothing.
    /// </summary>
    [TestMethod]
    public void SetComparisons_WhenSetIsEmpty_ShouldReportEmptySetSemantics()
    {
        var empty = new ConcurrentHashSet<int>();

        Assert.IsTrue(empty.IsSubsetOf(new[] { 1, 2 }));
        Assert.IsTrue(empty.IsSubsetOf(Array.Empty<int>()));
        Assert.IsTrue(empty.IsProperSubsetOf(new[] { 1 }));
        Assert.IsFalse(empty.IsProperSubsetOf(Array.Empty<int>()));
        Assert.IsFalse(empty.Overlaps(new[] { 1, 2 }));
        Assert.IsTrue(empty.SetEquals(Array.Empty<int>()));
    }
}
