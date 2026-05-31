// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Ctor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{

    /// <summary>
    /// Verifies that an invalid capacity throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void Ctor_WhenCapacityIsInvalid_ShouldThrowExactly(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Deque<int>(capacity);
        });
    }

    /// <summary>
    /// Verifies that the capacity-only constructor sets the requested initial capacity.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(64)]
    public void Ctor_WhenCapacityProvided_ShouldUseSpecifiedCapacity(int capacity)
    {
        var deque = new Deque<int>(capacity);
        Assert.AreEqual(capacity, deque.Capacity);
    }

    /// <summary>
    /// Verifies that constructing from a null collection throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Deque<int>(null!);
        });
    }

    /// <summary>
    /// Verifies that constructing from a collection adopts elements in order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionProvided_ShouldAdoptElements()
    {
        var source = new[] { 1, 2, 3 };
        var deque = new Deque<int>(source);
        CollectionAssert.AreEqual(source, deque.ToArray());
    }
    /// <summary>
    /// Verifies that the parameterless constructor uses the default capacity hint.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultUsed_ShouldUseDefaultCapacity()
    {
        var deque = new Deque<int>();
        Assert.AreEqual(DefaultCapacity, deque.Capacity);
        Assert.AreEqual(0, deque.Count);
    }

    /// <summary>
    /// Verifies that constructing from a small collection still uses at least the default capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSmallCollectionProvided_ShouldUseAtLeastDefaultCapacity()
    {
        var deque = new Deque<int>([1]);
        Assert.IsGreaterThanOrEqualTo(DefaultCapacity, deque.Capacity);
    }

}
