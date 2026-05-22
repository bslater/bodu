// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.NonGenericInterfaces.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
}
