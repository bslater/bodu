// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ValueRangeTests.ToString.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class ValueRangeTests
{
    /// <summary>
    /// Verifies that <see cref="ValueRange{TKey, TValue}.ToString" /> formats endpoints and value using the
    /// half-open assignment notation.
    /// </summary>
    [TestMethod]
    public void ToString_WhenAllPresent_ShouldFormatHalfOpenWithValue()
    {
        var sut = new ValueRange<int, string>(0, 10, "tier-A");

        Assert.AreEqual("[0, 10) = tier-A", sut.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="ValueRange{TKey, TValue}.ToString" /> handles value-typed values via their
    /// default string representation.
    /// </summary>
    [TestMethod]
    public void ToString_WhenValueIsValueType_ShouldFormatHalfOpenWithValue()
    {
        var sut = new ValueRange<int, int>(0, 10, 42);

        Assert.AreEqual("[0, 10) = 42", sut.ToString());
    }
}
