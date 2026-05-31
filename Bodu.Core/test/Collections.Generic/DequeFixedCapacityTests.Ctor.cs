// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeFixedCapacityTests.Ctor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeFixedCapacityTests
{

    /// <summary>
    /// Verifies that the (capacity, allowGrow=false) constructor reports the requested capacity exactly.
    /// </summary>
    /// <param name="capacity">The requested capacity.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(16)]
    public void Ctor_WhenAllowGrowFalse_ShouldUseExactCapacity(int capacity)
    {
        var deque = new Deque<int>(capacity, allowGrow: false);
        Assert.AreEqual(capacity, deque.Capacity);
        Assert.IsFalse(deque.AllowGrow);
    }

    /// <summary>
    /// Verifies that the (collection, capacity, allowGrow=false) constructor throws when the source exceeds capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAllowGrowFalseAndCollectionExceedsCapacity_ShouldThrowExactly()
    {
        var source = new[] { 1, 2, 3, 4 };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Deque<int>(source, capacity: 2, allowGrow: false);
        });
    }

    /// <summary>
    /// Verifies that the (collection, capacity, allowGrow=false) constructor adopts elements when they fit.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAllowGrowFalseAndCollectionFits_ShouldAdoptElements()
    {
        var source = new[] { 1, 2, 3 };
        var deque = new Deque<int>(source, capacity: 5, allowGrow: false);

        Assert.AreEqual(5, deque.Capacity);
        Assert.AreEqual(3, deque.Count);
        Assert.IsFalse(deque.AllowGrow);
        CollectionAssert.AreEqual(source, deque.ToArray());
    }

    /// <summary>
    /// Verifies that the (collection, capacity, allowGrow=false) constructor rejects a null source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAllowGrowFalseAndCollectionIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Deque<int>(null!, capacity: 4, allowGrow: false);
        });
    }

    /// <summary>
    /// Verifies that the (capacity, allowGrow) constructor rejects invalid capacities.
    /// </summary>
    /// <param name="capacity">The requested capacity.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void Ctor_WhenAllowGrowFalseAndInvalidCapacity_ShouldThrowExactly(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Deque<int>(capacity, allowGrow: false);
        });
    }

}
