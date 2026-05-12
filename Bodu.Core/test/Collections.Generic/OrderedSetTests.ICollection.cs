// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{
    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.Add(T)" /> implementation appends a new item.
    /// </summary>
    [TestMethod]
    public void ICollectionAdd_WhenItemIsNew_ShouldAppendItem()
    {
        var sut = new OrderedSet<int>();
        ICollection<int> typed = sut;

        typed.Add(42);

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(42, sut[0]);
    }

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.Add(T)" /> implementation discards the duplicate
    /// outcome and leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void ICollectionAdd_WhenItemIsDuplicate_ShouldLeaveSetUnchanged()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2, 3 });
        ICollection<int> typed = sut;

        typed.Add(2);

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.Add(T)" /> implementation rejects a
    /// <see langword="null" /> reference.
    /// </summary>
    [TestMethod]
    public void ICollectionAdd_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new OrderedSet<string>();
        ICollection<string> typed = sut;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            typed.Add(null!);
        });
    }
}
