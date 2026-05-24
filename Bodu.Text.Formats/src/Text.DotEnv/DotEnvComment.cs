// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvComment.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Represents a single full-line comment captured by the DotEnv parser as trivia attached to the next entry.
/// </summary>
/// <remarks>
/// <para>
/// Comments are retained only when <see cref="DotEnvParseOptions.PreserveComments" /> is <see langword="true" />
/// (the default). Each instance records the prefix character that introduced the line (currently always
/// <c>'#'</c> — the de facto DotEnv comment marker), the text after the prefix, and the 1-based source line.
/// </para>
/// <para>
/// Pending comments are attached to the next entry in source order via
/// <see cref="DotEnvEntry.LeadingComments" />.
/// </para>
/// </remarks>
[DebuggerDisplay("{Prefix}{Text,nq}")]
public readonly struct DotEnvComment : IEquatable<DotEnvComment>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvComment" /> struct.
    /// </summary>
    /// <param name="prefix">The introducing character; must be <c>'#'</c>.</param>
    /// <param name="text">The text of the comment, excluding the prefix character.</param>
    /// <param name="lineNumber">
    /// The 1-based source line at which the comment appeared, or <c>0</c> when the comment was constructed
    /// programmatically.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="prefix" /> is not <c>'#'</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    public DotEnvComment(char prefix, string text, int lineNumber = 0)
    {
        ThrowHelper.ThrowIfNull(text);
        if (prefix != '#')
            throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_DotEnvCommentPrefix, nameof(prefix));

        Prefix = prefix;
        Text = text;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the prefix character that introduced this comment.
    /// </summary>
    /// <returns>The character <c>'#'</c>.</returns>
    public char Prefix { get; }

    /// <summary>
    /// Gets the comment text, excluding the prefix character.
    /// </summary>
    /// <returns>A non-null string.</returns>
    public string Text { get; }

    /// <summary>
    /// Gets the 1-based source line at which the comment appeared, or <c>0</c> when programmatically constructed.
    /// </summary>
    /// <returns>A non-negative line number.</returns>
    public int LineNumber { get; }

    /// <inheritdoc />
    public bool Equals(DotEnvComment other) =>
        this.Prefix == other.Prefix
        && string.Equals(this.Text, other.Text, StringComparison.Ordinal)
        && this.LineNumber == other.LineNumber;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is DotEnvComment other && this.Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(this.Prefix, this.Text, this.LineNumber);

    /// <inheritdoc />
    public override string ToString() =>
        this.Text is null ? string.Empty : string.Concat(this.Prefix.ToString(), this.Text);

    /// <summary>
    /// Determines whether two comments are equal.
    /// </summary>
    /// <param name="left">The first comment.</param>
    /// <param name="right">The second comment.</param>
    /// <returns><see langword="true" /> when the comments are equal.</returns>
    public static bool operator ==(DotEnvComment left, DotEnvComment right) => left.Equals(right);

    /// <summary>
    /// Determines whether two comments are not equal.
    /// </summary>
    /// <param name="left">The first comment.</param>
    /// <param name="right">The second comment.</param>
    /// <returns><see langword="true" /> when the comments differ.</returns>
    public static bool operator !=(DotEnvComment left, DotEnvComment right) => !left.Equals(right);
}
