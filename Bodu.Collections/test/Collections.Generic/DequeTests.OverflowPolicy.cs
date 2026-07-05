// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.OverflowPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{

    /// <summary>
    /// Verifies that <see cref="Deque{T}.OverflowPolicy" /> defaults to <see cref="DequeOverflowPolicy.Reject" /> on
    /// every constructor overload.
    /// </summary>
    [TestMethod]
    public void OverflowPolicy_WhenDefaultUsed_ShouldBeReject()
    {
        Assert.AreEqual(DequeOverflowPolicy.Reject, new Deque<int>().OverflowPolicy);
        Assert.AreEqual(DequeOverflowPolicy.Reject, new Deque<int>(8).OverflowPolicy);
        Assert.AreEqual(DequeOverflowPolicy.Reject, new Deque<int>(8, allowGrow: false).OverflowPolicy);
        Assert.AreEqual(DequeOverflowPolicy.Reject, new Deque<int>([1, 2, 3]).OverflowPolicy);
        Assert.AreEqual(DequeOverflowPolicy.Reject, new Deque<int>([1, 2, 3], capacity: 8).OverflowPolicy);
        Assert.AreEqual(DequeOverflowPolicy.Reject, new Deque<int>([1, 2, 3], capacity: 8, allowGrow: false).OverflowPolicy);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.OverflowPolicy" /> reflects a defined value assigned to it.
    /// </summary>
    /// <param name="policy">The policy value to assign.</param>
    [TestMethod]
    [DataRow(DequeOverflowPolicy.Reject)]
    [DataRow(DequeOverflowPolicy.EvictOpposite)]
    public void OverflowPolicy_WhenSetToDefinedValue_ShouldReflectInProperty(DequeOverflowPolicy policy)
    {
        var deque = new Deque<int>(4, allowGrow: false)
        {
            OverflowPolicy = policy,
        };

        Assert.AreEqual(policy, deque.OverflowPolicy);
    }

    /// <summary>
    /// Verifies that assigning an undefined <see cref="DequeOverflowPolicy" /> value to
    /// <see cref="Deque{T}.OverflowPolicy" /> throws <see cref="ArgumentOutOfRangeException" /> and leaves the
    /// previous value in place.
    /// </summary>
    [TestMethod]
    public void OverflowPolicy_WhenSetToUndefinedValue_ShouldThrowArgumentOutOfRangeException()
    {
        var deque = new Deque<int>(4, allowGrow: false);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            deque.OverflowPolicy = (DequeOverflowPolicy)99;
        });

        Assert.AreEqual("value", ex.ParamName);
        Assert.AreEqual(DequeOverflowPolicy.Reject, deque.OverflowPolicy);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddFirst(T)" /> on a full, fixed-capacity deque still throws
    /// <see cref="InvalidOperationException" /> under the default <see cref="DequeOverflowPolicy.Reject" /> policy.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenFullAndPolicyReject_ShouldThrowInvalidOperationException()
    {
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: false);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            deque.AddFirst(0);
        });

        Assert.AreEqual(2, deque.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddLast(T)" /> on a full, fixed-capacity deque still throws
    /// <see cref="InvalidOperationException" /> under the default <see cref="DequeOverflowPolicy.Reject" /> policy.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenFullAndPolicyReject_ShouldThrowInvalidOperationException()
    {
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: false);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            deque.AddLast(3);
        });

        Assert.AreEqual(2, deque.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddFirst(T)" /> on a full, fixed-capacity deque still returns
    /// <see langword="false" /> without modifying state under the default <see cref="DequeOverflowPolicy.Reject" />
    /// policy.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenFullAndPolicyReject_ShouldReturnFalse()
    {
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: false);

        Assert.IsFalse(deque.TryAddFirst(0));
        Assert.AreEqual(2, deque.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddLast(T)" /> on a full, fixed-capacity deque still returns
    /// <see langword="false" /> without modifying state under the default <see cref="DequeOverflowPolicy.Reject" />
    /// policy.
    /// </summary>
    [TestMethod]
    public void TryAddLast_WhenFullAndPolicyReject_ShouldReturnFalse()
    {
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: false);

        Assert.IsFalse(deque.TryAddLast(3));
        Assert.AreEqual(2, deque.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddFirst(T)" /> on a full, fixed-capacity deque with
    /// <see cref="DequeOverflowPolicy.EvictOpposite" /> discards the tail element and stores the new item at the
    /// head, keeping the count at capacity.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenFullAndPolicyEvictOpposite_ShouldEvictLastAndAddAtFront()
    {
        var deque = new Deque<int>([1, 2, 3, 4], capacity: 4, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };

        deque.AddFirst(0);

        Assert.AreEqual(deque.Capacity, deque.Count);
        Assert.AreEqual(0, deque.PeekFirst());
        Assert.AreEqual(3, deque.PeekLast());
        Assert.IsFalse(deque.Contains(4));
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddLast(T)" /> on a full, fixed-capacity deque with
    /// <see cref="DequeOverflowPolicy.EvictOpposite" /> discards the head element and stores the new item at the
    /// tail, keeping the count at capacity.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenFullAndPolicyEvictOpposite_ShouldEvictFirstAndAddAtBack()
    {
        var deque = new Deque<int>([1, 2, 3, 4], capacity: 4, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };

        deque.AddLast(5);

        Assert.AreEqual(deque.Capacity, deque.Count);
        Assert.AreEqual(2, deque.PeekFirst());
        Assert.AreEqual(5, deque.PeekLast());
        Assert.IsFalse(deque.Contains(1));
        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddFirst(T)" /> on a full, fixed-capacity deque with
    /// <see cref="DequeOverflowPolicy.EvictOpposite" /> returns <see langword="true" /> and evicts the tail element.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenFullAndPolicyEvictOpposite_ShouldReturnTrueAndEvictLast()
    {
        var deque = new Deque<int>([1, 2, 3], capacity: 3, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };

        Assert.IsTrue(deque.TryAddFirst(0));
        Assert.AreEqual(deque.Capacity, deque.Count);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddLast(T)" /> on a full, fixed-capacity deque with
    /// <see cref="DequeOverflowPolicy.EvictOpposite" /> returns <see langword="true" /> and evicts the head element.
    /// </summary>
    [TestMethod]
    public void TryAddLast_WhenFullAndPolicyEvictOpposite_ShouldReturnTrueAndEvictFirst()
    {
        var deque = new Deque<int>([1, 2, 3], capacity: 3, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };

        Assert.IsTrue(deque.TryAddLast(4));
        Assert.AreEqual(deque.Capacity, deque.Count);
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that enumeration remains coherent front-to-back after a mix of head- and tail-side evictions under
    /// <see cref="DequeOverflowPolicy.EvictOpposite" />, matching <see cref="RingBackedCollection{T}.ToArray" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenMixedEvictionsOccurred_ShouldEnumerateFrontToBack()
    {
        var deque = new Deque<int>([1, 2, 3, 4], capacity: 4, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };

        deque.AddLast(5);   // evicts 1 → 2, 3, 4, 5
        deque.AddFirst(0);  // evicts 5 → 0, 2, 3, 4
        deque.AddLast(6);   // evicts 0 → 2, 3, 4, 6

        var enumerated = new List<int>();
        foreach (int item in deque)
            enumerated.Add(item);

        CollectionAssert.AreEqual(new[] { 2, 3, 4, 6 }, enumerated);
        CollectionAssert.AreEqual(new[] { 2, 3, 4, 6 }, deque.ToArray());
        Assert.AreEqual(deque.Capacity, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddFirst(T)" /> on a full, growable deque grows instead of evicting even
    /// when <see cref="DequeOverflowPolicy.EvictOpposite" /> is configured — growth always wins over the policy.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenAllowGrowTrueAndPolicyEvictOpposite_ShouldGrowWithoutEvicting()
    {
        bool anyEventFired = false;
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: true)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };
        deque.ItemEvicting += _ => anyEventFired = true;
        deque.ItemEvicted += _ => anyEventFired = true;

        deque.AddFirst(0);

        Assert.AreEqual(3, deque.Count);
        Assert.IsGreaterThanOrEqualTo(3, deque.Capacity);
        Assert.IsFalse(anyEventFired);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddLast(T)" /> on a full, growable deque grows instead of evicting even when
    /// <see cref="DequeOverflowPolicy.EvictOpposite" /> is configured — growth always wins over the policy.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenAllowGrowTrueAndPolicyEvictOpposite_ShouldGrowWithoutEvicting()
    {
        bool anyEventFired = false;
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: true)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };
        deque.ItemEvicting += _ => anyEventFired = true;
        deque.ItemEvicted += _ => anyEventFired = true;

        deque.AddLast(3);

        Assert.AreEqual(3, deque.Count);
        Assert.IsGreaterThanOrEqualTo(3, deque.Capacity);
        Assert.IsFalse(anyEventFired);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that an eviction triggered by <see cref="Deque{T}.AddFirst(T)" /> raises
    /// <see cref="Deque{T}.ItemEvicting" /> before <see cref="Deque{T}.ItemEvicted" />, both carrying the evicted
    /// tail element.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenEvicting_ShouldRaiseItemEvictingThenItemEvictedWithEvictedItem()
    {
        var sequence = new List<string>();
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };
        deque.ItemEvicting += item => sequence.Add($"Evicting:{item}");
        deque.ItemEvicted += item => sequence.Add($"Evicted:{item}");

        deque.AddFirst(0); // evicts tail element 2

        CollectionAssert.AreEqual(new[] { "Evicting:2", "Evicted:2" }, sequence);
        CollectionAssert.AreEqual(new[] { 0, 1 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that an eviction triggered by <see cref="Deque{T}.AddLast(T)" /> raises
    /// <see cref="Deque{T}.ItemEvicting" /> before <see cref="Deque{T}.ItemEvicted" />, both carrying the evicted
    /// head element.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenEvicting_ShouldRaiseItemEvictingThenItemEvictedWithEvictedItem()
    {
        var sequence = new List<string>();
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };
        deque.ItemEvicting += item => sequence.Add($"Evicting:{item}");
        deque.ItemEvicted += item => sequence.Add($"Evicted:{item}");

        deque.AddLast(3); // evicts head element 1

        CollectionAssert.AreEqual(new[] { "Evicting:1", "Evicted:1" }, sequence);
        CollectionAssert.AreEqual(new[] { 2, 3 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that an eviction triggered by <see cref="Deque{T}.TryAddLast(T)" /> raises the same
    /// <see cref="Deque{T}.ItemEvicting" /> / <see cref="Deque{T}.ItemEvicted" /> pair as the throwing variant.
    /// </summary>
    [TestMethod]
    public void TryAddLast_WhenEvicting_ShouldRaiseEvictionEvents()
    {
        var sequence = new List<string>();
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };
        deque.ItemEvicting += item => sequence.Add($"Evicting:{item}");
        deque.ItemEvicted += item => sequence.Add($"Evicted:{item}");

        Assert.IsTrue(deque.TryAddLast(3));
        CollectionAssert.AreEqual(new[] { "Evicting:1", "Evicted:1" }, sequence);
    }

    /// <summary>
    /// Verifies that an exception thrown from an <see cref="Deque{T}.ItemEvicting" /> handler vetoes the eviction in
    /// place — the exception propagates, no element is removed or added, and <see cref="Deque{T}.ItemEvicted" /> is
    /// not raised.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenItemEvictingHandlerThrows_ShouldVetoEvictionAndLeaveStateUnchanged()
    {
        bool evictedFired = false;
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: false)
        {
            OverflowPolicy = DequeOverflowPolicy.EvictOpposite,
        };
        deque.ItemEvicting += _ => throw new NotSupportedException("Eviction vetoed");
        deque.ItemEvicted += _ => evictedFired = true;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            deque.AddLast(3);
        });

        Assert.IsFalse(evictedFired);
        Assert.AreEqual(2, deque.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that no eviction event is raised when a full, fixed-capacity deque rejects an add under the default
    /// <see cref="DequeOverflowPolicy.Reject" /> policy.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenFullAndPolicyReject_ShouldNotRaiseEvictionEvents()
    {
        bool anyEventFired = false;
        var deque = new Deque<int>([1, 2], capacity: 2, allowGrow: false);
        deque.ItemEvicting += _ => anyEventFired = true;
        deque.ItemEvicted += _ => anyEventFired = true;

        Assert.IsFalse(deque.TryAddLast(3));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            deque.AddLast(3);
        });

        Assert.IsFalse(anyEventFired);
    }

}
