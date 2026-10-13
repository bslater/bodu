// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// Verifies that the capacity constructor rejects a capacity one above <see cref="Array.MaxLength" /> with
    /// <see cref="ArgumentOutOfRangeException" /> naming <c>capacity</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityExceedsArrayMaxLength_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Deque<int>(Array.MaxLength + 1);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the (capacity, allowGrow) constructor rejects a capacity one above <see cref="Array.MaxLength" />
    /// with <see cref="ArgumentOutOfRangeException" /> naming <c>capacity</c>, whether or not growth is allowed.
    /// </summary>
    /// <param name="allowGrow">The growth policy passed to the constructor.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_WhenGrowthPolicyProvidedAndCapacityExceedsArrayMaxLength_ShouldThrowArgumentOutOfRangeException(bool allowGrow)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Deque<int>(Array.MaxLength + 1, allowGrow);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the (collection, capacity) constructor rejects a capacity one above <see cref="Array.MaxLength" />
    /// with <see cref="ArgumentOutOfRangeException" /> naming <c>capacity</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionProvidedAndCapacityExceedsArrayMaxLength_ShouldThrowArgumentOutOfRangeException()
    {
        int[] source = [1, 2];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Deque<int>(source, Array.MaxLength + 1);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the (collection, capacity, allowGrow) constructor rejects a capacity one above
    /// <see cref="Array.MaxLength" /> with <see cref="ArgumentOutOfRangeException" /> naming <c>capacity</c>, whether or
    /// not growth is allowed.
    /// </summary>
    /// <param name="allowGrow">The growth policy passed to the constructor.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_WhenCollectionAndGrowthPolicyProvidedAndCapacityExceedsArrayMaxLength_ShouldThrowArgumentOutOfRangeException(bool allowGrow)
    {
        int[] source = [1];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Deque<int>(source, Array.MaxLength + 1, allowGrow);
        });

        Assert.AreEqual("capacity", ex.ParamName);
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
    /// Verifies that the (collection, capacity, allowGrow) constructor rejects a capacity below 1 with
    /// <see cref="ArgumentOutOfRangeException" /> whether or not growth is allowed.
    /// </summary>
    /// <param name="capacity">The invalid capacity.</param>
    /// <param name="allowGrow">The growth policy passed to the constructor.</param>
    [TestMethod]
    [DataRow(-1, true)]
    [DataRow(0, true)]
    [DataRow(-1, false)]
    [DataRow(0, false)]
    public void Ctor_WhenCollectionAndGrowthPolicyProvidedAndCapacityIsInvalid_ShouldThrowArgumentOutOfRangeException(int capacity, bool allowGrow)
    {
        int[] source = [1];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Deque<int>(source, capacity, allowGrow);
        });

        Assert.AreEqual("capacity", ex.ParamName);
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
    /// Verifies that the (collection, capacity) constructor rejects a capacity below 1 with
    /// <see cref="ArgumentOutOfRangeException" />, even though growth would let the source fit.
    /// </summary>
    /// <param name="capacity">The invalid capacity.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void Ctor_WhenCollectionProvidedAndCapacityIsInvalid_ShouldThrowArgumentOutOfRangeException(int capacity)
    {
        int[] source = [1, 2];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Deque<int>(source, capacity);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing from a collection adopts elements in order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionProvided_ShouldAdoptElements()
    {
        int[] source = new[] { 1, 2, 3 };
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

    /// <summary>
    /// Verifies that constructing from a collection larger than the requested capacity creates the deque with a capacity
    /// equal to the number of elements and keeps every element, as the constructor documents.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionExceedsRequestedCapacity_ShouldUseTheElementCount()
    {
        var deque = new Deque<int>(Enumerable.Range(0, 10), 4);

        Assert.AreEqual(10, deque.Capacity);
        CollectionAssert.AreEqual(Enumerable.Range(0, 10).ToArray(), deque.ToArray());
    }

    /// <summary>
    /// Verifies that constructing a growable deque from a collection larger than the requested capacity creates it with
    /// a capacity equal to the number of elements and keeps every element, as the constructor documents.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionExceedsRequestedCapacity_ForGrowthAllowed_ShouldUseTheElementCount()
    {
        var deque = new Deque<int>(Enumerable.Range(0, 10), 4, allowGrow: true);

        Assert.AreEqual(10, deque.Capacity);
        CollectionAssert.AreEqual(Enumerable.Range(0, 10).ToArray(), deque.ToArray());
    }

}
