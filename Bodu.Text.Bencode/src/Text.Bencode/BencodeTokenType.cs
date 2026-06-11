// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTokenType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Identifies the kind of token a <see cref="Utf8BencodeReader" /> is positioned on.
/// </summary>
public enum BencodeTokenType
{
    /// <summary>
    /// No token; the reader is positioned before the first token or after the last.
    /// </summary>
    None,

    /// <summary>
    /// The start of a list (<c>l</c>).
    /// </summary>
    StartList,

    /// <summary>
    /// The end of a list (<c>e</c>).
    /// </summary>
    EndList,

    /// <summary>
    /// The start of a dictionary (<c>d</c>).
    /// </summary>
    StartDictionary,

    /// <summary>
    /// The end of a dictionary (<c>e</c>).
    /// </summary>
    EndDictionary,

    /// <summary>
    /// A dictionary key (a byte string in key position).
    /// </summary>
    PropertyName,

    /// <summary>
    /// An integer value (<c>i…e</c>).
    /// </summary>
    Integer,

    /// <summary>
    /// A byte-string value (<c>&lt;length&gt;:&lt;bytes&gt;</c>).
    /// </summary>
    ByteString,
}
