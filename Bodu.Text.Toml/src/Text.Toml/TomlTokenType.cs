// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTokenType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Identifies the kind of token the forward-only TOML reader is positioned on, mirroring the role of
/// <see cref="System.Text.Json.JsonTokenType" /> for TOML.
/// </summary>
/// <remarks>
/// <para>
/// The reader projects TOML's surface syntax onto a single, normalized, nested token stream. TOML expresses table
/// structure in several different ways — out-of-line <c>[table]</c> and <c>[[array-of-tables]]</c> headers, dotted
/// keys, and inline <c>{ … }</c> tables — yet all of them describe the same logical shape. The reader collapses these
/// forms so that a caller sees a uniform sequence of <see cref="StartTable" /> / <see cref="EndTable" /> and
/// <see cref="StartArray" /> / <see cref="EndArray" /> boundaries with intervening <see cref="PropertyName" /> and
/// scalar tokens, regardless of how the source spelled the structure.
/// </para>
/// <para>
/// An array-of-tables is surfaced as a <see cref="StartArray" /> whose elements are each a <see cref="StartTable" />,
/// so the consumer never has to special-case the <c>[[ … ]]</c> header form.
/// </para>
/// </remarks>
public enum TomlTokenType
{
    /// <summary>
    /// No token; the reader is positioned before the first token or after the last.
    /// </summary>
    None,

    /// <summary>
    /// The start of a table or inline table.
    /// </summary>
    StartTable,

    /// <summary>
    /// The end of a table or inline table.
    /// </summary>
    EndTable,

    /// <summary>
    /// The start of an array, or of an array-of-tables surfaced as an array whose elements are tables.
    /// </summary>
    StartArray,

    /// <summary>
    /// The end of an array, or of an array-of-tables surfaced as an array whose elements are tables.
    /// </summary>
    EndArray,

    /// <summary>
    /// A key naming the value that follows.
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
}
