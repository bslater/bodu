// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTokenizerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Bodu.CodeStyle.XmlDocumentation.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Tokens;

[TestClass]
public sealed class XmlDocTokenizerTests
{
    private static readonly ImmutableHashSet<string> s_inlineTags = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "c",
        "see",
        "paramref",
        "typeparamref");

    /// <summary>
    /// Verifies that <see cref="XmlDocTokenizer.Tokenize" /> throws when the content is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenContentIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = XmlDocTokenizer.Tokenize(null!, s_inlineTags);
        });

        Assert.AreEqual("content", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocTokenizer.Tokenize" /> throws when the inline-tag set is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenInlineTagsIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = XmlDocTokenizer.Tokenize("foo", null!);
        });

        Assert.AreEqual("inlineTags", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty input yields an empty token array.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenContentIsEmpty_ShouldReturnEmpty()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize(string.Empty, s_inlineTags);

        Assert.AreEqual(0, tokens.Length);
    }

    /// <summary>
    /// Verifies that a plain text input emits a single text token.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenContentIsPlainText_ShouldEmitTextToken()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("foo bar baz", s_inlineTags);

        Assert.IsTrue(tokens.Any(t => t.Kind == XmlDocTokenKind.Text));
        Assert.IsTrue(tokens.Any(t => t.Kind == XmlDocTokenKind.Whitespace));
    }

    /// <summary>
    /// Verifies that an opening block tag with attributes emits a single block-start token carrying the tag
    /// name.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenBlockTagWithAttributes_ShouldEmitBlockStartToken()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("<param name=\"x\">", s_inlineTags);

        Assert.AreEqual(1, tokens.Length);
        Assert.AreEqual(XmlDocTokenKind.BlockStart, tokens[0].Kind);
        Assert.AreEqual("param", tokens[0].TagName);
        Assert.IsFalse(tokens[0].IsSelfClosing);
    }

    /// <summary>
    /// Verifies that a self-closing tag is emitted as a single inline atomic token marked self-closing.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenSelfClosingTagWithAttributes_ShouldEmitInlineSelfClosingToken()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("<see cref=\"X\" />", s_inlineTags);

        Assert.AreEqual(1, tokens.Length);
        Assert.AreEqual(XmlDocTokenKind.InlineXml, tokens[0].Kind);
        Assert.AreEqual("see", tokens[0].TagName);
        Assert.IsTrue(tokens[0].IsSelfClosing);
        Assert.AreEqual("<see cref=\"X\" />", tokens[0].RawText);
    }

    /// <summary>
    /// Verifies that a paired inline tag (e.g. <c>&lt;c&gt;foo&lt;/c&gt;</c>) becomes a single atomic token.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenInlinePairTag_ShouldEmitSingleAtomicToken()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("<c>foo bar</c>", s_inlineTags);

        Assert.AreEqual(1, tokens.Length);
        Assert.AreEqual(XmlDocTokenKind.InlineXml, tokens[0].Kind);
        Assert.AreEqual("c", tokens[0].TagName);
        Assert.IsFalse(tokens[0].IsSelfClosing);
        Assert.AreEqual("<c>foo bar</c>", tokens[0].RawText);
    }

    /// <summary>
    /// Verifies that a closing tag emits a block-end token.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenClosingTag_ShouldEmitBlockEndToken()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("</summary>", s_inlineTags);

        Assert.AreEqual(1, tokens.Length);
        Assert.AreEqual(XmlDocTokenKind.BlockEnd, tokens[0].Kind);
        Assert.AreEqual("summary", tokens[0].TagName);
    }

    /// <summary>
    /// Verifies that a single-line CDATA section is preserved verbatim as a single <see cref="XmlDocTokenKind.CData" />
    /// token so the layout pass can recognise it and bypass prose normalisation for its body.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenCDataSection_ShouldEmitCDataTokenVerbatim()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("<![CDATA[var x = new Foo<int>();]]>", s_inlineTags);

        Assert.AreEqual(1, tokens.Length);
        Assert.AreEqual(XmlDocTokenKind.CData, tokens[0].Kind);
        Assert.AreEqual("<![CDATA[var x = new Foo<int>();]]>", tokens[0].RawText);
    }

    /// <summary>
    /// Verifies that a multi-line CDATA section emits a single <see cref="XmlDocTokenKind.CData" /> token
    /// whose <c>RawText</c> includes the internal newlines unchanged.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenMultiLineCDataSection_ShouldEmitSingleCDataToken()
    {
        var content = "<![CDATA[\nvar buffer = new Foo();\nbuffer.Enqueue(1);\n]]>";

        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize(content, s_inlineTags);

        Assert.AreEqual(1, tokens.Length);
        Assert.AreEqual(XmlDocTokenKind.CData, tokens[0].Kind);
        Assert.AreEqual(content, tokens[0].RawText);
    }

    /// <summary>
    /// Verifies that an XML comment inside content is preserved verbatim as a text token.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenXmlComment_ShouldEmitTextTokenVerbatim()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("<!-- note -->", s_inlineTags);

        Assert.AreEqual(1, tokens.Length);
        Assert.AreEqual(XmlDocTokenKind.Text, tokens[0].Kind);
        Assert.AreEqual("<!-- note -->", tokens[0].RawText);
    }

    /// <summary>
    /// Verifies that a line feed in the content yields a line-break token.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenContentContainsLineFeed_ShouldEmitLineBreakToken()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("foo\nbar", s_inlineTags);

        Assert.IsTrue(tokens.Any(t => t.Kind == XmlDocTokenKind.LineBreak));
    }

    /// <summary>
    /// Verifies that a CR (no LF) is skipped because line-ending re-emission is the caller's job.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenContentContainsCarriageReturn_ShouldNotEmitToken()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("foo\rbar", s_inlineTags);

        Assert.IsFalse(tokens.Any(t => t.Kind == XmlDocTokenKind.LineBreak));
    }

    /// <summary>
    /// Verifies that an unbalanced inline tag (no matching closer) falls through to a block-start token.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenInlineTagHasNoClose_ShouldFallBackToBlockStart()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("<c>foo bar baz", s_inlineTags);

        XmlDocToken open = tokens.First(t => t.TagName == "c");
        Assert.AreEqual(XmlDocTokenKind.BlockStart, open.Kind);
    }

    /// <summary>
    /// Verifies that a literal <c>&lt;</c> with no valid tag name is preserved as text.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenContentContainsLooseLessThan_ShouldPreserveAsText()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("a < b", s_inlineTags);

        Assert.IsTrue(tokens.Any(t => t.Kind == XmlDocTokenKind.Text && t.RawText.Contains('<')));
    }

    /// <summary>
    /// Verifies that an inline tag's closing match respects element-name boundaries: <c>&lt;c&gt;</c> is not
    /// bundled as an inline atom against an unrelated longer-named close tag (<c>&lt;/code&gt;</c>), which
    /// merely shares the <c>&lt;/c</c> prefix.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenInlineCloseSharesPrefixWithLongerName_ShouldNotBundle()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("<c>x</code>", s_inlineTags);

        XmlDocToken open = tokens.First(t => t.TagName == "c");
        Assert.AreEqual(XmlDocTokenKind.BlockStart, open.Kind);
        Assert.IsFalse(
            tokens.Any(t => t.Kind == XmlDocTokenKind.InlineXml && t.RawText.Contains("</code>")),
            "The inline matcher must not consume an unrelated </code> close tag.");
    }

    /// <summary>
    /// Verifies that a tag whose name begins with an invalid start character (a digit) is not recognised as a
    /// tag and is preserved as literal text.
    /// </summary>
    [TestMethod]
    public void Tokenize_WhenTagNameStartsWithDigit_ShouldPreserveAsText()
    {
        ImmutableArray<XmlDocToken> tokens = XmlDocTokenizer.Tokenize("<1tag>x</1tag>", s_inlineTags);

        Assert.IsFalse(
            tokens.Any(t => t.Kind == XmlDocTokenKind.BlockStart || t.Kind == XmlDocTokenKind.InlineXml || t.Kind == XmlDocTokenKind.BlockEnd),
            "A name starting with a digit is not a valid element name and must be treated as text.");
        Assert.IsTrue(tokens.Any(t => t.Kind == XmlDocTokenKind.Text && t.RawText.Contains('<')));
    }
}
