// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    public void Range_WhenConstructed_ShouldExposeEndpointsAndContainValuesInside()
    {
        var sut = new Range<int>(0, 10);

        Assert.AreEqual(0, sut.StartInclusive);
        Assert.AreEqual(10, sut.EndExclusive);
        Assert.IsTrue(sut.Contains(5));
    }

}
