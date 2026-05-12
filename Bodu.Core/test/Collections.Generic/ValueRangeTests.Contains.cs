// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ValueRangeTests.Contains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class ValueRangeTests
{
    /// <summary>
    /// Verifies that <see cref="ValueRange{TKey, TValue}.Contains(TKey)" /> rejects a <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void Contains_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        ValueRange<string, int> sut = new ValueRange<string, int>("a", "z", 42);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Contains(null!);
        });
    }

    /// <summary>
    /// Verifies that the inclusive start of the range is contained.
    /// </summary>
    [TestMethod]
    public void Contains_WhenKeyEqualsStartInclusive_ShouldReturnTrue()
    {
        ValueRange<int, string> sut = new ValueRange<int, string>(0, 10, "x");

        Assert.IsTrue(sut.Contains(0));
    }

    /// <summary>
    /// Verifies that the exclusive end of the range is not contained.
    /// </summary>
    [TestMethod]
    public void Contains_WhenKeyEqualsEndExclusive_ShouldReturnFalse()
    {
        ValueRange<int, string> sut = new ValueRange<int, string>(0, 10, "x");

        Assert.IsFalse(sut.Contains(10));
    }

    /// <summary>
    /// Verifies the membership outcome across a representative set of inputs.
    /// </summary>
    [TestMethod]
    [DataRow(-1, false)]
    [DataRow(0, true)]
    [DataRow(5, true)]
    [DataRow(9, true)]
    [DataRow(10, false)]
    [DataRow(int.MaxValue, false)]
    public void Contains_WhenKeysVary_ShouldReturnExpected(int key, bool expected)
    {
        ValueRange<int, string> sut = new ValueRange<int, string>(0, 10, "x");

        Assert.AreEqual(expected, sut.Contains(key));
    }
}
