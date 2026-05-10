// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{
    // --------------------------------------------------------
    // Add(T item)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T)"/> increments <see cref="Multiset{T}.Count"/> by one.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemAdded_ShouldIncrementCount()
    {
        Multiset<int> sut = new Multiset<int>();

        sut.Add(1);

        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T)"/> increments the multiplicity of an existing element.
    /// </summary>
    [TestMethod]
    public void Add_WhenSameItemAddedTwice_ShouldIncrementCountOf()
    {
        Multiset<int> sut = new Multiset<int>();

        sut.Add(7);
        sut.Add(7);

        Assert.AreEqual(2, sut.CountOf(7));
        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(1, sut.DistinctCount);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T)"/> increases <see cref="Multiset{T}.DistinctCount"/> only for new elements.
    /// </summary>
    [TestMethod]
    public void Add_WhenDistinctItemsAdded_ShouldIncrementDistinctCount()
    {
        Multiset<string> sut = new Multiset<string>();

        sut.Add("a");
        sut.Add("b");
        sut.Add("a");

        Assert.AreEqual(2, sut.DistinctCount);
    }

    // --------------------------------------------------------
    // Add(T item, int count)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T, int)"/> throws <see cref="ArgumentOutOfRangeException"/> when count is zero.
    /// </summary>
    [TestMethod]
    public void Add_WhenCountIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        Multiset<int> sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.Add(1, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T, int)"/> throws <see cref="ArgumentOutOfRangeException"/> when count is negative.
    /// </summary>
    [TestMethod]
    public void Add_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Multiset<int> sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.Add(1, -3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T, int)"/> adds the correct number of occurrences at once.
    /// </summary>
    [TestMethod]
    public void Add_WhenCountIsPositive_ShouldAddMultipleOccurrences()
    {
        Multiset<int> sut = new Multiset<int>();

        sut.Add(42, 5);

        Assert.AreEqual(5, sut.CountOf(42));
        Assert.AreEqual(5, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T, int)"/> accumulates counts when called multiple times for the same element.
    /// </summary>
    [TestMethod]
    public void Add_WhenCalledMultipleTimesForSameElement_ShouldAccumulateCounts()
    {
        Multiset<string> sut = new Multiset<string>();

        sut.Add("x", 3);
        sut.Add("x", 4);

        Assert.AreEqual(7, sut.CountOf("x"));
        Assert.AreEqual(7, sut.Count);
        Assert.AreEqual(1, sut.DistinctCount);
    }

    /// <summary>
    /// Verifies that adding a large number of items maintains accurate count tracking.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Add_WhenManyDistinctItemsAdded_ShouldTrackAllCountsAccurately()
    {
        Multiset<int> sut = new Multiset<int>();
        int expectedTotal = 0;

        for (int i = 0; i < 1000; i++)
        {
            int count = (i % 5) + 1;
            sut.Add(i, count);
            expectedTotal += count;
        }

        Assert.AreEqual(expectedTotal, sut.Count);
        Assert.AreEqual(1000, sut.DistinctCount);

        for (int i = 0; i < 1000; i++)
            Assert.AreEqual((i % 5) + 1, sut.CountOf(i));
    }
}
