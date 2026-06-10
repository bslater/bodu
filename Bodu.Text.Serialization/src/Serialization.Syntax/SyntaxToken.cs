// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntaxToken.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Serves as the abstract base for the terminal tokens of a concrete syntax tree — the leaves produced by a lexer. A
/// token carries optional leading and trailing <see cref="SyntaxTrivia" /> so a text format can reproduce the exact
/// source surrounding it.
/// </summary>
/// <remarks>
/// The token base is unit-agnostic: the actual lexeme (a character string for a text format, a byte segment for a
/// binary format) is stored by the concrete per-format token type in its own representation. Binary tokens leave the
/// trivia lists empty, since a binary grammar has no insignificant text.
/// </remarks>
public abstract class SyntaxToken
    : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxToken" /> class with no trivia.
    /// </summary>
    /// <param name="rawKind">The format-specific token kind code.</param>
    /// <param name="span">The span the token covers in the source.</param>
    private protected SyntaxToken(int rawKind, SourceSpan span)
        : this(rawKind, span, SyntaxTriviaList.Empty, SyntaxTriviaList.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxToken" /> class with the supplied trivia.
    /// </summary>
    /// <param name="rawKind">The format-specific token kind code.</param>
    /// <param name="span">The span the token covers in the source.</param>
    /// <param name="leadingTrivia">The trivia immediately preceding the token.</param>
    /// <param name="trailingTrivia">The trivia immediately following the token.</param>
    private protected SyntaxToken(int rawKind, SourceSpan span, SyntaxTriviaList leadingTrivia, SyntaxTriviaList trailingTrivia)
        : base(rawKind, span)
    {
        LeadingTrivia = leadingTrivia;
        TrailingTrivia = trailingTrivia;
    }

    /// <inheritdoc />
    public sealed override bool IsToken => true;

    /// <summary>
    /// Gets the trivia immediately preceding the token.
    /// </summary>
    /// <returns>The leading trivia; empty when the token has none.</returns>
    public SyntaxTriviaList LeadingTrivia { get; }

    /// <summary>
    /// Gets the trivia immediately following the token.
    /// </summary>
    /// <returns>The trailing trivia; empty when the token has none.</returns>
    public SyntaxTriviaList TrailingTrivia { get; }

    /// <inheritdoc />
    /// <returns>An empty sequence, because a token has no child nodes.</returns>
    public sealed override IEnumerable<SyntaxNode> ChildNodesAndTokens() =>
        [];
}
