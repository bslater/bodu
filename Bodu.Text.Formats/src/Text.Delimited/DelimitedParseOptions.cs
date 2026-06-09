// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Controls dialect-specific parsing and formatting behaviour for <see cref="Delimited.Parse(ReadOnlySpan{char})" />
/// and related methods.
/// </summary>
/// <remarks>
/// <para>
/// The defaults produce a strict RFC 4180 CSV reader with header parsing enabled: comma delimiter, double-quote quote
/// character, first row treated as a header, no whitespace trimming. Use <see cref="Default" /> when none of those
/// choices need to change; otherwise construct a fresh value with the desired <c>init</c>-only properties overridden.
/// </para>
/// <para>
/// Most non-default settings exist to accept dialects that diverge from strict CSV — tab-delimited extracts emitted by
/// spreadsheet tools, vertical-bar files produced by legacy ETL pipelines, headerless data feeds, or sources that embed
/// a comment-line convention. Instances are <see langword="readonly" />; safe to share across threads.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Strict RFC 4180 CSV with a header row.
/// DelimitedDocument csv = Delimited.Parse(text);
///
/// // Tab-separated, no header, allow # comments.
/// var tsv = new DelimitedParseOptions
/// {
///     Delimiter         = '\t',
///     HasHeader         = false,
///     CommentCharacter  = '#',
/// };
/// DelimitedDocument data = Delimited.Parse(text, tsv);
///]]>
/// </example>
public readonly struct DelimitedParseOptions
{
    /// <summary>
    /// Gets a <see cref="DelimitedParseOptions" /> instance initialised with all default values — comma delimiter,
    /// double-quote character, header row present, no field trimming, no inline comments.
    /// </summary>
    /// <returns>A default <see cref="DelimitedParseOptions" /> value.</returns>
    public static readonly DelimitedParseOptions Default = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedParseOptions" /> struct with all properties set to their
    /// defaults.
    /// </summary>
    public DelimitedParseOptions()
    {
    }

    /// <summary>
    /// Gets the character used to separate fields within a record. The default is <c>','</c>.
    /// </summary>
    /// <returns>The field delimiter character.</returns>
    public char Delimiter { get; init; } = ',';

    /// <summary>
    /// Gets the character used to quote fields that contain the delimiter, the quote character, or line breaks. Within
    /// a quoted field, two consecutive quote characters represent a single literal quote. The default is <c>'"'</c>.
    /// </summary>
    /// <returns>The field-quoting character.</returns>
    public char Quote { get; init; } = '"';

    /// <summary>
    /// Gets a value indicating whether the first record of the source is treated as a header row containing column
    /// names. When <see langword="true" />, the header fields are exposed via <see cref="DelimitedDocument.Headers" />
    /// and fields in subsequent rows can be accessed by column name.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the first row is a header; otherwise, <see langword="false" />. The default is
    /// <see langword="true" />.
    /// </returns>
    public bool HasHeader { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether leading and trailing whitespace is trimmed from unquoted field values.
    /// Whitespace inside quoted fields is always preserved.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if unquoted fields are trimmed; otherwise, <see langword="false" />. The default is
    /// <see langword="false" />.
    /// </returns>
    public bool TrimFields { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether lines whose first non-whitespace character is <see cref="CommentChar" /> are
    /// treated as comments and ignored.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if comment lines are recognised; otherwise, <see langword="false" />. The default is
    /// <see langword="false" />.
    /// </returns>
    public bool AllowComments { get; init; } = false;

    /// <summary>
    /// Gets the character that marks the start of a comment line. Only relevant when <see cref="AllowComments" /> is
    /// <see langword="true" />. The default is <c>'#'</c>.
    /// </summary>
    /// <returns>The comment-line start character.</returns>
    public char CommentChar { get; init; } = '#';

    /// <summary>
    /// Gets the policy that controls whether data rows are required to contain exactly as many fields as the header
    /// row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default value is <see cref="DelimitedFieldCountBehavior.Strict" />, which raises
    /// <see cref="DelimitedFormatException" /> for any non-header row whose field count differs from the header. Use
    /// <see cref="DelimitedFieldCountBehavior.Ragged" /> to admit input that legitimately mixes row widths. The policy
    /// is not enforced when <see cref="HasHeader" /> is <see langword="false" /> because there is no reference row.
    /// </para>
    /// </remarks>
    /// <returns>The field-count enforcement policy.</returns>
    public DelimitedFieldCountBehavior FieldCountBehavior { get; init; } = DelimitedFieldCountBehavior.Strict;

    /// <summary>
    /// Gets the policy that controls how the parser reacts to a character that appears between a closing quote and the
    /// next field delimiter or line terminator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default value is <see cref="DelimitedMalformedRecordBehavior.Throw" />, which surfaces malformed records as
    /// <see cref="DelimitedFormatException" />. Use <see cref="DelimitedMalformedRecordBehavior.SkipRecord" /> to
    /// retain the historical lenient behaviour of discarding the remainder of the offending record.
    /// </para>
    /// </remarks>
    /// <returns>The malformed-record resolution policy.</returns>
    public DelimitedMalformedRecordBehavior MalformedRecordBehavior { get; init; } = DelimitedMalformedRecordBehavior.Throw;

    /// <summary>
    /// Gets the policy that controls how the parser resolves duplicate header names when constructing the column
    /// name-to-index map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default value is <see cref="DelimitedDuplicateHeaderBehavior.Throw" />, which treats duplicate header names
    /// as a structural error. Only relevant when <see cref="HasHeader" /> is <see langword="true" />.
    /// </para>
    /// </remarks>
    /// <returns>The duplicate-header resolution policy.</returns>
    public DelimitedDuplicateHeaderBehavior DuplicateHeaderBehavior { get; init; } = DelimitedDuplicateHeaderBehavior.Throw;
}
