// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{
    /// <summary>
    /// Verifies that specifying false for overwrite disables the overwrite functionality.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenAllowOverwriteFalseProvided_ShouldDisableOverwrite()
    {
        var buffer = new CircularBuffer<string>(5, false);
        Assert.IsFalse(buffer.AllowOverwrite);
    }

    /// <summary>
    /// Verifies that a buffer constructed with all parameters adopts the collection properly.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenAllParametersProvided_ShouldAdoptCollection()
    {
        var source = new[] { 1, 2, 3 };
        var buffer = new CircularBuffer<int>(source, 3, false);
        CollectionAssert.AreEqual(source, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that a buffer constructed with all parameters respects the overwrite flag.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenAllParametersProvided_ShouldRespectOverwriteFlag()
    {
        var source = new[] { 1, 2, 3 };
        var buffer = new CircularBuffer<int>(source, 3, false);
        Assert.IsFalse(buffer.AllowOverwrite);
    }

    /// <summary>
    /// Verifies that a buffer constructed with all parameters sets the specified capacity.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenAllParametersProvided_ShouldSetCapacity()
    {
        var source = new[] { 1, 2, 3 };
        var buffer = new CircularBuffer<int>(source, 3, false);
        Assert.AreEqual(3, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that the buffer can be created with the maximum allowed array length.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenArrayMaxLengthProvided_ShouldSetCapacity()
    {
        var buffer = new CircularBuffer<int>(0x7FFFFFC7);
        Assert.AreEqual(0x7FFFFFC7, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that constructing from an array preserves the element order.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenArrayProvided_ShouldPreserveOrder()
    {
        int[] source = { 1, 2, 3 };
        var buffer = new CircularBuffer<int>(source, 5);
        CollectionAssert.AreEqual(source, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that constructing from an array sets the correct count.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenArrayProvided_ShouldSetCorrectCount()
    {
        int[] source = { 1, 2, 3 };
        var buffer = new CircularBuffer<int>(source, 5);
        Assert.AreEqual(3, buffer.Count);
    }

    /// <summary>
    /// Verifies that invalid capacity values throw an <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(int.MaxValue)]
    public void Constructor_WhenCapacityIsInvalid_ShouldThrowExactly(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CircularBuffer<int>(capacity);
        });
    }

    /// <summary>
    /// Verifies that the buffer defaults to allowing overwrite when capacity is specified without an explicit overwrite flag.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCapacityProvided_ShouldEnableOverwriteByDefault()
    {
        var buffer = new CircularBuffer<int>(5);
        Assert.IsTrue(buffer.AllowOverwrite);
    }

    /// <summary>
    /// Verifies that providing a valid capacity initializes the buffer with the specified capacity.
    /// </summary>
    [TestMethod]
    [DataRow(5)]
    [DataRow(16)]
    public void Constructor_WhenCapacityProvided_ShouldSetSpecifiedCapacity(int capacity)
    {
        var buffer = new CircularBuffer<int>(capacity);
        Assert.AreEqual(capacity, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that an invalid collection capacity with overwrite flag set throws an <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(int.MaxValue)]
    public void Constructor_WhenCollectionAndOverwriteCapacityIsInvalid_ShouldThrowExactly(int capacity)
    {
        var collection = new[] { 1 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CircularBuffer<int>(collection, capacity, true);
        });
    }

    /// <summary>
    /// Verifies that a collection with invalid capacity throws an <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(int.MaxValue)]
    public void Constructor_WhenCollectionCapacityIsInvalid_ShouldThrowExactly(int capacity)
    {
        var collection = new[] { 1 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CircularBuffer<int>(collection, capacity);
        });
    }

    /// <summary>
    /// Verifies that when a collection exceeds capacity, older elements are trimmed.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionExceedsCapacity_ShouldTrimOldItems()
    {
        var buffer = new CircularBuffer<int>(new[] { 1, 2, 3, 4 }, 2);
        CollectionAssert.AreEqual(new[] { 3, 4 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that exceeding capacity with overwrite disabled throws an <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionExceedsCapacityAndOverwriteDisabled_ShouldThrowExactly()
    {
        var source = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new CircularBuffer<int>(source, 2, false);
        });
    }

    /// <summary>
    /// Verifies that constructing from a null collection throws an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CircularBuffer<object>(null!);
        });
    }

    /// <summary>
    /// Verifies that constructing from a collection adopts its elements.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionProvided_ShouldAdoptElements()
    {
        var source = new[] { "a", "b", "c" };
        var buffer = new CircularBuffer<string>(source);
        CollectionAssert.AreEqual(source, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that constructing from a collection sets the correct element count.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionProvided_ShouldSetCorrectCount()
    {
        var source = new[] { "a", "b", "c" };
        var buffer = new CircularBuffer<string>(source);
        Assert.AreEqual(source.Length, buffer.Count);
    }

    /// <summary>
    /// Verifies that constructing from a collection uses the default capacity.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionProvided_ShouldUseDefaultCapacity()
    {
        var source = new[] { "a", "b", "c" };
        var buffer = new CircularBuffer<string>(source);
        Assert.AreEqual(DefaultCapacity, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that the default constructor enables overwrite by default.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultUsed_ShouldEnableOverwrite()
    {
        var buffer = new CircularBuffer<int>();
        Assert.IsTrue(buffer.AllowOverwrite);
    }

    /// <summary>
    /// Verifies that the default constructor sets the buffer capacity to the predefined default value.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultUsed_ShouldSetDefaultCapacity()
    {
        var buffer = new CircularBuffer<int>();
        Assert.AreEqual(DefaultCapacity, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that the default constructor initializes an empty buffer.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultUsed_ShouldStartEmpty()
    {
        var buffer = new CircularBuffer<int>();
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that a negative capacity for an empty collection throws an <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenEmptyCollectionAndNegativeCapacity_ShouldThrowExactly()
    {
        var empty = Array.Empty<string>();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CircularBuffer<string>(empty, -1);
        });
    }

    /// <summary>
    /// Verifies that a negative capacity with overwrite for an empty collection throws an <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenEmptyCollectionNegativeCapacityAndOverwriteProvided_ShouldThrowExactly()
    {
        var empty = Array.Empty<string>();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CircularBuffer<string>(empty, -10, allowOverwrite: true);
        });
    }

    /// <summary>
    /// Verifies that a non-array enumerable can be used to construct the buffer correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenNonArrayEnumerableProvided_ShouldConsumeCorrectly()
    {
        IEnumerable<int> source = Enumerable.Range(1, 3).Select(x => x * 10);
        var buffer = new CircularBuffer<int>(source, 5);
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that constructing from a collection containing null elements retains nulls at the correct positions.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCollectionContainsNulls_ShouldRetainNulls()
    {
        var source = new string?[] { "A", null, "B" };
        var buffer = new CircularBuffer<string?>(source, 5);

        Assert.AreEqual(3, buffer.Count);

        string?[] result = buffer.ToArray();
        Assert.AreEqual("A", result[0]);
        Assert.IsNull(result[1]);
        Assert.AreEqual("B", result[2]);
    }

    /// <summary>
    /// Verifies that constructing from an empty collection with a valid capacity creates an empty buffer
    /// with the specified capacity.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenEmptyCollectionAndValidCapacity_ShouldCreateEmptyBuffer()
    {
        var buffer = new CircularBuffer<int>(Array.Empty<int>(), 5);

        Assert.AreEqual(0, buffer.Count);
        Assert.AreEqual(5, buffer.Capacity);
    }
}
