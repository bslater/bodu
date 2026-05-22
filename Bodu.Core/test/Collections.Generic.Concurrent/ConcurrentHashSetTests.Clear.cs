// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Clear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Clear" /> removes every element.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSetHasElements_ShouldRemoveEveryElement()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3, 4 });

        set.Clear();

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
        Assert.IsFalse(set.Contains(1));
    }

    /// <summary>
    /// Verifies that calling <see cref="ConcurrentHashSet{T}.Clear" /> on an empty set is a safe no-op.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSetIsEmpty_ShouldRemainEmpty()
    {
        var set = new ConcurrentHashSet<int>();

        set.Clear();

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
    }

    /// <summary>
    /// Verifies that the set is fully usable after being cleared.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByAdd_ShouldAcceptNewElements()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        set.Clear();

        Assert.IsTrue(set.Add(99));
        Assert.AreEqual(1, set.Count);
        Assert.IsTrue(set.Contains(99));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Clear" /> empties a large set whose table has grown.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSetIsLarge_ShouldRemoveEveryElement()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 5000));

        set.Clear();

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
    }
}
