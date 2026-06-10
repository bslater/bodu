// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntaxTrivia.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Represents a span of insignificant source text — whitespace, a line break, or a comment — attached to a
/// <see cref="SyntaxToken" /> as leading or trailing trivia. Trivia carries no semantic meaning but is preserved so a
/// parsed tree can be written back to text without loss.
/// </summary>
/// <remarks>
/// Trivia is a text-format concept: a character-oriented format such as TOML attaches the whitespace and comments
/// surrounding each token, whereas a binary format such as Bencode produces tokens with no trivia at all.
/// </remarks>
public readonly struct SyntaxTrivia
    : IEquatable<SyntaxTrivia>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxTrivia" /> struct.
    /// </summary>
    /// <param name="rawKind">The format-specific kind code identifying the trivia category.</param>
    /// <param name="span">The span the trivia covers in the source.</param>
    /// <param name="text">The exact source text of the trivia.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    public SyntaxTrivia(int rawKind, SourceSpan span, string text)
    {
        ThrowHelper.ThrowIfNull(text);

        RawKind = rawKind;
        Span = span;
        Text = text;
    }

    /// <summary>
    /// Gets the format-specific kind code identifying the trivia category.
    /// </summary>
    /// <returns>The raw kind code.</returns>
    public int RawKind { get; }

    /// <summary>
    /// Gets the span the trivia covers in the source.
    /// </summary>
    /// <returns>The source span.</returns>
    public SourceSpan Span { get; }

    /// <summary>
    /// Gets the exact source text of the trivia.
    /// </summary>
    /// <returns>The verbatim trivia text.</returns>
    public string Text { get; }

    /// <summary>
    /// Indicates whether two trivia values are equal.
    /// </summary>
    /// <param name="left">The first trivia.</param>
    /// <param name="right">The second trivia.</param>
    /// <returns><see langword="true" /> when the trivia values match; otherwise <see langword="false" />.</returns>
    public static bool operator ==(SyntaxTrivia left, SyntaxTrivia right) =>
        left.Equals(right);

    /// <summary>
    /// Indicates whether two trivia values are not equal.
    /// </summary>
    /// <param name="left">The first trivia.</param>
    /// <param name="right">The second trivia.</param>
    /// <returns><see langword="true" /> when the trivia values differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(SyntaxTrivia left, SyntaxTrivia right) =>
        !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(SyntaxTrivia other) =>
        RawKind == other.RawKind && Span == other.Span && string.Equals(Text, other.Text, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is SyntaxTrivia other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(RawKind, Span, Text);

    /// <summary>
    /// Returns the trivia text.
    /// </summary>
    /// <returns>The verbatim trivia text.</returns>
    public override string ToString() =>
        Text;
}
