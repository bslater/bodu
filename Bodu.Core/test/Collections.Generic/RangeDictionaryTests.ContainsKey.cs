// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.ContainsKey.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.ContainsKey(TKey)" /> rejects a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        RangeDictionary<string, int> sut = new RangeDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.ContainsKey(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="RangeDictionary{TKey, TValue}.ContainsKey(TKey)" /> on an empty dictionary
    /// returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        RangeDictionary<int, string> sut = new RangeDictionary<int, string>();

        Assert.IsFalse(sut.ContainsKey(5));
    }

    /// <summary>
    /// Verifies the membership outcome across boundary and interior positions for a single range.
    /// </summary>
    [TestMethod]
    [DataRow(-1, false)]
    [DataRow(0, true)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(9, false)]
    [DataRow(10, true)]
    [DataRow(14, true)]
    [DataRow(15, false)]
    [DataRow(int.MaxValue, false)]
    public void ContainsKey_WhenKeysVary_ShouldReturnExpected(int key, bool expected)
    {
        RangeDictionary<int, string> sut = CreateDictionary((0, 5, "A"), (10, 15, "B"));

        Assert.AreEqual(expected, sut.ContainsKey(key));
    }
}
