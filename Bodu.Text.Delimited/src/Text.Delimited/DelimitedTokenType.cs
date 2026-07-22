// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTokenType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Enumerates the token kinds a <see cref="Bodu.Text.Delimited.Reader.Utf8DelimitedReader" /> reports as it advances
/// through a delimited (CSV/TSV) document.
/// </summary>
/// <remarks>
/// A delimited document is an array of homogeneous records. With a header row each record is surfaced as an object
/// keyed by header name (<see cref="StartObject" /> … <see cref="EndObject" />); without a header each record is a
/// positional array of string fields (<see cref="StartArray" /> … <see cref="EndArray" />). Field values are always
/// <see cref="String" />.
/// </remarks>
public enum DelimitedTokenType
{
    /// <summary>
    /// No token has been read, or the reader has reached the end of the document.
    /// </summary>
    None = 0,

    /// <summary>
    /// The start of the document array, emitted before the first record.
    /// </summary>
    StartArray,

    /// <summary>
    /// The end of the document array, emitted after the last record.
    /// </summary>
    EndArray,

    /// <summary>
    /// The start of a record object (header mode).
    /// </summary>
    StartObject,

    /// <summary>
    /// The end of a record object (header mode).
    /// </summary>
    EndObject,

    /// <summary>
    /// A field name, emitted before its value (header mode).
    /// </summary>
    PropertyName,

    /// <summary>
    /// A field value.
    /// </summary>
    String,
}
