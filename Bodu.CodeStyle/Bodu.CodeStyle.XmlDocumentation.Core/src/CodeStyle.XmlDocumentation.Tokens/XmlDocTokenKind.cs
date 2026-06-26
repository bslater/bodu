// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTokenKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.XmlDocumentation.Tokens;

/// <summary>
/// Identifies the kind of a token produced by <see cref="XmlDocTokenizer" />.
/// </summary>
internal enum XmlDocTokenKind
{
    /// <summary>
    /// Indicates an opening block tag, e.g. <c>&lt;summary&gt;</c>.
    /// </summary>
    BlockStart = 0,

    /// <summary>
    /// Indicates a closing block tag, e.g. <c>&lt;/summary&gt;</c>.
    /// </summary>
    BlockEnd = 1,

    /// <summary>
    /// Indicates an inline atomic XML token, e.g. <c>&lt;see cref="X" /&gt;</c> or <c>&lt;c&gt;foo&lt;/c&gt;</c>.
    /// </summary>
    InlineXml = 2,

    /// <summary>
    /// Indicates a contiguous run of non-whitespace text outside any inline XML token.
    /// </summary>
    Text = 3,

    /// <summary>
    /// Indicates a contiguous run of inline whitespace inside a logical line.
    /// </summary>
    Whitespace = 4,

    /// <summary>
    /// Indicates a physical line break recorded in the input.
    /// </summary>
    LineBreak = 5,

    /// <summary>
    /// Indicates a <c>&lt;![CDATA[...]]&gt;</c> section. The token's <c>RawText</c> contains the entire CDATA literal,
    /// including its open and close delimiters and any internal newlines, captured verbatim. Downstream layout passes
    /// preserve the section's body byte-for-byte.
    /// </summary>
    CData = 6,
}
