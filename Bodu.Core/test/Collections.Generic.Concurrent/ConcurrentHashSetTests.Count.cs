// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Count.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Count" /> is zero for a newly created set.
    /// </summary>
    [TestMethod]
    public void Count_WhenSetIsEmpty_ShouldBeZero()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.AreEqual(0, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Count" /> reflects each distinct element added.
    /// </summary>
    [TestMethod]
    public void Count_WhenElementsAdded_ShouldReflectDistinctElementCount()
    {
        var set = new ConcurrentHashSet<int>();

        set.Add(1);
        set.Add(2);
        set.Add(2);
        set.Add(3);

        Assert.AreEqual(3, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Count" /> decreases as elements are removed.
    /// </summary>
    [TestMethod]
    public void Count_WhenElementsRemoved_ShouldDecrease()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.Remove(2);

        Assert.AreEqual(2, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Count" /> agrees with the length of the
    /// <see cref="ConcurrentHashSet{T}.ToArray" /> snapshot after the table has grown.
    /// </summary>
    [TestMethod]
    public void Count_WhenTableHasGrown_ShouldAgreeWithSnapshotLength()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 2000));

        Assert.AreEqual(set.ToArray().Length, set.Count);
        Assert.AreEqual(2000, set.Count);
    }
}
