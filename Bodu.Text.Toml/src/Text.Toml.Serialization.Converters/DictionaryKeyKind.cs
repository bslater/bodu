// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryKeyKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Identifies how a <see cref="DictionaryConverter{TDictionary, TKey, TValue}" /> converts a dictionary key to and from
/// the key of a TOML table.
/// </summary>
internal enum DictionaryKeyKind
{
    /// <summary>
    /// The key is a <see cref="string" /> and is used verbatim.
    /// </summary>
    String,

    /// <summary>
    /// The key is one of the fixed-width integer types; it is written as invariant-culture decimal text and parsed back
    /// with a range check.
    /// </summary>
    Integer,

    /// <summary>
    /// The key is an enumeration; it is written as its member name (or decimal fallback for undefined values) and
    /// parsed back case-insensitively.
    /// </summary>
    Enum,

    /// <summary>
    /// The key is a <see cref="Guid" />; it is written in the 32-digit hyphenated ("D") format and parsed back exactly.
    /// </summary>
    Guid,

    /// <summary>
    /// The key is a <see cref="bool" />; it is written as <c>True</c> or <c>False</c> and parsed back
    /// case-insensitively.
    /// </summary>
    Boolean,

    /// <summary>
    /// The key is a <see cref="char" />; it is written as a single-character string and parsed back from exactly one
    /// character.
    /// </summary>
    Char,
}
