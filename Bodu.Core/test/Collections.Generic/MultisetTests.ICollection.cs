// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{
    // --------------------------------------------------------
    // ICollection.IsSynchronized
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="System.Collections.ICollection.IsSynchronized"/> always returns <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void ICollectionIsSynchronized_ShouldReturnFalse()
    {
        ICollection sut = new Multiset<int>();

        Assert.IsFalse(sut.IsSynchronized);
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
        ICollection sut = new Multiset<int>();

        Assert.IsNotNull(sut.SyncRoot);
    }

    /// <summary>
    /// Verifies that <see cref="System.Collections.ICollection.SyncRoot"/> returns the same instance on successive calls.
    /// </summary>
    [TestMethod]
    public void ICollectionSyncRoot_WhenCalledMultipleTimes_ShouldReturnSameInstance()
    {
        ICollection sut = new Multiset<int>();

        var first = sut.SyncRoot;
        var second = sut.SyncRoot;

        Assert.AreSame(first, second);
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
        ICollection<int> sut = new Multiset<int>();

        Assert.IsFalse(sut.IsReadOnly);
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — null array
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentNullException"/> when the array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        ICollection sut = new Multiset<int>();
        ((Multiset<int>)sut).Add(1);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.CopyTo(null!, 0);
        });
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — negative index
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentOutOfRangeException"/> when the index is negative.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ICollection sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.CopyTo(new object[5], -1);
        });
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — array too small
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentException"/> when the destination array is too small.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayIsTooSmall_ShouldThrowArgumentException()
    {
        Multiset<int> multiset = new Multiset<int>();
        multiset.Add(1);
        multiset.Add(2);
        multiset.Add(3);
        ICollection sut = multiset;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CopyTo(new object[2], 0);
        });
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — multidimensional array
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentException"/> when the array is multidimensional.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenArrayIsMultidimensional_ShouldThrowArgumentException()
    {
        ICollection sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CopyTo(new object[2, 2], 0);
        });
    }

    // --------------------------------------------------------
    // ICollection.CopyTo — incompatible element type
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> throws <see cref="ArgumentException"/> when the destination array has an incompatible element type.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenElementTypeIsIncompatible_ShouldThrowArgumentException()
    {
        Multiset<string> multiset = new Multiset<string>();
        multiset.Add("hello");
        ICollection sut = multiset;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CopyTo(new int[1], 0);
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
        ICollection sut = new Multiset<int>();
        object[] dest = [99];

        sut.CopyTo(dest, 0);

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
        Multiset<int> multiset = new Multiset<int>();
        multiset.Add(7, 2);
        ICollection sut = multiset;
        var dest = new object[4];

        sut.CopyTo(dest, 2);

        Assert.IsNull(dest[0]);
        Assert.IsNull(dest[1]);
        Assert.AreEqual(7, (int)dest[2]);
        Assert.AreEqual(7, (int)dest[3]);
    }
}
