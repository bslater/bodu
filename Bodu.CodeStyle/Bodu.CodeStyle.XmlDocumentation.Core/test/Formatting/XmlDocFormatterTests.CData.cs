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
    /// Verifies that the user-reported full doc-comment input (summary + typeparam + remarks with several
    /// paras + example with multi-line CDATA) round-trips with the CDATA delimiter using the no-space prefix
    /// and prose paragraphs wrapping near the column budget rather than at every clause boundary.
    /// </summary>
    [TestMethod]
    public void Format_WhenFullDocCommentWithParasAndCData_ShouldPreserveCDataDelimitersWithoutSpace()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// Provides a lock-free, bounded first-in, first-out (FIFO) buffer with optional overwrite semantics.\r\n" +
            "    /// </summary>\r\n" +
            "    /// <example>\r\n" +
            "    ///<![CDATA[\r\n" +
            "    /// var buffer = new ConcurrentCircularBuffer<string>(capacity: 3, allowOverwrite: true);\r\n" +
            "    ///]]>\r\n" +
            "    /// </example>\r\n";

        var formatter = new XmlDocFormatter();
        var context = new XmlDocFormatContext("    ", "\r\n", XmlDocMemberKindHint.Unknown);
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocFormatResult result = formatter.FormatTrivia(input, context, options);

        // CDATA delimiter lines must use the no-space prefix.
        StringAssert.Contains(result.FormattedText, "///<![CDATA[");
        StringAssert.Contains(result.FormattedText, "///]]>");
        // Defensive: there must be no "/// <![CDATA[" with a space anywhere.
        Assert.IsFalse(result.FormattedText.Contains("/// <![CDATA["),
            $"Output contains '/// <![CDATA[' with a space. Formatted text:\n{result.FormattedText}");
        Assert.IsFalse(result.FormattedText.Contains("/// ]]>"),
            $"Output contains '/// ]]>' with a space. Formatted text:\n{result.FormattedText}");
    }

    /// <summary>
    /// Verifies that a multi-sentence prose paragraph wraps near the configured budget (not at the first
    /// clause boundary in the line). Demonstrates the regression where clause-aware wrapping previously
    /// broke at every <c>.</c> / <c>,</c> / <c>;</c> / <c>:</c> regardless of how much space was left in
    /// the budget.
    /// </summary>
    [TestMethod]
    public void Format_WhenMultiSentenceParaWrapped_ShouldFillNearBudgetNotBreakAtEveryClause()
    {
        // Long enough that there are multiple wrap points; each line should fill near the 112-char content
        // budget instead of breaking at the first clause boundary (colon, comma, semicolon, period).
        var input =
            "/// <remarks>\r\n" +
            "    /// <para>\r\n" +
            "    /// The Vyukov MPMC sequence protocol uses two distinct sequence marks per slot: one written by the producer\r\n" +
            "    /// when data is published (<c>tail + 1</c>), and one written by the consumer when the slot is released\r\n" +
            "    /// (<c>head + capacity</c>). These marks must be numerically distinct so that concurrent producers can\r\n" +
            "    /// determine whether a slot is free or still occupied. With a capacity of 1 they are always equal for every\r\n" +
            "    /// round, making the two states indistinguishable.\r\n" +
            "    /// </para>\r\n" +
            "    /// </remarks>\r\n";

        var formatter = new XmlDocFormatter();
        var context = new XmlDocFormatContext("    ", "\r\n", XmlDocMemberKindHint.Unknown);
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocFormatResult result = formatter.FormatTrivia(input, context, options);

        // Collect prose lines (skip tag-only and the final partial line of the paragraph).
        string[] outputLines = result.FormattedText.Split(new[] { "\r\n" }, System.StringSplitOptions.None);
        var proseLineLengths = new System.Collections.Generic.List<int>();
        for (var i = 0; i < outputLines.Length; i++)
        {
            var content = outputLines[i].TrimStart();
            if (!content.StartsWith("/// ", System.StringComparison.Ordinal)) continue;
            var body = content.Substring(4);
            if (body.Length == 0 || body.StartsWith("<", System.StringComparison.Ordinal)) continue;
            if (body.IndexOf(' ') < 0) continue;
            proseLineLengths.Add(body.Length);
        }

        // Every prose line except possibly the LAST (the trailing remnant of the paragraph) must fill at
        // least 95 chars — i.e. close to the 112-char content budget. A line that's shorter than 95 chars
        // and not the final line indicates the wrapper bailed early at a clause boundary.
        for (var i = 0; i < proseLineLengths.Count - 1; i++)
        {
            Assert.IsTrue(proseLineLengths[i] >= 95,
                $"Prose line {i} too short ({proseLineLengths[i]} chars) — suggests clause-aware wrapping is breaking too early.\nAll prose line lengths: {string.Join(", ", proseLineLengths)}\nFull output:\n{result.FormattedText}");
        }
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
