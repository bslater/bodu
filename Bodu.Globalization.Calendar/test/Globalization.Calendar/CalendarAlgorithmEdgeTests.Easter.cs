// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarAlgorithmEdgeTests.Easter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class CalendarAlgorithmEdgeTests
{
    /// <summary>
    /// Verifies that the Western and Orthodox Easter calculators return <see langword="null" /> for a year outside the
    /// representable range.
    /// </summary>
    [TestMethod]
    public void Easter_WhenYearOutOfRange_ShouldReturnNull()
    {
        Assert.IsNull(EasterCalculator.Western(0));
        Assert.IsNull(EasterCalculator.Western(10000));
        Assert.IsNull(EasterCalculator.Orthodox(0));
        Assert.IsNull(EasterCalculator.Orthodox(10000));
    }
}
