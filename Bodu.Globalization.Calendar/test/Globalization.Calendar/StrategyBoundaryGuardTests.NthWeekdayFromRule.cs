// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyBoundaryGuardTests.NthWeekdayFromRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

public sealed partial class StrategyBoundaryGuardTests
{
    /// <summary>
    /// Verifies that an n-th weekday from a rule with the least ordinal the loader accepts yields no occurrence when its
    /// target, counted back from a reference in the year 1, falls before the first representable date.
    /// </summary>
    [TestMethod]
    public void NthWeekdayFromRule_WhenTargetPrecedesMinValue_ShouldReturnNull()
    {
        var context = new StrategyResolutionContext(NotableDateResourceLoader.Load(OffsetForwardXml), null, Territory);
        var strategy = new NthWeekdayFromRuleStrategy("anchor", null, DayOfWeek.Monday, -366);

        Assert.IsNull(strategy.Calculate(1, context));
    }
}
