// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.IndentOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that a non-empty <see cref="XmlDocFormatOptions.IndentText" /> indents nested block content by
    /// one indent unit per nesting level, leaving the enclosing open/close tags at their parent's indent.
    /// </summary>
    [TestMethod]
    public void Format_WhenIndentTextSet_ShouldIndentNestedBlockContentPerLevel()
    {
        XmlDocFormatOptions options = CreateOptions().WithIndentText("  ");

        var input =
            "/// <remarks>\r\n" +
            "/// <para>\r\n" +
            "/// Text.\r\n" +
            "/// </para>\r\n" +
            "/// </remarks>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: string.Empty), options);

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <remarks>\r\n" +
            "///   <para>\r\n" +
            "///     Text.\r\n" +
            "///   </para>\r\n" +
            "/// </remarks>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that the indented layout produced under a non-empty <see cref="XmlDocFormatOptions.IndentText" />
    /// is itself a fixed point: re-formatting it leaves it unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenIndentTextSetAndAlreadyIndented_ShouldBeIdempotent()
    {
        XmlDocFormatOptions options = CreateOptions().WithIndentText("  ");

        var indented =
            "/// <remarks>\r\n" +
            "///   <para>\r\n" +
            "///     Text.\r\n" +
            "///   </para>\r\n" +
            "/// </remarks>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(indented, CreateContext(baseIndent: string.Empty), options);

        Assert.IsFalse(result.Changed, "Indented content should be a fixed point under the same IndentText policy.");
        Assert.AreEqual(indented, result.FormattedText);
    }

    /// <summary>
    /// Verifies that with the default (empty) <see cref="XmlDocFormatOptions.IndentText" /> nested block content
    /// stays flush with its enclosing tag — the contrast case confirming the default layout is unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenIndentTextDefault_ShouldKeepNestedContentFlush()
    {
        var input =
            "/// <remarks>\r\n" +
            "/// <para>\r\n" +
            "/// Text.\r\n" +
            "/// </para>\r\n" +
            "/// </remarks>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(baseIndent: string.Empty), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }
}
