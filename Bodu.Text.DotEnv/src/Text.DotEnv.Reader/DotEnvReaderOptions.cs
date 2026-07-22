// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Provides configuration for a <see cref="Utf8DotEnvReader" />, controlling how the reader interprets optional DotEnv
/// syntax conventions.
/// </summary>
/// <remarks>
/// The options are an immutable value type; assign the <see langword="init" />-only properties through an object
/// initializer and read <see cref="Default" /> for the standard behaviour.
/// </remarks>
public readonly struct DotEnvReaderOptions
{
    /// <summary>
    /// Gets the default reader options: the <c>export</c> prefix, inline comments, and comment preservation are all
    /// enabled.
    /// </summary>
    /// <value>The default options.</value>
    public static DotEnvReaderOptions Default => default;

    /// <summary>
    /// Gets a value indicating whether a leading <c>export</c> keyword before a key is recognized and stripped.
    /// </summary>
    /// <value><see langword="true" /> to accept the <c>export</c> prefix; otherwise <see langword="false" />.</value>
    /// <remarks>
    /// The property is inverted so the parameterless <see cref="Default" /> enables the prefix.
    /// </remarks>
    public bool DisallowExportPrefix { get; init; }

    /// <summary>
    /// Gets a value indicating whether a <c>#</c> preceded by whitespace terminates an unquoted value as an inline
    /// comment.
    /// </summary>
    /// <value><see langword="true" /> to disable inline comments; otherwise <see langword="false" />.</value>
    /// <remarks>
    /// The property is inverted so the parameterless <see cref="Default" /> enables inline comments.
    /// </remarks>
    public bool DisallowInlineComments { get; init; }

    /// <summary>
    /// Gets a value indicating whether comment lines are surfaced as <see cref="DotEnvTokenType.Comment" /> tokens.
    /// </summary>
    /// <value><see langword="true" /> to skip comment tokens; otherwise <see langword="false" />.</value>
    /// <remarks>
    /// The property is inverted so the parameterless <see cref="Default" /> preserves comments.
    /// </remarks>
    public bool SkipComments { get; init; }

    /// <summary>
    /// Gets a value indicating whether the <c>export</c> prefix is accepted.
    /// </summary>
    /// <value><see langword="true" /> when the prefix is accepted.</value>
    internal bool AllowExportPrefix => !DisallowExportPrefix;

    /// <summary>
    /// Gets a value indicating whether inline comments terminate unquoted values.
    /// </summary>
    /// <value><see langword="true" /> when inline comments are enabled.</value>
    internal bool AllowInlineComments => !DisallowInlineComments;

    /// <summary>
    /// Gets a value indicating whether comment tokens are surfaced.
    /// </summary>
    /// <value><see langword="true" /> when comments are preserved.</value>
    internal bool PreserveComments => !SkipComments;
}
