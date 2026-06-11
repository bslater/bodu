// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Serves as the base for the nodes of the intermediate value tree produced by <see cref="TomlDocumentBuilder" />.
/// </summary>
/// <remarks>
/// TOML cannot be tokenized in a single forward pass, because out-of-line <c>[table]</c> and <c>[[array-of-tables]]</c>
/// headers contribute to structure defined elsewhere in the document. The builder therefore materializes the whole
/// document into this tree, applying TOML's merge, redefinition, and ordering rules, and <see cref="Utf8TomlReader" />
/// walks the tree depth-first to emit a normalized token stream.
/// </remarks>
internal abstract class TomlReaderNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlReaderNode" /> class.
    /// </summary>
    /// <param name="offset">The zero-based source byte offset at which the node begins.</param>
    private protected TomlReaderNode(int offset)
    {
        Offset = offset;
    }

    /// <summary>
    /// Gets the kind of the node.
    /// </summary>
    /// <returns>The node kind.</returns>
    internal abstract TomlReaderNodeKind Kind { get; }

    /// <summary>
    /// Gets the zero-based source byte offset at which the node begins.
    /// </summary>
    /// <returns>The source offset.</returns>
    internal int Offset { get; }
}
