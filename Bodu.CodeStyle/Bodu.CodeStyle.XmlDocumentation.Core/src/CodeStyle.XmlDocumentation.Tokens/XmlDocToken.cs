// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocToken.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.CodeStyle.XmlDocumentation.Tokens;

/// <summary>
/// Represents a single token produced by <see cref="XmlDocTokenizer" /> when decomposing the prose content of an
/// XML documentation comment.
/// </summary>
internal sealed class XmlDocToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocToken" /> class.
    /// </summary>
    /// <param name="kind">The token kind.</param>
    /// <param name="rawText">The verbatim text the token represents.</param>
    /// <param name="tagName">The tag name when <paramref name="kind" /> identifies a block or inline tag; otherwise <see langword="null" />.</param>
    /// <param name="isSelfClosing">Indicates whether an inline token was emitted in self-closing form (<c>&lt;tag /&gt;</c>).</param>
    public XmlDocToken(XmlDocTokenKind kind, string rawText, string? tagName, bool isSelfClosing)
    {
        if (rawText is null) throw new ArgumentNullException(nameof(rawText));

        this.Kind = kind;
        this.RawText = rawText;
        this.TagName = tagName;
        this.IsSelfClosing = isSelfClosing;
    }

    /// <summary>
    /// Gets the kind of this token.
    /// </summary>
    /// <value>The token kind.</value>
    public XmlDocTokenKind Kind { get; }

    /// <summary>
    /// Gets the verbatim text the token represents.
    /// </summary>
    /// <value>The raw text of the token.</value>
    public string RawText { get; }

    /// <summary>
    /// Gets the tag name associated with this token when it is a block or inline XML token.
    /// </summary>
    /// <value>The tag name, or <see langword="null" /> when the token does not represent an XML element.</value>
    public string? TagName { get; }

    /// <summary>
    /// Gets a value indicating whether an inline token was emitted in self-closing form.
    /// </summary>
    /// <value><see langword="true" /> when the source used the <c>&lt;tag /&gt;</c> form; otherwise <see langword="false" />.</value>
    public bool IsSelfClosing { get; }

    /// <summary>
    /// Returns a token representing a text run.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <returns>A new text token.</returns>
    public static XmlDocToken Text(string text) =>
        new XmlDocToken(XmlDocTokenKind.Text, text, tagName: null, isSelfClosing: false);

    /// <summary>
    /// Returns a token representing a whitespace run.
    /// </summary>
    /// <param name="whitespace">The whitespace content.</param>
    /// <returns>A new whitespace token.</returns>
    public static XmlDocToken Whitespace(string whitespace) =>
        new XmlDocToken(XmlDocTokenKind.Whitespace, whitespace, tagName: null, isSelfClosing: false);

    /// <summary>
    /// Returns a token representing a physical line break.
    /// </summary>
    /// <returns>A new line-break token using a logical newline marker.</returns>
    public static XmlDocToken LineBreak() =>
        new XmlDocToken(XmlDocTokenKind.LineBreak, "\n", tagName: null, isSelfClosing: false);

    /// <summary>
    /// Returns a token representing a <c>&lt;![CDATA[...]]&gt;</c> section captured verbatim.
    /// </summary>
    /// <param name="rawText">The full CDATA literal, including the open and close delimiters and any internal newlines.</param>
    /// <returns>A new CData token.</returns>
    public static XmlDocToken CData(string rawText) =>
        new XmlDocToken(XmlDocTokenKind.CData, rawText, tagName: null, isSelfClosing: false);
}
