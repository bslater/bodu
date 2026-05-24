// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.SymmetricExceptWith.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SymmetricExceptWith" /> keeps the elements that appear in exactly
    /// one of the two collections.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenCollectionsOverlap_ShouldKeepElementsInExactlyOne()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        set.SymmetricExceptWith(new[] { 2, 3, 4, 5 });

        AssertContainsExactly(set, 1, 4, 5);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SymmetricExceptWith" /> with an empty collection leaves the set
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherIsEmpty_ShouldLeaveSetUnchanged()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        set.SymmetricExceptWith(Array.Empty<int>());

        AssertContainsExactly(set, 1, 2, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SymmetricExceptWith" /> with a disjoint collection adds every
    /// element from the other collection.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenCollectionsAreDisjoint_ShouldAddEveryOtherElement()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2 });

        set.SymmetricExceptWith(new[] { 3, 4 });

        AssertContainsExactly(set, 1, 2, 3, 4);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SymmetricExceptWith" /> applied to the set itself empties it.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherIsSameInstance_ShouldEmptyTheSet()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        set.SymmetricExceptWith(set);

        Assert.AreEqual(0, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SymmetricExceptWith" /> throws <see cref="ArgumentNullException" />
    /// for a <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.SymmetricExceptWith(null!);
        });
    }
}
