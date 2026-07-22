// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Provides configuration for a <see cref="Utf8DelimitedReader" />, controlling the delimiter, quoting, header
/// handling, and the field-count and duplicate-header policies.
/// </summary>
/// <remarks>
/// The options are an immutable value type. Because the default value of a <see cref="char" /> is <c>'\0'</c>, the
/// delimiter, quote, and comment characters fall back to their RFC 4180 defaults (<c>','</c>, <c>'"'</c>, <c>'#'</c>)
/// when left unset, and headers are read by default (set <see cref="NoHeader" /> to disable).
/// </remarks>
public readonly struct DelimitedReaderOptions
{
    /// <summary>
    /// Gets the default reader options (comma delimiter, double-quote quoting, header row present).
    /// </summary>
    /// <value>The default options.</value>
    public static DelimitedReaderOptions Default => default;

    /// <summary>
    /// Gets the field delimiter, or <c>'\0'</c> to use the default comma.
    /// </summary>
    /// <value>The delimiter character.</value>
    public char Delimiter { get; init; }

    /// <summary>
    /// Gets the quote character, or <c>'\0'</c> to use the default double quote.
    /// </summary>
    /// <value>The quote character.</value>
    public char Quote { get; init; }

    /// <summary>
    /// Gets a value indicating whether the first record is treated as data rather than a header row.
    /// </summary>
    /// <value><see langword="true" /> when there is no header; otherwise <see langword="false" />.</value>
    public bool NoHeader { get; init; }

    /// <summary>
    /// Gets a value indicating whether surrounding whitespace is trimmed from unquoted fields.
    /// </summary>
    /// <value><see langword="true" /> to trim fields; otherwise <see langword="false" />.</value>
    public bool TrimFields { get; init; }

    /// <summary>
    /// Gets a value indicating whether lines beginning with the comment character are skipped.
    /// </summary>
    /// <value><see langword="true" /> to allow comment lines; otherwise <see langword="false" />.</value>
    public bool AllowComments { get; init; }

    /// <summary>
    /// Gets the comment character, or <c>'\0'</c> to use the default <c>'#'</c>.
    /// </summary>
    /// <value>The comment character.</value>
    public char CommentChar { get; init; }

    /// <summary>
    /// Gets the policy applied to a record whose field count differs from the header's.
    /// </summary>
    /// <value>The field-count behavior.</value>
    public DelimitedFieldCountBehavior FieldCountBehavior { get; init; }

    /// <summary>
    /// Gets the policy applied to a record that violates the field-count policy.
    /// </summary>
    /// <value>The malformed-record behavior.</value>
    public DelimitedMalformedRecordBehavior MalformedRecordBehavior { get; init; }

    /// <summary>
    /// Gets the policy applied to a header row that repeats a column name.
    /// </summary>
    /// <value>The duplicate-header behavior.</value>
    public DelimitedDuplicateHeaderBehavior DuplicateHeaderBehavior { get; init; }

    /// <summary>
    /// Gets the effective delimiter, resolving the unset default to a comma.
    /// </summary>
    /// <value>The delimiter character.</value>
    internal char EffectiveDelimiter => Delimiter == '\0' ? ',' : Delimiter;

    /// <summary>
    /// Gets the effective quote, resolving the unset default to a double quote.
    /// </summary>
    /// <value>The quote character.</value>
    internal char EffectiveQuote => Quote == '\0' ? '"' : Quote;

    /// <summary>
    /// Gets the effective comment character, resolving the unset default to a hash.
    /// </summary>
    /// <value>The comment character.</value>
    internal char EffectiveCommentChar => CommentChar == '\0' ? '#' : CommentChar;

    /// <summary>
    /// Gets a value indicating whether a header row is present.
    /// </summary>
    /// <value><see langword="true" /> when a header is present.</value>
    internal bool HasHeader => !NoHeader;
}
