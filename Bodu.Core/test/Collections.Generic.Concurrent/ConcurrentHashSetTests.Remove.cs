// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Remove.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that removing an element that is present returns <see langword="true" /> and deletes the element.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementPresent_ShouldReturnTrueAndDeleteElement()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.IsTrue(set.Remove(2));
        Assert.AreEqual(2, set.Count);
        Assert.IsFalse(set.Contains(2));
    }

    /// <summary>
    /// Verifies that removing an element that is not present returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementAbsent_ShouldReturnFalse()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.IsFalse(set.Remove(99));
        Assert.AreEqual(3, set.Count);
    }

    /// <summary>
    /// Verifies that removing from an empty set returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.IsFalse(set.Remove(1));
    }

    /// <summary>
    /// Verifies that removing the head and a middle element of a colliding bucket chain leaves the remaining elements
    /// reachable.
    /// </summary>
    [TestMethod]
    public void Remove_WhenChainHasMultipleElements_ShouldKeepRemainingElementsReachable()
    {
        var set = new ConcurrentHashSet<int>(new[] { 10, 20, 30, 40, 50 });

        Assert.IsTrue(set.Remove(10));
        Assert.IsTrue(set.Remove(30));
        Assert.IsTrue(set.Remove(50));

        AssertContainsExactly(set, 20, 40);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Remove" /> honors a custom comparer when locating the element.
    /// </summary>
    [TestMethod]
    public void Remove_WhenComparerTreatsElementsEqual_ShouldRemoveTheMatchingElement()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add("Hello");

        Assert.IsTrue(set.Remove("HELLO"));
        Assert.AreEqual(0, set.Count);
    }
}
