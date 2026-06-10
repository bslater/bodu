// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntaxNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Serves as the abstract base for every node in a concrete syntax tree, regardless of the format that produced it. A
/// node is identified by a format-specific <see cref="RawKind" /> "type code", knows the <see cref="SourceSpan" /> it
/// covers, and links to its <see cref="Parent" />.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/serialization-syntax-model.svg" alt="The concrete syntax tree model. A SyntaxNode carries an integer RawKind that each format maps onto its own typed Kind enum (TomlSyntaxKind, BencodeSyntaxKind), a SourceSpan, and a parent link. Tokens are the leaves; text formats attach leading and trailing trivia (whitespace, comments, newlines) to each token while binary formats attach none, so walking the tokens reproduces the source exactly." />
/// </para>
/// <para>
/// This is the literal realization of a generic syntax object carrying a "type code": the common layer never names the
/// concrete kinds, it only stores the integer <see cref="RawKind" />. Each format exposes a strongly-typed <c>Kind</c>
/// property that casts the raw code onto its own enumeration, the same way <c>System.Text.Json</c> exposes a
/// <c>JsonValueKind</c>.
/// </para>
/// <para>
/// The tree is a single mutable structure (not a layered red/green tree). Reproduction back to text or bytes is a
/// format-specific concern provided by the per-format node base, because the common layer is unit-agnostic and makes no
/// assumption about whether the source is characters or bytes.
/// </para>
/// </remarks>
public abstract class SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxNode" /> class.
    /// </summary>
    /// <param name="rawKind">The format-specific kind code identifying the concrete node type.</param>
    /// <param name="span">The span the node covers in the source.</param>
    private protected SyntaxNode(int rawKind, SourceSpan span)
    {
        RawKind = rawKind;
        Span = span;
    }

    /// <summary>
    /// Gets the format-specific kind code identifying the concrete node type.
    /// </summary>
    /// <returns>The raw kind code, interpreted by each format as its own <c>Kind</c> enumeration value.</returns>
    public int RawKind { get; }

    /// <summary>
    /// Gets the span the node covers in the source.
    /// </summary>
    /// <returns>The source span.</returns>
    public SourceSpan Span { get; }

    /// <summary>
    /// Gets the parent node, or <see langword="null" /> when this node is the root of its tree.
    /// </summary>
    /// <returns>The parent node, or <see langword="null" /> for the root.</returns>
    public SyntaxNode? Parent { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this node is a terminal token (a leaf with no child nodes).
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the node is a <see cref="SyntaxToken" />; otherwise <see langword="false" />.
    /// </returns>
    public abstract bool IsToken { get; }

    /// <summary>
    /// Enumerates the direct child nodes and tokens of this node, in source order.
    /// </summary>
    /// <returns>The ordered child nodes and tokens; empty for a token.</returns>
    public abstract IEnumerable<SyntaxNode> ChildNodesAndTokens();

    /// <summary>
    /// Enumerates every terminal token at or beneath this node, in source order.
    /// </summary>
    /// <returns>The ordered descendant tokens.</returns>
    public IEnumerable<SyntaxToken> DescendantTokens()
    {
        if (this is SyntaxToken token)
        {
            yield return token;
            yield break;
        }

        foreach (SyntaxNode child in ChildNodesAndTokens())
        {
            foreach (SyntaxToken descendant in child.DescendantTokens())
                yield return descendant;
        }
    }

    /// <summary>
    /// Attaches the supplied nodes to this node as their parent, ignoring any that are <see langword="null" />.
    /// </summary>
    /// <param name="children">The child nodes to attach.</param>
    /// <remarks>
    /// Called from a derived constructor to wire up the parent links of fixed children. Children supplied through a
    /// <see cref="SyntaxList{TNode}" /> are parented by the list itself.
    /// </remarks>
    protected void AttachChildren(params SyntaxNode?[] children)
    {
        ThrowHelper.ThrowIfNull(children);

        foreach (SyntaxNode? child in children)
        {
            if (child is not null)
                child.Parent = this;
        }
    }
}
