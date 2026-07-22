// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniWriterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Provides configuration for a <see cref="Utf8IniWriter" />, controlling the comment prefix.
/// </summary>
public readonly struct IniWriterOptions
{
    /// <summary>
    /// Gets the default writer options (semicolon comment prefix).
    /// </summary>
    /// <value>The default options.</value>
    public static IniWriterOptions Default => default;

    /// <summary>
    /// Gets the comment prefix character, or <c>'\0'</c> to use the default <c>';'</c>.
    /// </summary>
    /// <value>The comment prefix character.</value>
    public char CommentPrefix { get; init; }

    /// <summary>
    /// Gets the effective comment prefix, resolving the unset default to a semicolon.
    /// </summary>
    /// <value>The comment prefix character.</value>
    internal char EffectiveCommentPrefix => CommentPrefix == '\0' ? ';' : CommentPrefix;
}
