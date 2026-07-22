// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Provides configuration for a <see cref="Utf8IniReader" />, controlling comment recognition.
/// </summary>
/// <remarks>
/// The reader is source-order and does not itself resolve duplicate sections/keys or apply case rules — those are
/// document-model concerns. Inline comments are deliberately not recognized: a value retains everything after the
/// assignment to the end of the line, matching the safer <c>configparser</c> dialect and avoiding the classic
/// <c>value ; not-a-comment</c> footgun.
/// </remarks>
public readonly struct IniReaderOptions
{
    /// <summary>
    /// Gets the default reader options (both <c>;</c> and <c>#</c> begin comments, comments surfaced as tokens).
    /// </summary>
    /// <value>The default options.</value>
    public static IniReaderOptions Default => default;

    /// <summary>
    /// Gets a value indicating whether a leading <c>#</c> is rejected as a comment prefix, leaving <c>;</c> as the only
    /// comment character.
    /// </summary>
    /// <value><see langword="true" /> to disable <c>#</c> comments; otherwise <see langword="false" />.</value>
    /// <remarks>
    /// The property is inverted so the parameterless <see cref="Default" /> accepts both comment characters.
    /// </remarks>
    public bool DisallowHashComments { get; init; }

    /// <summary>
    /// Gets a value indicating whether comment lines are surfaced as <see cref="IniTokenType.Comment" /> tokens.
    /// </summary>
    /// <value><see langword="true" /> to skip comment tokens; otherwise <see langword="false" />.</value>
    /// <remarks>
    /// The property is inverted so the parameterless <see cref="Default" /> preserves comments.
    /// </remarks>
    public bool SkipComments { get; init; }

    /// <summary>
    /// Gets a value indicating whether <c>#</c> begins a comment.
    /// </summary>
    /// <value><see langword="true" /> when <c>#</c> comments are accepted.</value>
    internal bool AllowHashComments => !DisallowHashComments;

    /// <summary>
    /// Gets a value indicating whether comment tokens are surfaced.
    /// </summary>
    /// <value><see langword="true" /> when comments are preserved.</value>
    internal bool PreserveComments => !SkipComments;
}
