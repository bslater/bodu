// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// A single node in the flat row store produced by <see cref="TomlDocumentBuilder" />: a table, an array, or a decoded
/// scalar. Children are held as an index-linked list (<see cref="FirstChild" /> … <see cref="NextSibling" />) rather
/// than a contiguous span, so the builder can append out-of-line <c>[table]</c> content to an already-recorded table
/// without shifting rows. The same store backs random-access navigation in <c>TomlDocument</c> and the depth-first
/// cursor in <see cref="TomlDocumentReader" />, so no separate tree or token list is materialized.
/// </summary>
internal struct TomlReaderRow
{
    /// <summary>
    /// The kind of node: <see cref="TomlReaderNodeKind.Table" />, <see cref="TomlReaderNodeKind.Array" />, or
    /// <see cref="TomlReaderNodeKind.Scalar" />.
    /// </summary>
    public TomlReaderNodeKind Kind;

    /// <summary>
    /// For a scalar, the token type that classifies the value; unused for a table or array.
    /// </summary>
    public TomlTokenType TokenType;

    /// <summary>
    /// For a scalar, the decoded CLR value; <see langword="null" /> for a table or array.
    /// </summary>
    public object? Value;

    /// <summary>
    /// The key under which this row sits in its parent table, or <see langword="null" /> for an array element or the
    /// document root.
    /// </summary>
    public string? Key;

    /// <summary>
    /// The zero-based source byte offset at which the node begins.
    /// </summary>
    public int Offset;

    /// <summary>
    /// The row index of this container's first child, or <c>-1</c> when it has none.
    /// </summary>
    public int FirstChild;

    /// <summary>
    /// The row index of this container's last child, used to append in constant time during the build, or <c>-1</c>.
    /// </summary>
    public int LastChild;

    /// <summary>
    /// The row index of the sibling that follows this row within its parent, or <c>-1</c> when it is the last child.
    /// </summary>
    public int NextSibling;

    /// <summary>
    /// The number of children: key/value pairs for a table, elements for an array.
    /// </summary>
    public int ChildCount;

    /// <summary>
    /// The nesting depth of a table within its tree, where the document root is zero, used to bound nesting created by
    /// dotted keys and header paths.
    /// </summary>
    public int Depth;

    /// <summary>
    /// The structural classifications recorded while building, used to enforce TOML's table rules.
    /// </summary>
    public TomlReaderRowFlags Flags;
}
