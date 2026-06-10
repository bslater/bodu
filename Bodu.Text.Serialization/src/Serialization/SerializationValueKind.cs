// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationValueKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Identifies the format-neutral kind of value a serialization reader is positioned on, or that a serialization writer
/// is asked to emit. This is the common vocabulary every format maps its own value kinds onto, in the same role that
/// <see cref="System.Text.Json.JsonValueKind" /> plays for JSON.
/// </summary>
/// <remarks>
/// The enumeration is a superset across the supported formats. A format advertises only the kinds its grammar can
/// represent: a text format such as TOML surfaces the four date-time kinds, whereas a binary format such as Bencode
/// surfaces only <see cref="Int64" /> and <see cref="Bytes" /> for its scalars and rejects the rest unless a converter
/// supplies a representation.
/// </remarks>
public enum SerializationValueKind
{
    /// <summary>
    /// No value; the reader is positioned before the first token or after the last.
    /// </summary>
    None,

    /// <summary>
    /// The start of an object (a key/value structure such as a TOML table or a Bencode dictionary).
    /// </summary>
    StartObject,

    /// <summary>
    /// The end of an object.
    /// </summary>
    EndObject,

    /// <summary>
    /// The start of an array.
    /// </summary>
    StartArray,

    /// <summary>
    /// The end of an array.
    /// </summary>
    EndArray,

    /// <summary>
    /// The name of the property whose value follows.
    /// </summary>
    PropertyName,

    /// <summary>
    /// A Unicode string value.
    /// </summary>
    String,

    /// <summary>
    /// A binary value (a sequence of bytes).
    /// </summary>
    Bytes,

    /// <summary>
    /// A 64-bit signed integer value.
    /// </summary>
    Int64,

    /// <summary>
    /// A 64-bit unsigned integer value.
    /// </summary>
    UInt64,

    /// <summary>
    /// An IEEE 754 binary64 floating-point value.
    /// </summary>
    Double,

    /// <summary>
    /// A Boolean value.
    /// </summary>
    Boolean,

    /// <summary>
    /// A null value.
    /// </summary>
    Null,

    /// <summary>
    /// A date and time with a known offset from UTC.
    /// </summary>
    DateTimeOffset,

    /// <summary>
    /// A date and time with no offset or time-zone relation.
    /// </summary>
    DateTime,

    /// <summary>
    /// A calendar date with no offset or time-zone relation.
    /// </summary>
    DateOnly,

    /// <summary>
    /// A time of day with no offset or time-zone relation.
    /// </summary>
    TimeOnly,
}
