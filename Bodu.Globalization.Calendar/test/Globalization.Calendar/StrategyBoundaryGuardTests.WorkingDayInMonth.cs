// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyBoundaryGuardTests.WorkingDayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

public sealed partial class StrategyBoundaryGuardTests
{
    /// <summary>
    /// Verifies that a working day in a month with an ordinal of <see cref="int.MinValue" /> yields no occurrence, as
    /// any ordinal beyond the month's working days does, rather than overflowing.
    /// </summary>
    [TestMethod]
    public void WorkingDayInMonth_WhenOrdinalIsMinimumInteger_ShouldReturnNull()
    {
        var context = new StrategyResolutionContext(NotableDateResourceLoader.Load(OffsetForwardXml), null, Territory);
        var strategy = new WorkingDayInMonthStrategy(3, int.MinValue);

        Assert.IsNull(strategy.Calculate(2025, context));
    }

    /// <summary>
    /// Verifies that a working day in a month with an ordinal beyond the month's working days yields no occurrence.
    /// </summary>
    [TestMethod]
    public void WorkingDayInMonth_WhenOrdinalExceedsTheMonthsWorkingDays_ShouldReturnNull()
    {
        var context = new StrategyResolutionContext(NotableDateResourceLoader.Load(OffsetForwardXml), null, Territory);
        var strategy = new WorkingDayInMonthStrategy(3, -40);

        Assert.IsNull(strategy.Calculate(2025, context));
    }
}
