// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderNodeKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Identifies the kind of a node in the intermediate value tree that <see cref="TomlDocumentBuilder" /> builds and
/// <see cref="Utf8TomlReader" /> flattens into its normalized token stream.
/// </summary>
internal enum TomlReaderNodeKind
{
    /// <summary>
    /// A table of key/value pairs (a header-defined table, an inline table, or an array-of-tables element).
    /// </summary>
    Table,

    /// <summary>
    /// An ordered array of values, including an array-of-tables.
    /// </summary>
    Array,

    /// <summary>
    /// A scalar value carrying a decoded CLR value.
    /// </summary>
    Scalar,
}
