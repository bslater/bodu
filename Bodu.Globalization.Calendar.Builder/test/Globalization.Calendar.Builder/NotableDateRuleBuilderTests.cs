// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Contains unit tests for <see cref="NotableDateRuleBuilder" /> — the single-strategy invariant and argument guards.
/// </summary>
[TestClass]
public class NotableDateRuleBuilderTests
{
    /// <summary>
    /// Builds a one-rule document whose rule is configured by the supplied callback.
    /// </summary>
    /// <param name="configure">The rule configuration.</param>
    /// <returns>The built resource.</returns>
    private static NotableDateResource BuildRule(Action<NotableDateRuleBuilder> configure) =>
        NotableDateDocumentBuilder.Create("demo.rule")
            .AddNotableDate("d", "D", NotableDateCategory.Observance, def => def.AddRule("default", configure))
            .Build();

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

    /// <summary>
    /// Verifies that passing a <see langword="null" /> rule configuration to <c>AddRule</c> throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddRule_WhenConfigureNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            NotableDateDocumentBuilder.Create("demo.rule")
                .AddNotableDate("d", "D", NotableDateCategory.Observance, def => def.AddRule("default", null!)));
    }
}
