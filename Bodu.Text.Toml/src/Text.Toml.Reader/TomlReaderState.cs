// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Identifies the lexical context a <see cref="Utf8TomlReader" /> resumes from on its next read, which determines whether
/// the bytes at the cursor are interpreted as a key, a value, or structural punctuation.
/// </summary>
/// <remarks>
/// TOML is lexically context-sensitive: the same bytes lex as a bare key in key position and as an integer in value
/// position. The lexer tracks that context itself — it is a property of the grammar's productions, not of the
/// document's table structure — so consumers receive correctly classified tokens from a plain forward scan.
/// </remarks>
internal enum TomlReaderState
{
    /// <summary>
    /// At the top level, before an expression: a table header, a key/value pair, a comment, or a blank line.
    /// </summary>
    Expression,

    /// <summary>
    /// Inside a <c>[table]</c> or <c>[[array-of-tables]]</c> header, expecting a key segment.
    /// </summary>
    HeaderPath,

    /// <summary>
    /// Expecting a key segment of a key/value pair, at the top level or inside an inline table.
    /// </summary>
    KeyPath,

    /// <summary>
    /// Expecting a value, after an equals sign.
    /// </summary>
    Value,

    /// <summary>
    /// Inside an array, expecting a value or the closing bracket.
    /// </summary>
    ArrayBody,

    /// <summary>
    /// Inside an inline table, expecting a key or the closing brace.
    /// </summary>
    InlineBody,

    /// <summary>
    /// After a completed value; the continuation depends on the enclosing container.
    /// </summary>
    AfterValue,

    /// <summary>
    /// After a completed top-level expression, expecting only whitespace, a comment, and a newline or the end of input.
    /// </summary>
    LineEnd,
}
