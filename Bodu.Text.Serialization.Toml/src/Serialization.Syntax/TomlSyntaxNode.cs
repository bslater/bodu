// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSyntaxNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Toml.Syntax;

/// <summary>
/// Serves as the base for every node in a TOML concrete syntax tree, adding a strongly-typed <see cref="Kind" /> over
/// the common <see cref="SyntaxNode.RawKind" /> "type code".
/// </summary>
/// <remarks>
/// The model is semantic: a <c>[table]</c> header, an inline <c>{ }</c> table, and dotted keys all materialize as
/// <see cref="TomlTableSyntax" /> nodes, so two documents that differ only in that syntactic choice parse to equal
/// trees. The exact source text is retained on <see cref="TomlDocumentSyntax" /> for lossless reproduction.
/// </remarks>
public abstract class TomlSyntaxNode
    : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSyntaxNode" /> class.
    /// </summary>
    /// <param name="kind">The node kind.</param>
    /// <param name="span">The span the node covers in the source.</param>
    private protected TomlSyntaxNode(TomlSyntaxKind kind, SourceSpan span)
        : base((int)kind, span)
    {
    }

    /// <summary>
    /// Gets the strongly-typed kind of the node.
    /// </summary>
    /// <returns>The node kind.</returns>
    public TomlSyntaxKind Kind => (TomlSyntaxKind)RawKind;

    /// <inheritdoc />
    public override bool IsToken => false;
}
