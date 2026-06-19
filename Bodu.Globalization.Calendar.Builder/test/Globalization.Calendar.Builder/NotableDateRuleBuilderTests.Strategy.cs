// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilderTests.Strategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateRuleBuilderTests
{
    /// <summary>
    /// Verifies that selecting a second resolution strategy on the same rule throws
    /// <see cref="InvalidOperationException" /> (the single-strategy invariant).
    /// </summary>
    [TestMethod]
    public void Strategy_WhenSecondStrategySelected_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
            BuildRule(r => r.Fixed(1, 1).Algorithm("western-easter")));
    }

    /// <summary>
    /// Verifies that a rule configured with exactly one strategy builds successfully.
    /// </summary>
    [TestMethod]
    public void Strategy_WhenSingleStrategySelected_ShouldBuild()
    {
        NotableDateResource resource = BuildRule(r => r.Fixed(12, 25));

        Assert.HasCount(1, resource.NotableDates);
    }
}
