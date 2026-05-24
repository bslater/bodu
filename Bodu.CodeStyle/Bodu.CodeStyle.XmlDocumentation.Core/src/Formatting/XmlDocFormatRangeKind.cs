// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatRangeKind.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Identifies the category of a layout change applied by <see cref="XmlDocFormatter" />.
/// </summary>
public enum XmlDocFormatRangeKind
{
    /// <summary>
    /// The documentation line prefix or indent was normalised.
    /// </summary>
    LinePrefix = 0,

    /// <summary>
    /// A block tag was expanded onto its own lines or its body was re-indented.
    /// </summary>
    BlockLayout = 1,

    /// <summary>
    /// A long line was wrapped at a token boundary.
    /// </summary>
    LineWrap = 2,

    /// <summary>
    /// The trailing space before <c>/&gt;</c> on a self-closing tag was added or removed.
    /// </summary>
    SelfClosingTagSpacing = 3,

    /// <summary>
    /// Prose whitespace was collapsed to a single space.
    /// </summary>
    WhitespaceNormalisation = 4,

    /// <summary>
    /// Trailing whitespace was stripped from a line.
    /// </summary>
    TrailingWhitespace = 5,

    /// <summary>
    /// A blank documentation line was added, removed, or normalised.
    /// </summary>
    BlankLine = 6,
}
