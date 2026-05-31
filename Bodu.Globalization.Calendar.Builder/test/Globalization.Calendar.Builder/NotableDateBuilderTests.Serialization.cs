// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBuilderTests.Serialization.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Verifies the serialisation behaviour of <see cref="NotableDateBuilder" /> when the builder contains no rules.
/// </summary>
[TestClass]
public class NotableDateBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToXml" /> throws
    /// <see cref="InvalidOperationException" /> when a notable date entry has no rules, because
    /// <c>NotableDateBuilder.ToXElement</c> requires at least one rule.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenNotableDateHasNoRules_ShouldThrowExactly()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Empty Date", _ => { });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.ToXml();
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToJson" /> throws
    /// <see cref="InvalidOperationException" /> when a notable date entry has no rules, because
    /// <c>NotableDateBuilder.ToJsonNode</c> requires at least one rule.
    /// </summary>
    [TestMethod]
    public void ToJson_WhenNotableDateHasNoRules_ShouldThrowExactly()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Empty Date", _ => { });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.ToJson();
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToJsonNode" /> throws
    /// <see cref="InvalidOperationException" /> when a notable date entry has no rules.
    /// </summary>
    [TestMethod]
    public void ToJsonNode_WhenNotableDateHasNoRules_ShouldThrowExactly()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create()
            .AddDate("Empty Date", _ => { });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = builder.ToJsonNode();
        });
    }
}
