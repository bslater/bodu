// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.FaultingComparers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that an exception thrown by the comparer's <c>GetHashCode</c> during <see cref="ConcurrentHashSet{T}.Add" />
    /// propagates to the caller unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenComparerGetHashCodeThrows_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>(new ThrowingHashCodeComparer(faultingValue: 42));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Add(42);
        });
    }

    /// <summary>
    /// Verifies that an exception thrown by the comparer's <c>GetHashCode</c> during
    /// <see cref="ConcurrentHashSet{T}.Contains" /> propagates to the caller unchanged.
    /// </summary>
    [TestMethod]
    public void Contains_WhenComparerGetHashCodeThrows_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>(new ThrowingHashCodeComparer(faultingValue: 42));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Contains(42);
        });
    }

    /// <summary>
    /// Verifies that an exception thrown by the comparer's <c>GetHashCode</c> during
    /// <see cref="ConcurrentHashSet{T}.Remove" /> propagates to the caller unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenComparerGetHashCodeThrows_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>(new ThrowingHashCodeComparer(faultingValue: 42));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Remove(42);
        });
    }

    /// <summary>
    /// Verifies that an exception thrown by the comparer's <c>Equals</c> during a bucket-chain walk in
    /// <see cref="ConcurrentHashSet{T}.Add" /> propagates to the caller unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenComparerEqualsThrows_ShouldPropagateException()
    {
        var comparer = new ThrowingEqualsComparer();
        var set = new ConcurrentHashSet<int>(comparer);
        set.Add(1);

        comparer.FaultArmed = true;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Add(2);
        });
    }

    /// <summary>
    /// Verifies that an exception thrown by the comparer's <c>Equals</c> during a bucket-chain walk in
    /// <see cref="ConcurrentHashSet{T}.Contains" /> propagates to the caller unchanged.
    /// </summary>
    [TestMethod]
    public void Contains_WhenComparerEqualsThrows_ShouldPropagateException()
    {
        var comparer = new ThrowingEqualsComparer();
        var set = new ConcurrentHashSet<int>(comparer);
        set.Add(1);

        comparer.FaultArmed = true;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Contains(2);
        });
    }

    /// <summary>
    /// Verifies that an exception thrown by the comparer's <c>Equals</c> during a bucket-chain walk in
    /// <see cref="ConcurrentHashSet{T}.Remove" /> propagates to the caller unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenComparerEqualsThrows_ShouldPropagateException()
    {
        var comparer = new ThrowingEqualsComparer();
        var set = new ConcurrentHashSet<int>(comparer);
        set.Add(1);

        comparer.FaultArmed = true;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Remove(2);
        });
    }

    /// <summary>
    /// Verifies that an <see cref="ConcurrentHashSet{T}.Add" /> aborted by a faulting comparer hash leaves the element
    /// out of the set and the element count unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenComparerGetHashCodeThrows_ShouldNotChangeCount()
    {
        var set = new ConcurrentHashSet<int>(new ThrowingHashCodeComparer(faultingValue: 42));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Add(42);
        });

        Assert.AreEqual(0, set.Count, "A faulted Add must not change the element count.");
    }

    /// <summary>
    /// Verifies that an <see cref="ConcurrentHashSet{T}.Add" /> aborted by a faulting comparer <c>Equals</c> releases
    /// its stripe lock, so a later operation on the same stripe does not deadlock.
    /// </summary>
    [TestMethod]
    public void Add_WhenComparerEqualsThrows_ShouldReleaseStripeLock()
    {
        var comparer = new ThrowingEqualsComparer();
        var set = new ConcurrentHashSet<int>(comparer);
        set.Add(1);

        comparer.FaultArmed = true;
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Add(2);
        });

        comparer.FaultArmed = false;
        bool completed = Task.Run(() => set.Add(3)).Wait(TimeSpan.FromSeconds(5));

        Assert.IsTrue(completed, "A faulted Add must release its stripe lock so later operations do not deadlock.");
        Assert.IsTrue(set.Contains(3));
    }

    /// <summary>
    /// Verifies that an <see cref="ConcurrentHashSet{T}.Add" /> aborted by a faulting comparer <c>Equals</c> leaves
    /// every element already present in the set intact.
    /// </summary>
    [TestMethod]
    public void Add_WhenComparerEqualsThrows_ShouldPreserveExistingElements()
    {
        var comparer = new ThrowingEqualsComparer();
        var set = new ConcurrentHashSet<int>(comparer);
        set.Add(1);
        set.Add(2);
        set.Add(3);

        comparer.FaultArmed = true;
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Add(4);
        });

        comparer.FaultArmed = false;
        Assert.AreEqual(3, set.Count, "The faulted Add must not change the element count.");
        Assert.IsTrue(set.Contains(1));
        Assert.IsTrue(set.Contains(2));
        Assert.IsTrue(set.Contains(3));
        Assert.IsFalse(set.Contains(4), "The element whose Add faulted must not be present.");
    }
}
