// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.GetEntryAt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.GetEntryAt(int)" /> on an empty dictionary
    /// throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void GetEntryAt_WhenDictionaryIsEmpty_ShouldThrowExactly()
    {
        var sut = new RangeDictionary<int, string>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.GetEntryAt(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.GetEntryAt(int)" /> returns each entry in
    /// ascending order.
    /// </summary>
    [TestMethod]
    public void GetEntryAt_WhenDictionaryPopulated_ShouldReturnEntryInAscendingOrder()
    {
        var sut = new RangeDictionary<int, string>();
        sut.Add(20, 30, "B");
        sut.Add(0, 10, "A");

        ValueRange<int, string> first = sut.GetEntryAt(0);
        ValueRange<int, string> second = sut.GetEntryAt(1);

        Assert.AreEqual(0, first.StartInclusive);
        Assert.AreEqual(10, first.EndExclusive);
        Assert.AreEqual("A", first.Value);

        Assert.AreEqual(20, second.StartInclusive);
        Assert.AreEqual(30, second.EndExclusive);
        Assert.AreEqual("B", second.Value);
    }
    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.GetEntryAt(int)" /> rejects out-of-range indices.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void GetEntryAt_WhenIndexIsOutOfRange_ShouldThrowExactly(int index)
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 10, "A"), (20, 30, "B"));

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.GetEntryAt(index);
        });
    }

}
