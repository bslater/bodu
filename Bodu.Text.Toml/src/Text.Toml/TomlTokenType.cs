// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTokenType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Identifies the kind of token a forward-only TOML reader is positioned on.
/// </summary>
/// <remarks>
/// <para>
/// The vocabulary serves the library's two readers. <see cref="Bodu.Text.Toml.Reader.Utf8TomlReader" /> follows the
/// document's surface syntax in source order: a header surfaces as <see cref="TableHeader" /> or
/// <see cref="ArrayTableHeader" /> followed by one <see cref="Key" /> per dotted segment, a key/value pair as its
/// <see cref="Key" /> segments followed by the value's tokens, an inline table as <see cref="StartInlineTable" /> /
/// <see cref="EndInlineTable" />, and comments as <see cref="Comment" /> tokens.
/// </para>
/// <para>
/// <see cref="Bodu.Text.Toml.Reader.TomlDocumentReader" /> projects the parsed document onto a single, normalized,
/// nested token stream. TOML expresses table structure in several different ways — out-of-line <c>[table]</c> and
/// <c>[[array-of-tables]]</c> headers, dotted keys, and inline <c>{ … }</c> tables — yet all of them describe the same
/// logical shape. The normalized stream collapses these forms so that a caller sees a uniform sequence of
/// <see cref="StartTable" /> / <see cref="EndTable" /> and <see cref="StartArray" /> / <see cref="EndArray" />
/// boundaries with intervening <see cref="PropertyName" /> and scalar tokens, regardless of how the source spelled the
/// structure; an array-of-tables is surfaced as a <see cref="StartArray" /> whose elements are each a
/// <see cref="StartTable" />.
/// </para>
/// <para>
/// The scalar members are shared by both readers; the header, key, comment, and inline-table members appear only in
/// the source-order stream, and the table and property-name members only in the normalized stream.
/// </para>
/// </remarks>
public enum TomlTokenType
{
    /// <summary>
    /// No token; the reader is positioned before the first token or after the last.
    /// </summary>
    None,

    /// <summary>
    /// The start of a table in the normalized stream (a header-defined table, a dotted-key table, or an inline
    /// table).
    /// </summary>
    StartTable,

    /// <summary>
    /// The end of a table in the normalized stream.
    /// </summary>
    EndTable,

    /// <summary>
    /// The start of an array, or — in the normalized stream — of an array-of-tables surfaced as an array whose
    /// elements are tables.
    /// </summary>
    StartArray,

    /// <summary>
    /// The end of an array, or — in the normalized stream — of an array-of-tables surfaced as an array whose elements
    /// are tables.
    /// </summary>
    EndArray,

    /// <summary>
    /// A key naming the value that follows, in the normalized stream.
    /// </summary>
    PropertyName,

    /// <summary>
    /// A string value.
    /// </summary>
    String,

    /// <summary>
    /// A 64-bit signed integer value.
    /// </summary>
    Integer,

    /// <summary>
    /// An IEEE 754 binary64 floating-point value.
    /// </summary>
    Float,

    /// <summary>
    /// A Boolean value.
    /// </summary>
    Boolean,

    /// <summary>
    /// An RFC 3339 date-time that carries an explicit time offset from UTC.
    /// </summary>
    OffsetDateTime,

    /// <summary>
    /// A date-time without any offset or time-zone relation.
    /// </summary>
    LocalDateTime,

    /// <summary>
    /// A calendar date without any time-of-day, offset, or time-zone relation.
    /// </summary>
    LocalDate,

    /// <summary>
    /// A time of day without any date, offset, or time-zone relation.
    /// </summary>
    LocalTime,

    /// <summary>
    /// A <c>[table]</c> header in the source-order stream, emitted before the header's <see cref="Key" /> segments.
    /// </summary>
    TableHeader,

    /// <summary>
    /// An <c>[[array-of-tables]]</c> header in the source-order stream, emitted before the header's
    /// <see cref="Key" /> segments.
    /// </summary>
    ArrayTableHeader,

    /// <summary>
    /// One segment of a dotted key in the source-order stream, in a header or in a key/value pair.
    /// </summary>
    Key,

    /// <summary>
    /// A comment in the source-order stream, from after the <c>#</c> to the end of the line.
    /// </summary>
    Comment,

    /// <summary>
    /// The start of an inline table value (<c>{</c>) in the source-order stream.
    /// </summary>
    StartInlineTable,

    /// <summary>
    /// The end of an inline table value (<c>}</c>) in the source-order stream.
    /// </summary>
    EndInlineTable,
}
