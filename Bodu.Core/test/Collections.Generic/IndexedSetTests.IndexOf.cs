// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.IndexOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.IndexOf(T)" /> uses the configured comparer when locating items.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenCustomComparerProvided_ShouldUseComparerForEquality()
    {
        IndexedSet<string> sut = CreateSet(["Alpha", "Beta"], StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(0, sut.IndexOf("ALPHA"));
        Assert.AreEqual(1, sut.IndexOf("beta"));
    }
    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.IndexOf(T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new IndexedSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.IndexOf(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.IndexOf(T)" /> reflects index shifts after a removal.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenItemRemovedFromMiddle_ShouldUpdateLaterIndices()
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30, 40]);

        sut.Remove(20);

        Assert.AreEqual(0, sut.IndexOf(10));
        Assert.AreEqual(1, sut.IndexOf(30));
        Assert.AreEqual(2, sut.IndexOf(40));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.IndexOf(T)" /> returns <c>-1</c> on an empty set.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenSetIsEmpty_ShouldReturnMinusOne()
    {
        var sut = new IndexedSet<int>();

        Assert.AreEqual(-1, sut.IndexOf(99));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.IndexOf(T)" /> returns the insertion-order index for each item.
    /// </summary>
    [TestMethod]
    [DataRow(10, 0)]
    [DataRow(20, 1)]
    [DataRow(30, 2)]
    [DataRow(99, -1)]
    public void IndexOf_WhenSetPopulated_ShouldReturnExpectedIndex(int item, int expected)
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30]);

        Assert.AreEqual(expected, sut.IndexOf(item));
    }

}
