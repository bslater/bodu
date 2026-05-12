// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="Range{T}" />.
/// </summary>
[TestClass]
public partial class RangeTests
{
    /// <summary>
    /// Verifies the happy-path smoke check: constructing a range exposes the supplied endpoints and reports
    /// a contained value through <see cref="Range{T}.Contains(T)" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Range_WhenConstructed_ShouldExposeEndpointsAndContainValuesInside()
    {
        Range<int> sut = new Range<int>(0, 10);

        Assert.AreEqual(0, sut.StartInclusive);
        Assert.AreEqual(10, sut.EndExclusive);
        Assert.IsTrue(sut.Contains(5));
    }
}
