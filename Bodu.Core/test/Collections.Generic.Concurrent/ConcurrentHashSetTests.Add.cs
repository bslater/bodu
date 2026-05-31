// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Add.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that adding a new element returns <see langword="true" /> and stores the element.
    /// </summary>
    [TestMethod]
    public void Add_WhenElementIsNew_ShouldReturnTrueAndStoreElement()
    {
        var set = new ConcurrentHashSet<string>();

        Assert.IsTrue(set.Add("a"));
        Assert.AreEqual(1, set.Count);
        Assert.IsTrue(set.Contains("a"));
    }

    /// <summary>
    /// Verifies that adding an element equal to one already present returns <see langword="false" /> and does not
    /// change the element count.
    /// </summary>
    [TestMethod]
    public void Add_WhenElementAlreadyPresent_ShouldReturnFalseAndLeaveCountUnchanged()
    {
        var set = new ConcurrentHashSet<string>();
        Assert.IsTrue(set.Add("a"));

        Assert.IsFalse(set.Add("a"));
        Assert.AreEqual(1, set.Count);
    }

    /// <summary>
    /// Verifies that adding several distinct elements stores each one.
    /// </summary>
    [TestMethod]
    public void Add_WhenElementsAreDistinct_ShouldStoreEveryElement()
    {
        var set = new ConcurrentHashSet<int>();

        for (var i = 0; i < 100; i++)
            Assert.IsTrue(set.Add(i));

        Assert.AreEqual(100, set.Count);
        AssertContainsExactly(set, Enumerable.Range(0, 100).ToArray());
    }

    /// <summary>
    /// Verifies that adding enough distinct elements to force several internal table resizes preserves every element.
    /// </summary>
    [TestMethod]
    public void Add_WhenManyElementsForceResize_ShouldPreserveEveryElement()
    {
        var set = new ConcurrentHashSet<int>(capacity: 4);

        for (var i = 0; i < 5000; i++)
            Assert.IsTrue(set.Add(i));

        Assert.AreEqual(5000, set.Count);
        for (var i = 0; i < 5000; i++)
            Assert.IsTrue(set.Contains(i), $"Element {i} was lost across a resize.");
    }

    /// <summary>
    /// Verifies that re-adding an element after it was removed returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenElementReaddedAfterRemoval_ShouldReturnTrue()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(42);
        set.Remove(42);

        Assert.IsTrue(set.Add(42));
        Assert.IsTrue(set.Contains(42));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Add" /> honors a custom comparer when detecting duplicates.
    /// </summary>
    [TestMethod]
    public void Add_WhenComparerTreatsElementsEqual_ShouldRejectTheDuplicate()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(set.Add("Hello"));
        Assert.IsFalse(set.Add("HELLO"));
        Assert.AreEqual(1, set.Count);
    }
}
