// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T, int)"/> accumulates counts when called multiple times for the same element.
    /// </summary>
    [TestMethod]
    public void Add_WhenCalledMultipleTimesForSameElement_ShouldAccumulateCounts()
    {
        var mvd = new Multiset<string>();

        mvd.Add("x", 3);
        mvd.Add("x", 4);

        Assert.AreEqual(7, mvd.CountOf("x"));
        Assert.AreEqual(7, mvd.Count);
        Assert.AreEqual(1, mvd.DistinctCount);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T, int)"/> throws <see cref="ArgumentOutOfRangeException"/> when count is negative.
    /// </summary>
    [TestMethod]
    public void Add_WhenCountIsNegative_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            mvd.Add(1, -3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T, int)"/> adds the correct number of occurrences at once.
    /// </summary>
    [TestMethod]
    public void Add_WhenCountIsPositive_ShouldAddMultipleOccurrences()
    {
        var mvd = new Multiset<int>();

        mvd.Add(42, 5);

        Assert.AreEqual(5, mvd.CountOf(42));
        Assert.AreEqual(5, mvd.Count);
    }

    // --------------------------------------------------------
    // Add(T item, int count)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T, int)"/> throws <see cref="ArgumentOutOfRangeException"/> when count is zero.
    /// </summary>
    [TestMethod]
    public void Add_WhenCountIsZero_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            mvd.Add(1, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T)"/> increases <see cref="Multiset{T}.DistinctCount"/> only for new elements.
    /// </summary>
    [TestMethod]
    public void Add_WhenDistinctItemsAdded_ShouldIncrementDistinctCount()
    {
        var mvd = new Multiset<string>();

        mvd.Add("a");
        mvd.Add("b");
        mvd.Add("a");

        Assert.AreEqual(2, mvd.DistinctCount);
    }
    // --------------------------------------------------------
    // Add(T item)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T)"/> increments <see cref="Multiset{T}.Count"/> by one.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemAdded_ShouldIncrementCount()
    {
        var mvd = new Multiset<int>();

        mvd.Add(1);

        Assert.AreEqual(1, mvd.Count);
    }

    /// <summary>
    /// Verifies that adding a large number of items maintains accurate count tracking.
    /// </summary>
    [TestMethod]
    public void Add_WhenManyDistinctItemsAdded_ShouldTrackAllCountsAccurately()
    {
        var mvd = new Multiset<int>();
        var expectedTotal = 0;

        for (var i = 0; i < 1000; i++)
        {
            var count = (i % 5) + 1;
            mvd.Add(i, count);
            expectedTotal += count;
        }

        Assert.AreEqual(expectedTotal, mvd.Count);
        Assert.AreEqual(1000, mvd.DistinctCount);

        for (var i = 0; i < 1000; i++)
            Assert.AreEqual((i % 5) + 1, mvd.CountOf(i));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T)"/> increments the multiplicity of an existing element.
    /// </summary>
    [TestMethod]
    public void Add_WhenSameItemAddedTwice_ShouldIncrementCountOf()
    {
        var mvd = new Multiset<int>();

        mvd.Add(7);
        mvd.Add(7);

        Assert.AreEqual(2, mvd.CountOf(7));
        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual(1, mvd.DistinctCount);
    }

}
