// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.ICollection.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{

    // --------------------------------------------------------
    // ICollection.CopyTo — multidimensional array
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentException"/> when the array is multidimensional.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        ICollection mvd = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            mvd.CopyTo(new object[2, 2], 0);
        });
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — null array
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentNullException"/> when the array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        ICollection mvd = new Multiset<int>();
        ((Multiset<int>)mvd).Add(1);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            mvd.CopyTo(null!, 0);
        });
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — array too small
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentException"/> when the destination array is too small.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayIsTooSmall_ShouldThrowExactly()
    {
        var multiset = new Multiset<int>();
        multiset.Add(1);
        multiset.Add(2);
        multiset.Add(3);
        ICollection mvd = multiset;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            mvd.CopyTo(new object[2], 0);
        });
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — incompatible element type
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentException"/> when the destination array has an incompatible element type.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenElementTypeIsIncompatible_ShouldThrowExactly()
    {
        var multiset = new Multiset<string>();
        multiset.Add("hello");
        ICollection mvd = multiset;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            mvd.CopyTo(new int[1], 0);
        });
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — negative index
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentOutOfRangeException"/> when the index is negative.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenIndexIsNegative_ShouldThrowExactly()
    {
        ICollection mvd = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            mvd.CopyTo(new object[5], -1);
        });
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — empty multiset
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> does not modify the destination array when the multiset is empty.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenMultisetIsEmpty_ShouldNotModifyArray()
    {
        ICollection mvd = new Multiset<int>();
        object[] dest = [99];

        mvd.CopyTo(dest, 0);

        Assert.AreEqual(99, dest[0]);
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — with offset
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> copies elements starting at the specified index offset.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenOffsetSpecified_ShouldCopyAtCorrectPosition()
    {
        var multiset = new Multiset<int>();
        multiset.Add(7, 2);
        ICollection mvd = multiset;
        var dest = new object[4];

        mvd.CopyTo(dest, 2);

        Assert.IsNull(dest[0]);
        Assert.IsNull(dest[1]);
        Assert.AreEqual(7, (int)dest[2]);
        Assert.AreEqual(7, (int)dest[3]);
    }

    // --------------------------------------------------------
    // ICollection<T>.IsReadOnly
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.IsReadOnly"/> returns <see langword="false"/> for a mutable multiset.
    /// </summary>
    [TestMethod]
    public void ICollectionIsReadOnly_ShouldReturnFalse()
    {
        ICollection<int> mvd = new Multiset<int>();

        Assert.IsFalse(mvd.IsReadOnly);
    }
    // --------------------------------------------------------
    // ICollection.IsSynchronized
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="System.Collections.ICollection.IsSynchronized"/> always returns <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void ICollectionIsSynchronized_ShouldReturnFalse()
    {
        ICollection mvd = new Multiset<int>();

        Assert.IsFalse(mvd.IsSynchronized);
    }

    // --------------------------------------------------------
    // ICollection.SyncRoot
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="System.Collections.ICollection.SyncRoot"/> returns a non-null object.
    /// </summary>
    [TestMethod]
    public void ICollectionSyncRoot_ShouldReturnNonNull()
    {
        ICollection mvd = new Multiset<int>();

        Assert.IsNotNull(mvd.SyncRoot);
    }

    /// <summary>
    /// Verifies that <see cref="System.Collections.ICollection.SyncRoot"/> returns the same instance on successive calls.
    /// </summary>
    [TestMethod]
    public void ICollectionSyncRoot_WhenCalledMultipleTimes_ShouldReturnSameInstance()
    {
        ICollection mvd = new Multiset<int>();

        var first = mvd.SyncRoot;
        var second = mvd.SyncRoot;

        Assert.AreSame(first, second);
    }

}
