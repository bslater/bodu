// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeValueKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Identifies the kind of value a <see cref="BencodeNode" /> represents, mirroring the role of
/// <see cref="System.Text.Json.JsonValueKind" /> for Bencode.
/// </summary>
/// <remarks>
/// Bencode (BEP 3) defines only four value kinds — dictionaries, lists, byte strings, and integers — so this
/// enumeration omits the Boolean, null, and floating-point members that <see cref="System.Text.Json.JsonValueKind" />
/// carries for JSON.
/// </remarks>
public enum BencodeValueKind
{
    /// <summary>
    /// A dictionary, represented by a <see cref="BencodeObject" />.
    /// </summary>
    Object,

    /// <summary>
    /// A list, represented by a <see cref="BencodeArray" />.
    /// </summary>
    Array,

    /// <summary>
    /// A byte string, represented by a <see cref="BencodeValue" />.
    /// </summary>
    ByteString,

    /// <summary>
    /// An integer, represented by a <see cref="BencodeValue" />.
    /// </summary>
    Integer,
}
