// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentPolicyBuilderTests.ToXml.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class AdjustmentPolicyBuilderTests
{
    /// <summary>
    /// Verifies that serializing a document whose adjustment policy is missing its required trigger, action, and
    /// emission throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenAdjustmentPolicyIncomplete_ShouldThrowInvalidOperationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.policy")
            .AddAdjustmentPolicy("incomplete", a => a.WithPriority(1))
            .AddNotableDate("d", "D", NotableDateCategory.Observance, def => def.AddRule("default", r => r.Fixed(1, 1)));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => builder.ToXml());
    }
}
