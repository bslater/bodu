// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Controls dialect-specific parsing behaviour for <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> and related methods.
/// </summary>
/// <remarks>
/// <para>
/// The defaults accept the broad <c>.env</c> convention used by tools such as <c>dotenv</c>, Foreman / Docker Compose,
/// and the Twelve-Factor pattern: <c>LastWins</c> duplicate-key behaviour, support for the legacy <c>export </c>
/// prefix, and inline-comment recognition on unquoted values. Override the properties when consuming files produced by
/// a stricter dialect that should reject duplicates, omit shell-style prefixes, or treat <c>#</c> as literal data.
/// </para>
/// <para>
/// Instances are <see langword="readonly" />; safe to share across threads.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Default behaviour — wide compatibility with .env conventions.
/// DotEnvDocument doc = DotEnv.Parse(text);
///
/// Strict ingest: reject duplicate keys and disallow inline comments.
/// var options = new DotEnvParseOptions
/// {
///     DuplicateKeyBehavior = DuplicateKeyPolicy.Disallowed,
///     AllowExportPrefix    = false,
///     AllowInlineComments  = false,
/// };
/// DotEnvDocument strict = DotEnv.Parse(text, options);
///]]>
/// </example>
public readonly struct DotEnvParseOptions
{
    /// <summary>
    /// Gets a <see cref="DotEnvParseOptions" /> instance initialised with all default values.
    /// </summary>
    /// <returns>A default <see cref="DotEnvParseOptions" /> value.</returns>
    public static readonly DotEnvParseOptions Default = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvParseOptions" /> struct with all properties set to their
    /// defaults.
    /// </summary>
    public DotEnvParseOptions()
    {
    }

    /// <summary>
    /// Gets a value indicating how the parser behaves when the same key appears more than once in the source.
    /// </summary>
    /// <returns>
    /// A <see cref="DuplicateKeyPolicy" /> value. The default is
    /// <see cref="DuplicateKeyPolicy.LastWins" />.
    /// </returns>
    public DuplicateKeyPolicy DuplicateKeyBehavior { get; init; } = DuplicateKeyPolicy.LastWins;

    /// <summary>
    /// Gets a value indicating whether lines that begin with the <c>export </c> prefix are accepted. When
    /// <see langword="true" />, the literal word <c>export</c> followed by one or more spaces is stripped before the
    /// key is parsed. Only one level of <c>export</c> is consumed per line.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if <c>export </c> lines are accepted; otherwise, <see langword="false" />. The default
    /// is <see langword="true" />.
    /// </returns>
    public bool AllowExportPrefix { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether inline comments are recognised in unquoted values. When <see langword="true" />,
    /// a <c>#</c> character that is preceded by at least one whitespace character truncates an unquoted value at that
    /// point; trailing whitespace is then also trimmed.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if inline comments are stripped; otherwise, <see langword="false" />. The default is
    /// <see langword="true" />.
    /// </returns>
    public bool AllowInlineComments { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether full-line <c>#</c> comments are retained as trivia attached to the next entry
    /// through <see cref="DotEnvEntry.LeadingComments" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true" /> (the default), the parser accumulates pending comment lines and attaches them to
    /// the next <see cref="DotEnvEntry" /> in source order. <see cref="DotEnv.Format(DotEnvDocument)" /> then emits the
    /// leading comments before each entry, so simple comment-annotated <c>.env</c> files round-trip without losing
    /// their structure. Set to <see langword="false" /> to discard comments and treat the parsed document strictly as a
    /// data model.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see langword="true" /> if leading comments are preserved on entries; otherwise, <see langword="false" />. The
    /// default is <see langword="true" />.
    /// </returns>
    public bool PreserveComments { get; init; } = true;
}
