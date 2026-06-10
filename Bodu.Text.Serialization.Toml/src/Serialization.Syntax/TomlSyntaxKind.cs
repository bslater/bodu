// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSyntaxKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Toml.Syntax;

/// <summary>
/// Identifies the kind of a node in a TOML concrete syntax tree. The value is the
/// <see cref="Bodu.Text.Serialization.Syntax.SyntaxNode.RawKind" /> "type code" interpreted in the TOML grammar.
/// </summary>
public enum TomlSyntaxKind
{
    /// <summary>
    /// The document root.
    /// </summary>
    Document,

    /// <summary>
    /// A table (a key/value structure).
    /// </summary>
    Table,

    /// <summary>
    /// An array.
    /// </summary>
    Array,

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
    /// An RFC 3339 date-time with a time offset.
    /// </summary>
    OffsetDateTime,

    /// <summary>
    /// A date-time without an offset.
    /// </summary>
    LocalDateTime,

    /// <summary>
    /// A calendar date without an offset.
    /// </summary>
    LocalDate,

    /// <summary>
    /// A time of day without an offset.
    /// </summary>
    LocalTime,
}
