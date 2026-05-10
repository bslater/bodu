// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.DataDriven.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{
    // --------------------------------------------------------
    // Add(T, int) — parameterised count
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Add(T, int)"/> adds exactly the specified number of occurrences
    /// for a range of count values.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(10)]
    [DataRow(1000)]
    public void Add_WhenCountProvided_ShouldSetCountOfExactly(int count)
    {
        Multiset<int> sut = new Multiset<int>();

        sut.Add(99, count);

        Assert.AreEqual(count, sut.CountOf(99));
        Assert.AreEqual(count, sut.Count);
        Assert.AreEqual(1, sut.DistinctCount);
    }

    // --------------------------------------------------------
    // Remove — decrements from various initial counts
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> decrements the occurrence count by exactly one,
    /// across a range of initial counts.
    /// </summary>
    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(2, 1)]
    [DataRow(5, 4)]
    [DataRow(10, 9)]
    public void Remove_WhenElementHasMultipleOccurrences_ShouldDecrementCountOf(int initialCount, int expectedAfter)
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(42, initialCount);

        sut.Remove(42);

        Assert.AreEqual(expectedAfter, sut.CountOf(42));
        Assert.AreEqual(expectedAfter, sut.Count);
    }

    // --------------------------------------------------------
    // Union — various left/right count combinations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Union"/> returns the maximum of the two operand counts for a
    /// single shared element, across a range of count combinations.
    /// </summary>
    [TestMethod]
    [DataRow(3, 1, 3)]
    [DataRow(1, 3, 3)]
    [DataRow(2, 2, 2)]
    [DataRow(0, 5, 5)]
    [DataRow(5, 0, 5)]
    [DataRow(4, 4, 4)]
    public void Union_WhenCountsVary_ShouldReturnMaxCount(int leftCount, int rightCount, int expectedCount)
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>();
        if (leftCount > 0) a.Add(1, leftCount);
        if (rightCount > 0) b.Add(1, rightCount);

        Multiset<int> result = a.Union(b);

        Assert.AreEqual(expectedCount, result.CountOf(1));
    }

    // --------------------------------------------------------
    // Intersect — various left/right count combinations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Intersect"/> returns the minimum of the two operand counts for a
    /// shared element, across a range of count combinations.
    /// </summary>
    [TestMethod]
    [DataRow(3, 1, 1)]
    [DataRow(1, 3, 1)]
    [DataRow(4, 4, 4)]
    [DataRow(0, 5, 0)]
    [DataRow(5, 0, 0)]
    public void Intersect_WhenCountsVary_ShouldReturnMinCount(int leftCount, int rightCount, int expectedCount)
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>();
        if (leftCount > 0) a.Add(1, leftCount);
        if (rightCount > 0) b.Add(1, rightCount);

        Multiset<int> result = a.Intersect(b);

        Assert.AreEqual(expectedCount, result.CountOf(1));
    }

    // --------------------------------------------------------
    // Except — various left/right count combinations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Except"/> returns max(0, left − right) for a single element,
    /// across a range of count combinations.
    /// </summary>
    [TestMethod]
    [DataRow(3, 1, 2)]
    [DataRow(1, 3, 0)]
    [DataRow(2, 2, 0)]
    [DataRow(5, 0, 5)]
    [DataRow(0, 5, 0)]
    [DataRow(3, 3, 0)]
    public void Except_WhenCountsVary_ShouldReturnClampedDifference(int leftCount, int rightCount, int expectedCount)
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>();
        if (leftCount > 0) a.Add(1, leftCount);
        if (rightCount > 0) b.Add(1, rightCount);

        Multiset<int> result = a.Except(b);

        Assert.AreEqual(expectedCount, result.CountOf(1));
    }

    // --------------------------------------------------------
    // Sum — various left/right count combinations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Sum"/> returns the sum of the two operand counts for a single
    /// element, across a range of count combinations.
    /// </summary>
    [TestMethod]
    [DataRow(3, 1, 4)]
    [DataRow(1, 3, 4)]
    [DataRow(2, 2, 4)]
    [DataRow(0, 5, 5)]
    [DataRow(5, 0, 5)]
    [DataRow(4, 4, 8)]
    public void Sum_WhenCountsVary_ShouldReturnSumOfCounts(int leftCount, int rightCount, int expectedCount)
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>();
        if (leftCount > 0) a.Add(1, leftCount);
        if (rightCount > 0) b.Add(1, rightCount);

        Multiset<int> result = a.Sum(b);

        Assert.AreEqual(expectedCount, result.CountOf(1));
    }

    // --------------------------------------------------------
    // CopyTo — parameterised array size and offset
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.CopyTo"/> fills the destination array starting at the
    /// specified index, leaving preceding slots untouched, for a range of offsets.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    public void CopyTo_WhenOffsetVaries_ShouldFillFromOffsetOnly(int offset)
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(7, 2);
        int[] dest = new int[offset + 2];

        sut.CopyTo(dest, offset);

        for (int i = 0; i < offset; i++)
            Assert.AreEqual(0, dest[i], $"Slot {i} before offset should be untouched.");

        Assert.AreEqual(7, dest[offset]);
        Assert.AreEqual(7, dest[offset + 1]);
    }
}
