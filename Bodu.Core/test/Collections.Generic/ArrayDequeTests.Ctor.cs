// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that the parameterless constructor uses the default capacity.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultUsed_ShouldSetDefaultCapacity()
    {
        var deque = new ArrayDeque<int>();
        Assert.AreEqual(DefaultCapacity, deque.Capacity);
    }

    /// <summary>
    /// Verifies that the parameterless constructor produces an empty deque.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultUsed_ShouldStartEmpty()
    {
        var deque = new ArrayDeque<int>();
        Assert.AreEqual(0, deque.Count);
        Assert.IsTrue(deque.IsEmpty);
    }

    /// <summary>
    /// Verifies that the capacity-only constructor sets the requested capacity.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(16)]
    public void Constructor_WhenCapacityProvided_ShouldSetSpecifiedCapacity(int capacity)
    {
        var deque = new ArrayDeque<int>(capacity);
        Assert.AreEqual(capacity, deque.Capacity);
    }

    /// <summary>
    /// Verifies that an invalid capacity throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(int.MaxValue)]
    public void Constructor_WhenCapacityIsInvalid_ShouldThrowExactly(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ArrayDeque<int>(capacity);
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
            _ = new ArrayDeque<object>(null!);
        });
    }

    /// <summary>
    /// Verifies that constructing from a collection adopts its elements in order.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionProvided_ShouldAdoptElements()
    {
        var source = new[] { "a", "b", "c" };
        var deque = new ArrayDeque<string>(source);
        CollectionAssert.AreEqual(source, deque.ToArray());
    }

    /// <summary>
    /// Verifies that the IEnumerable-only constructor sizes capacity to at least the default.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSmallCollectionProvided_ShouldUseAtLeastDefaultCapacity()
    {
        var deque = new ArrayDeque<int>(new[] { 1, 2, 3 });
        Assert.IsTrue(deque.Capacity >= DefaultCapacity);
        Assert.AreEqual(3, deque.Count);
    }

    /// <summary>
    /// Verifies that constructing from a collection larger than the default capacity grows to fit.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenLargeCollectionProvided_ShouldFitAll()
    {
        var source = Enumerable.Range(0, 100).ToArray();
        var deque = new ArrayDeque<int>(source);
        Assert.AreEqual(100, deque.Count);
        Assert.IsTrue(deque.Capacity >= 100);
        CollectionAssert.AreEqual(source, deque.ToArray());
    }

    /// <summary>
    /// Verifies that the (collection, capacity) constructor copies elements and uses the requested capacity.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionAndCapacityProvided_ShouldUseRequestedCapacity()
    {
        var source = new[] { 1, 2, 3 };
        var deque = new ArrayDeque<int>(source, 5);
        Assert.AreEqual(5, deque.Capacity);
        Assert.AreEqual(3, deque.Count);
        CollectionAssert.AreEqual(source, deque.ToArray());
    }

    /// <summary>
    /// Verifies that supplying a collection larger than the requested capacity throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionExceedsCapacity_ShouldThrowExactly()
    {
        var source = new[] { 1, 2, 3, 4 };
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new ArrayDeque<int>(source, 2);
        });
    }

    /// <summary>
    /// Verifies that an invalid capacity in the (collection, capacity) constructor throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void Constructor_WhenCollectionAndInvalidCapacity_ShouldThrowExactly(int capacity)
    {
        var source = new[] { 1 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ArrayDeque<int>(source, capacity);
        });
    }
}
