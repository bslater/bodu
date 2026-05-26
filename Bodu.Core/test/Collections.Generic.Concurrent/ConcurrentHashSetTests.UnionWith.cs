// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.UnionWith.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.UnionWith" /> adds every element from the other collection.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherHasNewElements_ShouldAddThemAll()
    {
        var set = new ConcurrentHashSet<int>([1, 2]);

        set.UnionWith([2, 3, 4]);

        AssertContainsExactly(set, 1, 2, 3, 4);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.UnionWith" /> with an empty collection leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherIsEmpty_ShouldLeaveSetUnchanged()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.UnionWith(Array.Empty<int>());

        AssertContainsExactly(set, 1, 2, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.UnionWith" /> applied to the set itself leaves it unchanged.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherIsSameInstance_ShouldLeaveSetUnchanged()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.UnionWith(set);

        AssertContainsExactly(set, 1, 2, 3);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.UnionWith" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.UnionWith(null!);
        });
    }
}
