// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeTests.ToString.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class RangeTests
{
    /// <summary>
    /// Verifies that <see cref="Range{T}.ToString" /> formats integer endpoints using the half-open notation
    /// <c>[start, end)</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenEndpointsAreIntegers_ShouldFormatHalfOpen()
    {
        var sut = new Range<int>(0, 10);

        Assert.AreEqual("[0, 10)", sut.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="Range{T}.ToString" /> uses the string representation of each endpoint.
    /// </summary>
    [TestMethod]
    public void ToString_WhenEndpointsAreStrings_ShouldFormatHalfOpen()
    {
        var sut = new Range<string>("alpha", "omega");

        Assert.AreEqual("[alpha, omega)", sut.ToString());
    }
}
