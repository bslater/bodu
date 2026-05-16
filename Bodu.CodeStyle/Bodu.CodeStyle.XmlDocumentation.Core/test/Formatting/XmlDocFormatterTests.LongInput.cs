// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.LongInput.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that a doc comment with hundreds of paragraphs survives formatting without exhausting stack
    /// space or throwing.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocContainsManyParaBlocks_ShouldEmitOneBlockPerParagraph()
    {
        var builder = new StringBuilder();
        builder.Append("/// <remarks>\r\n");
        for (var i = 0; i < 200; i++)
        {
            builder.Append("    /// <para>\r\n");
            builder.Append("    /// Paragraph ");
            builder.Append(i);
            builder.Append(".\r\n");
            builder.Append("    /// </para>\r\n");
        }

        builder.Append("    /// </remarks>\r\n");
        var input = builder.ToString();

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed, "Already-canonical input should not report changes.");
    }

    /// <summary>
    /// Verifies that an extremely long single-line input is wrapped without throwing or producing oversized
    /// content lines.
    /// </summary>
    [TestMethod]
    public void Format_WhenSummaryHasVeryLongProse_ShouldWrapToFit()
    {
        var builder = new StringBuilder();
        builder.Append("/// <summary>");
        for (var i = 0; i < 100; i++)
        {
            builder.Append("Lorem ipsum dolor sit amet ");
        }

        builder.Append("</summary>\r\n");

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(builder.ToString(), CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        foreach (var line in result.FormattedText.Split(["\r\n"], System.StringSplitOptions.None))
        {
            Assert.IsTrue(line.Length <= 200, $"Wrapped line is unexpectedly long: '{line}'.");
        }
    }
}
