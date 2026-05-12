// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ValueRangeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="ValueRange{TKey, TValue}" />.
/// </summary>
[TestClass]
public partial class ValueRangeTests
{
    /// <summary>
    /// Verifies the happy-path smoke check: constructing a value range exposes the supplied endpoints and
    /// associated value.
    /// </summary>
    [TestMethod]
    public void ValueRange_WhenConstructed_ShouldExposeEndpointsAndValue()
    {
        var sut = new ValueRange<int, string>(0, 10, "hello");

        Assert.AreEqual(0, sut.StartInclusive);
        Assert.AreEqual(10, sut.EndExclusive);
        Assert.AreEqual("hello", sut.Value);
    }
}
