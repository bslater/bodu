// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedParseOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Controls dialect-specific parsing and formatting behaviour for <see cref="Delimited.Parse(ReadOnlySpan{char})" />
/// and related methods.
/// </summary>
public readonly struct DelimitedParseOptions
{
    /// <summary>
    /// Gets a <see cref="DelimitedParseOptions" /> instance initialised with all default values — comma delimiter,
    /// double-quote character, header row present, no field trimming, no inline comments.
    /// </summary>
    /// <returns>A default <see cref="DelimitedParseOptions" /> value.</returns>
    public static readonly DelimitedParseOptions Default = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedParseOptions" /> struct with all properties set to
    /// their defaults.
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
    /// Gets the character used to quote fields that contain the delimiter, the quote character, or line breaks.
    /// Within a quoted field, two consecutive quote characters represent a single literal quote. The default is
    /// <c>'"'</c>.
    /// </summary>
    /// <returns>The field-quoting character.</returns>
    public char Quote { get; init; } = '"';

    /// <summary>
    /// Gets a value indicating whether the first record of the source is treated as a header row containing column
    /// names. When <see langword="true" />, the header fields are exposed via <see cref="DelimitedDocument.Headers" />
    /// and fields in subsequent rows can be accessed by column name.
    /// </summary>
    /// <returns><see langword="true" /> when the first row is a header; otherwise, <see langword="false" />. The default is <see langword="true" />.</returns>
    public bool HasHeader { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether leading and trailing whitespace is trimmed from unquoted field values.
    /// Whitespace inside quoted fields is always preserved.
    /// </summary>
    /// <returns><see langword="true" /> if unquoted fields are trimmed; otherwise, <see langword="false" />. The default is <see langword="false" />.</returns>
    public bool TrimFields { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether lines whose first non-whitespace character is <see cref="CommentChar" />
    /// are treated as comments and ignored.
    /// </summary>
    /// <returns><see langword="true" /> if comment lines are recognised; otherwise, <see langword="false" />. The default is <see langword="false" />.</returns>
    public bool AllowComments { get; init; } = false;

    /// <summary>
    /// Gets the character that marks the start of a comment line. Only relevant when
    /// <see cref="AllowComments" /> is <see langword="true" />. The default is <c>'#'</c>.
    /// </summary>
    /// <returns>The comment-line start character.</returns>
    public char CommentChar { get; init; } = '#';
}
