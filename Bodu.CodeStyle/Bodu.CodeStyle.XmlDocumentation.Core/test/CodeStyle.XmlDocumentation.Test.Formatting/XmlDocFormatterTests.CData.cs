// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.CData.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
        var outputLines = result.FormattedText.Split(new[] { "\r\n" }, System.StringSplitOptions.None);
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

    /// <summary>
    /// Verifies that a <c>&lt;para&gt;</c> block whose prose is followed by a <c>&lt;code&gt;</c> element on
    /// its own line — the pattern that triggered BODU1002 — is considered canonical and round-trips unchanged.
    /// Before the fix, the formatter packed <c>&lt;code&gt;</c> inline onto the preceding prose line, causing
    /// the analyser to report a difference and the code-fix provider to produce incorrect output.
    /// </summary>
    [TestMethod]
    public void Format_WhenParaHasProseThenCodeBlockWithMultiLineCData_ShouldLeaveCodeTagOnOwnLine()
    {
        // Base indent is empty to match the real-world shape of a file-scoped class doc comment
        // (e.g. GcmSivModeTransform.cs). The content budget is 120 - 0 - 4 = 116 chars, which
        // accommodates the 113-char prose line without rewrapping. With a 4-char base indent the
        // budget drops to 112 and the prose would be reformatted — that is a correct reformat, not
        // the bug; the bug was specifically <code> being pulled onto the preceding prose line.
        var input =
            "/// <remarks>\r\n" +
            "/// <para>\r\n" +
            "/// GCM-SIV derives per-message authentication and encryption keys from the master key and a 12-byte nonce using four\r\n" +
            "/// cipher calls with little-endian counters (RFC 8452 Section 4):\r\n" +
            "/// <code>\r\n" +
            "///<![CDATA[\r\n" +
            "/// K_auth = E_K(LE32(0) || nonce)[0..7] || E_K(LE32(1) || nonce)[0..7]   (16 bytes)\r\n" +
            "/// K_enc  = E_K(LE32(2) || nonce)[0..7] || E_K(LE32(3) || nonce)[0..7]   (16 bytes)\r\n" +
            "///]]>\r\n" +
            "/// </code>\r\n" +
            "/// </para>\r\n" +
            "/// </remarks>\r\n";

        var formatter = new XmlDocFormatter();
        var context = new XmlDocFormatContext("", "\r\n", XmlDocMemberKindHint.Unknown);
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocFormatResult result = formatter.FormatTrivia(input, context, options);

        Assert.IsFalse(result.Changed, $"Formatter changed a canonical <code> block. Formatted text was:\n{result.FormattedText}");
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that a <c>&lt;code&gt;</c> element written inline after prose (the broken form that the
    /// old code-fix provider produced) is moved to its own line, with the close tag likewise on its own line.
    /// </summary>
    [TestMethod]
    public void Format_WhenCodeTagInlinedAfterProse_ShouldMoveCodeTagToOwnLine()
    {
        var input =
            "/// <remarks>\r\n" +
            "    /// <para>\r\n" +
            "    /// Key derivation (RFC 8452 Section 4): <code>\r\n" +
            "    ///<![CDATA[\r\n" +
            "    /// K_auth = E_K(LE32(0) || nonce)[0..7]\r\n" +
            "    ///]]>\r\n" +
            "    /// </code>\r\n" +
            "    /// </para>\r\n" +
            "    /// </remarks>\r\n";

        var expected =
            "/// <remarks>\r\n" +
            "    /// <para>\r\n" +
            "    /// Key derivation (RFC 8452 Section 4):\r\n" +
            "    /// <code>\r\n" +
            "    ///<![CDATA[\r\n" +
            "    /// K_auth = E_K(LE32(0) || nonce)[0..7]\r\n" +
            "    ///]]>\r\n" +
            "    /// </code>\r\n" +
            "    /// </para>\r\n" +
            "    /// </remarks>\r\n";

        var formatter = new XmlDocFormatter();
        var context = new XmlDocFormatContext("    ", "\r\n", XmlDocMemberKindHint.Unknown);
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocFormatResult result = formatter.FormatTrivia(input, context, options);

        Assert.IsTrue(result.Changed, "Formatter should have moved the inline <code> tag to its own line.");
        Assert.AreEqual(expected, result.FormattedText);
    }

    /// <summary>
    /// Verifies that a content line beginning with <c>&lt;![CDATA[</c> followed by body text on the same
    /// line (the shape produced when prior reflow has collapsed a multi-line CDATA payload onto one line)
    /// receives the no-space prefix. Without this rule the formatter would emit
    /// <c>/// &lt;![CDATA[ …</c>, tripping the sibling BODU1405 analyzer which requires the opener to
    /// butt directly against <c>///</c>.
    /// </summary>
    [TestMethod]
    public void Format_WhenLineStartsWithCDataOpenerAndBody_ShouldUseNoSpacePrefix()
    {
        var input =
            "/// <example>\r\n" +
            "    /// <code language=\"csharp\">\r\n" +
            "    ///<![CDATA[ int volume = 11; // Replace explicit min/max calls with a fluent clamp. int safeVolume =\r\n" +
            "    /// volume.Clamp(0, 10); // => 10 // Inclusive \"between\" predicate, ideal in guard clauses. bool inDecimalDigits =\r\n" +
            "    /// '7'.IsBetween('0', '9'); // => true ]]>\r\n" +
            "    /// </code>\r\n" +
            "    /// </example>\r\n";

        var formatter = new XmlDocFormatter();
        var context = new XmlDocFormatContext("    ", "\r\n", XmlDocMemberKindHint.Unknown);
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocFormatResult result = formatter.FormatTrivia(input, context, options);

        StringAssert.Contains(result.FormattedText, "///<![CDATA[");
        Assert.IsFalse(result.FormattedText.Contains("/// <![CDATA["),
            $"Output contains '/// <![CDATA[' with a space (BODU1405 would fire). Formatted text:\n{result.FormattedText}");
    }
}
