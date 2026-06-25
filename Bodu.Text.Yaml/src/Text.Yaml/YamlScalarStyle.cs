// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlScalarStyle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Specifies the presentation style of a YAML scalar node. The style controls how a scalar's content is delimited and
/// escaped when emitted, and records how it was delimited when read.
/// </summary>
/// <remarks>
/// The five styles correspond to the scalar presentations defined by the YAML specification. The two block styles (<see cref="Literal" />
/// and <see cref="Folded" />) are valid only within block context; the flow styles (<see cref="Plain" />,
/// <see cref="SingleQuoted" />, and <see cref="DoubleQuoted" />) are valid in both block and flow context.
/// </remarks>
public enum YamlScalarStyle
{
    /// <summary>
    /// Lets the writer select the most compact valid style for the content. Used as the default when a style is not
    /// specified.
    /// </summary>
    Any = 0,

    /// <summary>
    /// The plain (unquoted) style. The content carries no delimiters and is subject to implicit type resolution.
    /// </summary>
    Plain = 1,

    /// <summary>
    /// The single-quoted style. Only the single quote is escaped, by doubling; no other escape sequences are processed.
    /// </summary>
    SingleQuoted = 2,

    /// <summary>
    /// The double-quoted style. Supports the full set of backslash escape sequences.
    /// </summary>
    DoubleQuoted = 3,

    /// <summary>
    /// The literal block style (<c>|</c>). Line breaks within the content are preserved verbatim.
    /// </summary>
    Literal = 4,

    /// <summary>
    /// The folded block style (<c>&gt;</c>). Single line breaks are folded into spaces, with blank lines preserved.
    /// </summary>
    Folded = 5,
}
