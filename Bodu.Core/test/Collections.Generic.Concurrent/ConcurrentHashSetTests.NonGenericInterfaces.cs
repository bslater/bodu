// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.NonGenericInterfaces.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.Count" /> reports the element count.
    /// </summary>
    [TestMethod]
    public void ICollectionCount_ShouldReportElementCount()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.AreEqual(3, collection.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.IsSynchronized" /> reports <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void IsSynchronized_WhenAccessed_ShouldReturnFalse()
    {
        ICollection collection = new ConcurrentHashSet<int>();

        Assert.IsFalse(collection.IsSynchronized);
    }

    /// <summary>
    /// Verifies that accessing <see cref="ICollection.SyncRoot" /> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void SyncRoot_WhenAccessed_ShouldThrowNotSupportedException()
    {
        ICollection collection = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = collection.SyncRoot;
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> copies every element into a compatible array.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayCompatible_ShouldCopyEveryElement()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        var array = new int[3];

        collection.CopyTo(array, 0);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, array);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> throws <see cref="ArgumentNullException" /> for
    /// a <see langword="null" /> destination array.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1 });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            collection.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> throws <see cref="ArgumentException" /> for a
    /// multidimensional destination array.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayIsMultidimensional_ShouldThrowArgumentException()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1 });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new int[2, 2], 0);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> throws <see cref="ArgumentException" /> when the
    /// destination array element type is incompatible with <c>T</c>.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayElementTypeIncompatible_ShouldThrowArgumentException()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1, 2 });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new string[5], 0);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> begins copying at the supplied index.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenIndexSupplied_ShouldCopyStartingAtThatIndex()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 7, 8 });
        var array = new int[5];

        collection.CopyTo(array, 2);

        Assert.AreEqual(0, array[0]);
        Assert.AreEqual(0, array[1]);
        Assert.AreEqual(0, array[4]);
        CollectionAssert.AreEquivalent(new[] { 7, 8 }, new[] { array[2], array[3] });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for a negative destination index.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1 });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            collection.CopyTo(new int[2], -1);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> throws <see cref="ArgumentException" /> when the
    /// destination array has insufficient space.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayTooSmall_ShouldThrowArgumentException()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new int[2], 0);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> throws <see cref="ArgumentException" /> when the
    /// start index leaves too little room even though the array is large enough overall.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenIndexLeavesInsufficientRoom_ShouldThrowArgumentException()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new int[5], 4);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> copies every element when the destination fits
    /// the set exactly.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayExactlyFits_ShouldCopyEveryElement()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        var array = new int[3];

        collection.CopyTo(array, 0);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, array);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> copies an empty set at an index equal to the
    /// destination length without throwing.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenSetIsEmptyAndIndexEqualsLength_ShouldSucceed()
    {
        ICollection collection = new ConcurrentHashSet<int>();
        var array = new int[3];

        collection.CopyTo(array, 3);

        CollectionAssert.AreEqual(new int[3], array);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> throws <see cref="ArgumentException" /> rather
    /// than mis-validating when the start index plus the element count would overflow <see cref="int" />.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenIndexPlusCountWouldOverflow_ShouldThrowArgumentException()
    {
        ICollection collection = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new int[10], int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ICollection.CopyTo" /> throws <see cref="ArgumentException" /> when an
    /// element cannot be cast to the destination array's element type.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenElementCannotCastToDestination_ShouldThrowArgumentException()
    {
        ICollection collection = new ConcurrentHashSet<object>(new object[] { new object() });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new string[5], 0);
        });
    }
}
