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
public partial class NotableDateRuleBuilderTests
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
}
