// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlLexTokenType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Identifies the kind of source-order token a <see cref="TomlLexer" /> is positioned on.
/// </summary>
/// <remarks>
/// Unlike <see cref="TomlTokenType" />, which describes the normalized tree-order stream, this vocabulary follows the
/// document's surface syntax: table headers appear as their own tokens followed by one <see cref="Key" /> per dotted
/// segment, key/value pairs appear as <see cref="Key" /> segments followed by the value's tokens, and inline tables are
/// distinguished from header-defined tables.
/// </remarks>
internal enum TomlLexTokenType
{
    /// <summary>
    /// No token; the lexer is positioned before the first token or after the last.
    /// </summary>
    None,

    /// <summary>
    /// A <c>[table]</c> header, emitted before the header's <see cref="Key" /> segments.
    /// </summary>
    TableHeader,

    /// <summary>
    /// An <c>[[array-of-tables]]</c> header, emitted before the header's <see cref="Key" /> segments.
    /// </summary>
    ArrayTableHeader,

    /// <summary>
    /// One segment of a dotted key, in a header or in a key/value pair.
    /// </summary>
    Key,

    /// <summary>
    /// A comment, from after the <c>#</c> to the end of the line.
    /// </summary>
    Comment,

    /// <summary>
    /// The start of an array value (<c>[</c>).
    /// </summary>
    StartArray,

    /// <summary>
    /// The end of an array value (<c>]</c>).
    /// </summary>
    EndArray,

    /// <summary>
    /// The start of an inline table value (<c>{</c>).
    /// </summary>
    StartInlineTable,

    /// <summary>
    /// The end of an inline table value (<c>}</c>).
    /// </summary>
    EndInlineTable,

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
}
