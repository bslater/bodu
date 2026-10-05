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
    /// Verifies that an n-th weekday from a rule with an ordinal of <see cref="int.MinValue" /> yields no occurrence, as
    /// any ordinal whose target falls before the first representable date does, rather than overflowing.
    /// </summary>
    [TestMethod]
    public void NthWeekdayFromRule_WhenOrdinalIsMinimumInteger_ShouldReturnNull()
    {
        var context = new StrategyResolutionContext(NotableDateResourceLoader.Load(OffsetForwardXml), null, Territory);
        var strategy = new NthWeekdayFromRuleStrategy("anchor", null, DayOfWeek.Monday, int.MinValue);

        Assert.IsNull(strategy.Calculate(2025, context));
    }

    /// <summary>
    /// Verifies that an n-th weekday from a rule with an ordinal one greater than <see cref="int.MinValue" /> yields no
    /// occurrence, its target falling before the first representable date.
    /// </summary>
    [TestMethod]
    public void NthWeekdayFromRule_WhenTargetPrecedesMinValue_ShouldReturnNull()
    {
        var context = new StrategyResolutionContext(NotableDateResourceLoader.Load(OffsetForwardXml), null, Territory);
        var strategy = new NthWeekdayFromRuleStrategy("anchor", null, DayOfWeek.Monday, int.MinValue + 1);

        Assert.IsNull(strategy.Calculate(2025, context));
    }
}
