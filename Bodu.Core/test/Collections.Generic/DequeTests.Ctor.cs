// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that the parameterless constructor uses the default capacity hint.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultUsed_ShouldUseDefaultCapacity()
    {
        var deque = new Deque<int>();
        Assert.AreEqual(DefaultCapacity, deque.Capacity);
        Assert.AreEqual(0, deque.Count);
    }

    /// <summary>
    /// Verifies that the capacity-only constructor sets the requested initial capacity.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(64)]
    public void Constructor_WhenCapacityProvided_ShouldUseSpecifiedCapacity(int capacity)
    {
        var deque = new Deque<int>(capacity);
        Assert.AreEqual(capacity, deque.Capacity);
    }

    /// <summary>
    /// Verifies that an invalid capacity throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void Constructor_WhenCapacityIsInvalid_ShouldThrowExactly(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Deque<int>(capacity);
        });
    }

    /// <summary>
    /// Verifies that constructing from a null collection throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionIsNull_ShouldThrowExactly()
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
    public void Constructor_WhenCollectionProvided_ShouldAdoptElements()
    {
        var source = new[] { 1, 2, 3 };
        var deque = new Deque<int>(source);
        CollectionAssert.AreEqual(source, deque.ToArray());
    }

    /// <summary>
    /// Verifies that constructing from a small collection still uses at least the default capacity.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSmallCollectionProvided_ShouldUseAtLeastDefaultCapacity()
    {
        var deque = new Deque<int>(new[] { 1 });
        Assert.IsTrue(deque.Capacity >= DefaultCapacity);
    }
}
