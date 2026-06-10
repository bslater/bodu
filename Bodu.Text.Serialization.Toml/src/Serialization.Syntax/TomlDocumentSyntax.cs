// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentSyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Toml.Syntax;

/// <summary>
/// Represents the root of a parsed TOML document, wrapping the root table and retaining the exact source text so the
/// document can be written back without loss.
/// </summary>
/// <remarks>
/// Because TOML's surface syntax (table headers, dotted keys, inline tables, comments, and whitespace) cannot be
/// recovered from the semantic table model alone, the original source is retained and returned by
/// <see cref="ToFullString" />, giving an exact round trip for a parsed document.
/// </remarks>
public sealed class TomlDocumentSyntax
    : TomlSyntaxNode
{
    /// <summary>
    /// The exact source text of the document.
    /// </summary>
    private readonly string _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDocumentSyntax" /> class.
    /// </summary>
    /// <param name="root">The root table.</param>
    /// <param name="source">The exact source text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="root" /> or <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    public TomlDocumentSyntax(TomlTableSyntax root, string source)
        : base(TomlSyntaxKind.Document, new SourceSpan(0, source?.Length ?? 0))
    {
        ThrowHelper.ThrowIfNull(root);
        ThrowHelper.ThrowIfNull(source);

        Root = root;
        _source = source;
        AttachChildren(root);
    }

    /// <summary>
    /// Gets the root table of the document.
    /// </summary>
    /// <returns>The root table.</returns>
    public TomlTableSyntax Root { get; }

    /// <summary>
    /// Returns the exact source text of the document.
    /// </summary>
    /// <returns>The original TOML source.</returns>
    public string ToFullString() => _source;

    /// <inheritdoc />
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens()
    {
        yield return Root;
    }
}
