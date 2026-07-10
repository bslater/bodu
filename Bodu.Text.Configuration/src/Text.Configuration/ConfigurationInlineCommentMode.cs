// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationInlineCommentMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how the reader treats <c>#</c> or <c>;</c> characters that appear after a property value.
/// </summary>
/// <remarks>
/// EditorConfig 0.17.2 explicitly forbids inline comments — a <c>#</c> or <c>;</c> anywhere other than the start of a
/// line is part of the value. Bodu permits inline comments as an opt-in extension.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // value = 8 # comment  -- WhitespaceIntroduced treats "# comment" as a comment
/// // only when preceded by whitespace; Disabled keeps it as part of the value.
/// var options = new ConfigurationParseOptions
/// {
///     InlineCommentMode = ConfigurationInlineCommentMode.WhitespaceIntroduced,
/// };
///]]>
/// </code>
/// </example>
/// </remarks>
public enum ConfigurationInlineCommentMode
{
    /// <summary>
    /// Inline comments are not recognized. Any <c>#</c> or <c>;</c> that appears in a value is preserved verbatim. This
    /// is the EditorConfig-compatible setting.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Recognize an inline comment when, and only when, the comment prefix is preceded by at least one ASCII whitespace
    /// character. This is the default Bodu behaviour and minimizes the risk of accidentally truncating values that
    /// legitimately contain <c>#</c> or <c>;</c>.
    /// </summary>
    WhitespaceIntroduced = 1,

    /// <summary>
    /// Always treat the first unescaped <c>#</c> or <c>;</c> after the value separator as the start of a comment, even
    /// when not preceded by whitespace. Surrounding whitespace in the captured value is trimmed.
    /// </summary>
    Always = 2,
}
