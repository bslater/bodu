// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.ToArray.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeSetTests
{

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

        Assert.HasCount(1, snapshot);
        Assert.AreEqual(new Range<int>(0, 5), snapshot[0]);
    }
    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.ToArray" /> on an empty set returns an empty array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSetIsEmpty_ShouldReturnEmptyArray()
    {
        var sut = new RangeSet<int>();

        Range<int>[] array = sut.ToArray();

        Assert.IsEmpty(array);
    }

    /// <summary>
    /// Verifies that <see cref="RangeSet{T}.ToArray" /> returns the stored ranges in ascending order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSetPopulated_ShouldReturnRangesInAscendingOrder()
    {
        RangeSet<int> sut = CreateSet((20, 25), (0, 5), (10, 15));

        Range<int>[] array = sut.ToArray();

        Assert.HasCount(3, array);
        Assert.AreEqual(new Range<int>(0, 5), array[0]);
        Assert.AreEqual(new Range<int>(10, 15), array[1]);
        Assert.AreEqual(new Range<int>(20, 25), array[2]);
    }

}
