// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.CData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public sealed class XmlDocFormatterCDataTests
{
    /// <summary>
    /// Verifies that a multi-line CDATA block inside an <c>&lt;example&gt;</c> tag survives the formatter
    /// without modification: every internal line keeps its <c>///</c> prefix, the <c>&lt;![CDATA[</c> and
    /// <c>]]&gt;</c> delimiters keep their no-space-after-slash form, blank doc lines stay as <c>///</c>,
    /// and the body content is byte-identical to the input.
    /// </summary>
    [TestMethod]
    public void Format_WhenCDataInsideExample_ShouldPreserveBodyVerbatim()
    {
        // Real-world doc trivia text (the form analyzer code passes to FormatTrivia): the first line has no
        // leading base indent because Roslyn keeps that on a separate WhitespaceTrivia; subsequent lines
        // carry the indent inside the trivia.
        var input =
            "/// <example>\r\n" +
            "    ///<![CDATA[\r\n" +
            "    /// var buffer = new ConcurrentCircularBuffer<string>(capacity: 3, allowOverwrite: true);\r\n" +
            "    /// buffer.Enqueue(\"A\");\r\n" +
            "    /// buffer.Enqueue(\"B\");\r\n" +
            "    /// buffer.Enqueue(\"C\");\r\n" +
            "    /// buffer.Enqueue(\"D\"); // \"A\" is evicted\r\n" +
            "    ///\r\n" +
            "    /// if (buffer.TryPeek(out var head))\r\n" +
            "    ///     Console.WriteLine(head); // \"B\"\r\n" +
            "    ///\r\n" +
            "    /// Console.WriteLine(buffer.Dequeue()); // \"B\"\r\n" +
            "    ///]]>\r\n" +
            "    /// </example>\r\n";

        var formatter = new XmlDocFormatter();
        var context = new XmlDocFormatContext("    ", "\r\n", XmlDocMemberKindHint.Unknown);
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocFormatResult result = formatter.FormatTrivia(input, context, options);

        Assert.IsFalse(result.Changed, $"Formatter changed CDATA content. Formatted text was:\n{result.FormattedText}");
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that the formatter still preserves the existing single-line CDATA-in-summary case (no
    /// internal newlines, no delimiter-on-its-own-line) to confirm the multi-line fix is a strict refinement.
    /// </summary>
    [TestMethod]
    public void Format_WhenInlineCDataInsideSummary_ShouldPreserveLine()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// Use <![CDATA[var x = new Foo<int>();]]> for X.\r\n" +
            "    /// </summary>\r\n";

        var formatter = new XmlDocFormatter();
        var context = new XmlDocFormatContext("    ", "\r\n", XmlDocMemberKindHint.Unknown);
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocFormatResult result = formatter.FormatTrivia(input, context, options);

        Assert.IsFalse(result.Changed, $"Formatter changed inline-CDATA content. Formatted text was:\n{result.FormattedText}");
        StringAssert.Contains(result.FormattedText, "<![CDATA[var x = new Foo<int>();]]>");
    }
}
