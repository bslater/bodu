// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedValueKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Enumerates the value kinds a delimited node or element can represent.
/// </summary>
public enum DelimitedValueKind
{
    /// <summary>
    /// An array — the document of records, or a positional (headerless) record.
    /// </summary>
    Array = 0,

    /// <summary>
    /// An object — a record keyed by header name.
    /// </summary>
    Object,

    /// <summary>
    /// A string field value.
    /// </summary>
    String,
}
