// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarAlgorithmEdgeTests.FixedDateStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class CalendarAlgorithmEdgeTests
{
    /// <summary>
    /// Verifies that a fixed-date strategy yields no occurrence for an out-of-range year through both the single and
    /// all-occurrence calculation paths.
    /// </summary>
    [TestMethod]
    public void FixedDateStrategy_WhenYearOutOfRange_ShouldReturnNullAndEmpty()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(MinimalResource);
        var context = new StrategyResolutionContext(resource);
        var strategy = new FixedDateStrategy(1, 1);

        Assert.IsNull(strategy.Calculate(0, context));
        Assert.IsEmpty(strategy.CalculateAll(0, context));
    }
}
