// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedValueKind.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Identifies the concrete type of a bencoded value.
/// </summary>
public enum BencodedValueKind
{
    /// <summary>
    /// A signed integer value.
    /// </summary>
    Integer,

    /// <summary>
    /// A length-prefixed raw byte string.
    /// </summary>
    String,

    /// <summary>
    /// An ordered list of bencoded values.
    /// </summary>
    List,

    /// <summary>
    /// A dictionary with raw byte-string keys and bencoded values.
    /// </summary>
    Dictionary,
}
