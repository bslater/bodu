// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSyntaxKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Identifies the kind of a node in a Bencode concrete syntax tree. The value is the
/// <see cref="Bodu.Text.Serialization.Syntax.SyntaxNode.RawKind" /> "type code" interpreted in the Bencode grammar.
/// </summary>
public enum BencodeSyntaxKind
{
    /// <summary>
    /// The document root, wrapping the single top-level value.
    /// </summary>
    Document,

    /// <summary>
    /// An integer value (<c>i…e</c>).
    /// </summary>
    Integer,

    /// <summary>
    /// A byte-string value (<c>&lt;length&gt;:&lt;bytes&gt;</c>).
    /// </summary>
    ByteString,

    /// <summary>
    /// A list value (<c>l…e</c>).
    /// </summary>
    List,

    /// <summary>
    /// A dictionary value (<c>d…e</c>).
    /// </summary>
    Dictionary,

    /// <summary>
    /// A single key/value pair within a dictionary.
    /// </summary>
    KeyValuePair,
}
