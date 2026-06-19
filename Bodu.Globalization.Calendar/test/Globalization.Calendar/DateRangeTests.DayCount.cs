// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeTests.DayCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class DateRangeTests
{
    /// <summary>
    /// Verifies that the inclusive day count is zero for an ill-formed (reversed) range and the span length plus one
    /// for a well-formed range.
    /// </summary>
    [TestMethod]
    public void DayCount_ShouldBeZeroWhenInvalidAndInclusiveWhenValid()
    {
        Assert.AreEqual(0, Range(5, 1).DayCount);
        Assert.AreEqual(10, Range(1, 10).DayCount);
    }
}
