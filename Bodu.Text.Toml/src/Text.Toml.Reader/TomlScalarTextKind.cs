// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlScalarTextKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Identifies how the raw bytes of a <see cref="TomlLexer" /> text token are decoded into a string when
/// <see cref="TomlLexer.GetString" /> is called.
/// </summary>
internal enum TomlScalarTextKind
{
    /// <summary>
    /// The current token carries no text.
    /// </summary>
    None,

    /// <summary>
    /// The raw bytes are the text: a bare key, a literal string, a multi-line literal string (its leading newline
    /// already trimmed), or a comment. Decoding is a direct UTF-8 transcode.
    /// </summary>
    Verbatim,

    /// <summary>
    /// A single-line basic string whose escape sequences are resolved during decoding.
    /// </summary>
    Basic,

    /// <summary>
    /// A multi-line basic string whose escape sequences and line-ending backslashes are resolved during decoding (its
    /// leading newline already trimmed).
    /// </summary>
    MultilineBasic,
}
