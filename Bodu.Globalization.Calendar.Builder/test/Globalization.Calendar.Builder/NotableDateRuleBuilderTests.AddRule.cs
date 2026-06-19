// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilderTests.AddRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateRuleBuilderTests
{
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
