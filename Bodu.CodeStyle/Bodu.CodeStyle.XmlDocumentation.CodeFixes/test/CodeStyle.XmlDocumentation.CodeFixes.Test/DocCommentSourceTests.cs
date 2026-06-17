// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocCommentSourceTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.CodeFixes.Test;

[TestClass]
public sealed class DocCommentSourceTests
{
    /// <summary>
    /// Verifies that a carriage-return / line-feed pair is detected as the document line ending.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenCarriageReturnLineFeed_ShouldReturnCrLf()
    {
        Assert.AreEqual("\r\n", DocCommentSource.ResolveLineEnding(SourceText.From("a\r\nb")));
    }

    /// <summary>
    /// Verifies that a bare line feed is detected as the document line ending.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenLineFeed_ShouldReturnLf()
    {
        Assert.AreEqual("\n", DocCommentSource.ResolveLineEnding(SourceText.From("a\nb")));
    }

    /// <summary>
    /// Verifies that a bare carriage return is detected as the document line ending.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenCarriageReturn_ShouldReturnCr()
    {
        Assert.AreEqual("\r", DocCommentSource.ResolveLineEnding(SourceText.From("a\rb")));
    }

    /// <summary>
    /// Verifies that a single-line document with no terminator falls back to the canonical CRLF ending.
    /// </summary>
    [TestMethod]
    public void ResolveLineEnding_WhenNoTerminator_ShouldDefaultToCrLf()
    {
        Assert.AreEqual("\r\n", DocCommentSource.ResolveLineEnding(SourceText.From("abc")));
    }

    /// <summary>
    /// Verifies that the start of the line containing the supplied position is located just past the preceding
    /// terminator.
    /// </summary>
    [TestMethod]
    public void FindLineStart_WhenPositionOnSecondLine_ShouldReturnStartOfThatLine()
    {
        var text = SourceText.From("first\r\nsecond");

        Assert.AreEqual(7, DocCommentSource.FindLineStart(text, 10));
    }

    /// <summary>
    /// Verifies that the line end including its terminator is located just past the trailing newline.
    /// </summary>
    [TestMethod]
    public void FindLineEndIncludingTerminator_WhenCrLf_ShouldReturnIndexPastNewline()
    {
        var text = SourceText.From("first\r\nsecond");

        Assert.AreEqual(7, DocCommentSource.FindLineEndIncludingTerminator(text, 2));
    }

    /// <summary>
    /// Verifies that the leading whitespace run of the line containing the supplied position is returned as the
    /// indentation.
    /// </summary>
    [TestMethod]
    public void ExtractIndent_WhenDocCommentLine_ShouldReturnLeadingWhitespace()
    {
        var text = SourceText.From("class C\r\n    /// <summary>x</summary>\r\n    void M() { }");

        Assert.AreEqual("    ", DocCommentSource.ExtractIndent(text, 14));
    }

    /// <summary>
    /// Verifies that the standard <c>/// </c> prefix (with its single trailing space) is recovered from the
    /// line containing the supplied position.
    /// </summary>
    [TestMethod]
    public void DetectPrefix_WhenStandardPrefix_ShouldIncludeTrailingSpace()
    {
        var text = SourceText.From("    /// <summary>x</summary>");

        Assert.AreEqual("/// ", DocCommentSource.DetectPrefix(text, 8));
    }

    /// <summary>
    /// Verifies that a <c>///</c> prefix with no following space is recovered without a trailing space.
    /// </summary>
    [TestMethod]
    public void DetectPrefix_WhenNoSpaceAfterPrefix_ShouldExcludeTrailingSpace()
    {
        var text = SourceText.From("    ///<summary>x</summary>");

        Assert.AreEqual("///", DocCommentSource.DetectPrefix(text, 8));
    }

    /// <summary>
    /// Verifies that a line without a <c>///</c> prefix falls back to the canonical <c>/// </c> prefix.
    /// </summary>
    [TestMethod]
    public void DetectPrefix_WhenNoDocPrefix_ShouldFallBackToDefault()
    {
        var text = SourceText.From("    var x = 5;");

        Assert.AreEqual("/// ", DocCommentSource.DetectPrefix(text, 8));
    }
}
