// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationComment.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Text.Configuration;

/// <summary>
/// Captures a single comment as preserved trivia within a configuration document. Stores the prefix
/// character (<c>#</c> or <c>;</c>), the comment text, and the source location.
/// </summary>
/// <remarks>
/// Comments are immutable. They participate in round-tripping so the writer can emit the document in a form
/// indistinguishable from the input.
/// </remarks>
[DebuggerDisplay("{Prefix}{Text,nq}")]
public readonly struct BoduConfigurationComment : IEquatable<BoduConfigurationComment>
{
    /// <summary>
    /// Initializes a new <see cref="BoduConfigurationComment" /> with the specified prefix, text, and location.
    /// </summary>
    /// <param name="prefix">The comment prefix character. Must be <c>#</c> or <c>;</c>.</param>
    /// <param name="text">The comment body excluding the prefix.</param>
    /// <param name="location">The location at which the comment appeared in the source.</param>
    /// <exception cref="ArgumentException"><paramref name="prefix" /> is neither <c>#</c> nor <c>;</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    public BoduConfigurationComment(char prefix, string text, BoduConfigurationSourceLocation location)
    {
        ThrowHelper.ThrowIfNull(text);
        if (prefix != '#' && prefix != ';')
            throw new ArgumentException("Comment prefix must be '#' or ';'.", nameof(prefix));

        this.Prefix = prefix;
        this.Text = text;
        this.Location = location;
    }

    /// <summary>
    /// Gets the prefix character that introduced the comment (<c>#</c> or <c>;</c>).
    /// </summary>
    /// <returns>The prefix supplied at construction.</returns>
    public char Prefix { get; }

    /// <summary>
    /// Gets the comment text, excluding the prefix character.
    /// </summary>
    /// <returns>The comment body.</returns>
    public string Text { get; }

    /// <summary>
    /// Gets the location at which the comment appeared in the source document.
    /// </summary>
    /// <returns>The associated source location.</returns>
    public BoduConfigurationSourceLocation Location { get; }

    /// <inheritdoc />
    public bool Equals(BoduConfigurationComment other) =>
        this.Prefix == other.Prefix
        && string.Equals(this.Text, other.Text, StringComparison.Ordinal)
        && this.Location.Equals(other.Location);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is BoduConfigurationComment other && this.Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(this.Prefix, this.Text, this.Location);

    /// <inheritdoc />
    public override string ToString() =>
        this.Text is null ? string.Empty : string.Concat(this.Prefix.ToString(), this.Text);

    /// <summary>
    /// Determines whether two comments are equal.
    /// </summary>
    /// <param name="left">The first comment to compare.</param>
    /// <param name="right">The second comment to compare.</param>
    /// <returns><see langword="true" /> when the comments are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(BoduConfigurationComment left, BoduConfigurationComment right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two comments are not equal.
    /// </summary>
    /// <param name="left">The first comment to compare.</param>
    /// <param name="right">The second comment to compare.</param>
    /// <returns><see langword="true" /> when the comments differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(BoduConfigurationComment left, BoduConfigurationComment right) =>
        !left.Equals(right);
}
