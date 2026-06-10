// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlValueKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Identifies the concrete type of a TOML value, mirroring the role of <see cref="System.Text.Json.JsonValueKind" />
/// for TOML. The members correspond to the value types defined by the TOML specification.
/// </summary>
public enum TomlValueKind
{
    /// <summary>
    /// A Unicode string value, expressed in TOML as a basic, multi-line basic, literal, or multi-line literal string.
    /// </summary>
    String,

    /// <summary>
    /// A 64-bit signed integer value.
    /// </summary>
    Integer,

    /// <summary>
    /// An IEEE 754 binary64 floating-point value, including the special values <c>inf</c> and <c>nan</c>.
    /// </summary>
    Float,

    /// <summary>
    /// A Boolean value, expressed in TOML as the lowercase literal <c>true</c> or <c>false</c>.
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
    /// An ordered, heterogeneous array of TOML values.
    /// </summary>
    Array,

    /// <summary>
    /// A table of key/value pairs, expressed in TOML as a header-defined table, an inline table, or an array-of-tables
    /// element.
    /// </summary>
    Table,
}
