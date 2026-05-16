// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTokenTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.CodeStyle.XmlDocumentation.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Tokens;

[TestClass]
public sealed class XmlDocTokenTests
{
    /// <summary>
    /// Verifies that the constructor throws when the raw text is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRawTextIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlDocToken(XmlDocTokenKind.Text, null!, tagName: null, isSelfClosing: false);
        });

        Assert.AreEqual("rawText", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocToken.Text" /> returns a token with the expected kind.
    /// </summary>
    [TestMethod]
    public void Text_ShouldReturnTextToken()
    {
        XmlDocToken token = XmlDocToken.Text("foo");

        Assert.AreEqual(XmlDocTokenKind.Text, token.Kind);
        Assert.AreEqual("foo", token.RawText);
        Assert.IsNull(token.TagName);
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocToken.Whitespace" /> returns a token with the expected kind.
    /// </summary>
    [TestMethod]
    public void Whitespace_ShouldReturnWhitespaceToken()
    {
        XmlDocToken token = XmlDocToken.Whitespace("  ");

        Assert.AreEqual(XmlDocTokenKind.Whitespace, token.Kind);
        Assert.AreEqual("  ", token.RawText);
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocToken.LineBreak" /> returns a token with the line-break kind.
    /// </summary>
    [TestMethod]
    public void LineBreak_ShouldReturnLineBreakToken()
    {
        XmlDocToken token = XmlDocToken.LineBreak();

        Assert.AreEqual(XmlDocTokenKind.LineBreak, token.Kind);
    }
}
