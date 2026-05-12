// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{
    /// <summary>
    /// Verifies that the indexer rejects out-of-range indices.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void Indexer_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut[index];
        });
    }

    /// <summary>
    /// Verifies that the indexer on an empty set throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetIsEmpty_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut[0];
        });
    }

    /// <summary>
    /// Verifies that the indexer returns elements in insertion order.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetPopulated_ShouldReturnElementInInsertionOrder()
    {
        OrderedSet<int> sut = CreateSet(new[] { 10, 20, 30 });

        Assert.AreEqual(10, sut[0]);
        Assert.AreEqual(20, sut[1]);
        Assert.AreEqual(30, sut[2]);
    }

    /// <summary>
    /// Verifies that the indexer reflects removals — every surviving element shifts to the slot it would
    /// occupy in a newly-built set with the same insertion sequence.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenItemRemoved_ShouldReflectShiftedIndices()
    {
        OrderedSet<int> sut = CreateSet(new[] { 10, 20, 30, 40 });

        sut.Remove(20);

        Assert.AreEqual(10, sut[0]);
        Assert.AreEqual(30, sut[1]);
        Assert.AreEqual(40, sut[2]);
    }
}
