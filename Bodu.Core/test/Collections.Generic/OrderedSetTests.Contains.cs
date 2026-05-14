// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.Contains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{
    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Contains(T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new OrderedSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Contains(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Contains(T)" /> returns <see langword="false" /> on an empty set.
    /// </summary>
    [TestMethod]
    public void Contains_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new OrderedSet<int>();

        Assert.IsFalse(sut.Contains(99));
    }

    /// <summary>
    /// Verifies the membership outcome across a range of inputs.
    /// </summary>
    [TestMethod]
    [DataRow(1, true)]
    [DataRow(2, true)]
    [DataRow(3, true)]
    [DataRow(0, false)]
    [DataRow(4, false)]
    [DataRow(int.MaxValue, false)]
    public void Contains_WhenItemPresenceVaries_ShouldReturnExpected(int item, bool expected)
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.AreEqual(expected, sut.Contains(item));
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Contains(T)" /> uses the configured comparer for equality.
    /// </summary>
    [TestMethod]
    public void Contains_WhenCustomComparerProvided_ShouldUseComparerForEquality()
    {
        OrderedSet<string> sut = CreateSet(["Hello"], StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(sut.Contains("HELLO"));
        Assert.IsTrue(sut.Contains("hello"));
        Assert.IsFalse(sut.Contains("world"));
    }
}
