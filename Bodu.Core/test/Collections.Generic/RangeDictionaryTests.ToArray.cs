// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.ToArray.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.ToArray" /> returns a fresh array disconnected
    /// from subsequent mutations.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ShouldReturnDisconnectedSnapshot()
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"));
        ValueRange<int, string>[] snapshot = sut.ToArray();

        sut.Add(10, 15, "B");

        Assert.HasCount(1, snapshot);
        Assert.AreEqual("A", snapshot[0].Value);
    }
    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.ToArray" /> on an empty dictionary returns an
    /// empty array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenDictionaryIsEmpty_ShouldReturnEmptyArray()
    {
        var sut = new RangeDictionary<int, string>();

        ValueRange<int, string>[] array = sut.ToArray();

        Assert.IsEmpty(array);
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.ToArray" /> returns the stored entries in
    /// ascending order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenDictionaryPopulated_ShouldReturnEntriesInAscendingOrder()
    {
        var sut = new RangeDictionary<int, string>();
        sut.Add(20, 25, "C");
        sut.Add(0, 5, "A");
        sut.Add(10, 15, "B");

        ValueRange<int, string>[] array = sut.ToArray();

        Assert.HasCount(3, array);
        Assert.AreEqual(0, array[0].StartInclusive);
        Assert.AreEqual("A", array[0].Value);
        Assert.AreEqual(10, array[1].StartInclusive);
        Assert.AreEqual("B", array[1].Value);
        Assert.AreEqual(20, array[2].StartInclusive);
        Assert.AreEqual("C", array[2].Value);
    }

}
