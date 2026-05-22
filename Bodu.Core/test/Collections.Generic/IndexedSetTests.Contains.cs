// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.Contains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Contains(T)" /> uses the configured comparer for equality.
    /// </summary>
    [TestMethod]
    public void Contains_WhenCustomComparerProvided_ShouldUseComparerForEquality()
    {
        IndexedSet<string> sut = CreateSet(["Hello"], StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(sut.Contains("HELLO"));
        Assert.IsTrue(sut.Contains("hello"));
        Assert.IsFalse(sut.Contains("world"));
    }
    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Contains(T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsNull_ShouldThrowExactly()
    {
        var sut = new IndexedSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Contains(null!);
        });
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
    public void Contains_WhenItemPresenceVaries_ShouldReturnExpected(int item, bool expected)
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.AreEqual(expected, sut.Contains(item));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Contains(T)" /> on an empty set returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new IndexedSet<int>();

        Assert.IsFalse(sut.Contains(99));
    }

}
