// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlValueKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Identifies the concrete type of a TOML value, corresponding to the value types defined by the TOML v1.1.0
/// specification.
/// </summary>
public enum TomlValueKind
{
    /// <summary>
    /// A Unicode string value.
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
    /// A boolean value.
    /// </summary>
    Boolean,

    /// <summary>
    /// An RFC 3339 date-time with a time offset.
    /// </summary>
    OffsetDateTime,

    /// <summary>
    /// A date-time without any offset or time-zone relation.
    /// </summary>
    LocalDateTime,

    /// <summary>
    /// A calendar date without any offset or time-zone relation.
    /// </summary>
    LocalDate,

    /// <summary>
    /// A time of day without any offset or time-zone relation.
    /// </summary>
    LocalTime,

    /// <summary>
    /// An ordered array of TOML values.
    /// </summary>
    Array,

    /// <summary>
    /// A table of key/value pairs.
    /// </summary>
    Table,
}
