// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

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
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

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
        var set = new ConcurrentHashSet<int>([10, 20, 30, 40, 50]);

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

    /// <summary>
    /// Verifies that removing an element and re-adding it yields exactly one live copy - the logically deleted node
    /// must never be resurrected alongside the new one.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementIsReAdded_ShouldYieldExactlyOneLiveCopy()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.IsTrue(set.Add(5));
        Assert.IsTrue(set.Remove(5));
        Assert.IsTrue(set.Add(5));

        Assert.AreEqual(1, set.Count);
        AssertContainsExactly(set, 5);
    }

    /// <summary>
    /// Verifies that interleaved removes and adds through a fully colliding equal-key run keep every surviving
    /// element reachable - unlinking a marked node must never detach its unmarked successors.
    /// </summary>
    [TestMethod]
    public void Remove_WhenInterleavedWithAddsOnCollidingRun_ShouldKeepSurvivorsReachable()
    {
        var set = new ConcurrentHashSet<int>(new ConstantHashComparer());
        for (int i = 0; i < 20; i++)
            set.Add(i);

        for (int i = 0; i < 20; i += 2)
            Assert.IsTrue(set.Remove(i), $"Remove of colliding key {i} must succeed.");

        for (int i = 100; i < 110; i++)
            Assert.IsTrue(set.Add(i), $"Add of colliding key {i} must succeed after removals.");

        int[] expected = Enumerable.Range(0, 20).Where(value => value % 2 == 1)
            .Concat(Enumerable.Range(100, 10))
            .ToArray();
        AssertContainsExactly(set, expected);
    }

    /// <summary>
    /// Verifies that when the comparer inserts a new predecessor in front of the element being removed, from inside
    /// the traversal that has just matched it, the removal still succeeds, its best-effort unlink leaves the node
    /// logically deleted in the list, later lookups skip that node through its marker, and a later insertion unlinks
    /// it.
    /// </summary>
    [TestMethod]
    public void Remove_WhenComparerInsertsPredecessorReentrantly_ShouldRemoveAndLeaveMarkedNodeForLaterTraversals()
    {
        var comparer = new ReentrantComparer();
        var set = new ConcurrentHashSet<int>(comparer);
        set.Add(2);
        set.Add(3);
        comparer.OnEquals = () => Assert.IsTrue(set.Add(1), "The re-entrant insert between 2 and 3 must succeed.");

        Assert.IsTrue(set.Remove(3));

        Assert.IsNull(comparer.OnEquals, "The callback must have run from inside the removal's traversal.");
        Assert.AreEqual(2, set.Count);
        Assert.IsFalse(set.Contains(3), "A lookup must skip the logically deleted node through its marker.");
        Assert.IsTrue(set.Contains(1));
        Assert.IsTrue(set.Add(3), "A later insertion must unlink the marked node and append the fresh element.");
        Assert.IsTrue(set.Contains(3));
        AssertContainsExactly(set, 2, 1, 3);
    }

    /// <summary>
    /// Verifies that when the comparer removes the very element being removed, from inside the traversal that has
    /// just matched it, the outer removal observes the marker the inner one installed, retries, and reports the
    /// element absent, so the element is removed exactly once.
    /// </summary>
    [TestMethod]
    public void Remove_WhenComparerRemovesSameElementReentrantly_ShouldRemoveExactlyOnce()
    {
        var comparer = new ReentrantComparer();
        var set = new ConcurrentHashSet<int>(comparer);
        set.Add(2);
        set.Add(3);
        var innerRemoved = false;
        comparer.OnEquals = () => innerRemoved = set.Remove(3);

        var outerRemoved = set.Remove(3);

        Assert.IsTrue(innerRemoved, "The re-entrant removal must have claimed the element.");
        Assert.IsFalse(outerRemoved, "The outer removal must observe the marker, retry, and find the element gone.");
        Assert.AreEqual(1, set.Count);
        Assert.IsFalse(set.Contains(3));
        AssertContainsExactly(set, 2);
    }
}
