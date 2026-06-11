// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeValueKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Identifies the kind of value a Bencode element represents, mirroring the role of
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
    /// A dictionary, represented by a <see cref="Nodes.BencodeObject" /> in the mutable model or an object
    /// <see cref="BencodeElement" /> in the read-only model.
    /// </summary>
    Object,

    /// <summary>
    /// A list, represented by a <see cref="Nodes.BencodeArray" /> in the mutable model or an array
    /// <see cref="BencodeElement" /> in the read-only model.
    /// </summary>
    Array,

    /// <summary>
    /// A byte string, represented by a <see cref="Nodes.BencodeValue" /> in the mutable model or a byte-string
    /// <see cref="BencodeElement" /> in the read-only model.
    /// </summary>
    ByteString,

    /// <summary>
    /// An integer, represented by a <see cref="Nodes.BencodeValue" /> in the mutable model or an integer
    /// <see cref="BencodeElement" /> in the read-only model.
    /// </summary>
    Integer,
}
