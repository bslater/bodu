// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.ToArray.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{
    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.ToArray" /> on an empty set returns an empty array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSetIsEmpty_ShouldReturnEmptyArray()
    {
        var sut = new RangeSet<int>();

        Range<int>[] array = sut.ToArray();

        Assert.AreEqual(0, array.Length);
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.ToArray" /> returns the stored ranges in ascending order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSetPopulated_ShouldReturnRangesInAscendingOrder()
    {
        RangeSet<int> sut = CreateSet((20, 25), (0, 5), (10, 15));

        Range<int>[] array = sut.ToArray();

        Assert.AreEqual(3, array.Length);
        Assert.AreEqual(new Range<int>(0, 5), array[0]);
        Assert.AreEqual(new Range<int>(10, 15), array[1]);
        Assert.AreEqual(new Range<int>(20, 25), array[2]);
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.ToArray" /> returns a fresh array disconnected from subsequent
    /// mutations.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ShouldReturnDisconnectedSnapshot()
    {
        RangeSet<int> sut = CreateSet((0, 5));
        Range<int>[] snapshot = sut.ToArray();

        sut.Add(10, 15);

        Assert.AreEqual(1, snapshot.Length);
        Assert.AreEqual(new Range<int>(0, 5), snapshot[0]);
    }
}
