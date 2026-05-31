// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniComment.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Text.Ini;

/// <summary>
/// Represents a single comment line preserved as trivia within an <see cref="IniDocument" />.
/// </summary>
/// <remarks>
/// <para>
/// Comments are captured by the parser only when <see cref="IniParseOptions.PreserveComments" /> is
/// <see langword="true" /> (the default). Each <see cref="IniComment" /> records the prefix character that introduced
/// the line (either <c>'#'</c> or <c>';'</c>), the trimmed text, and the 1-based source line at which it appeared.
/// </para>
/// <para>
/// Pending comments are attached to the next entry or section in source order: comments authored before a property line
/// appear in <see cref="IniEntry.LeadingComments" />; comments authored before a section header appear in
/// <see cref="IniSection.LeadingComments" />.
/// </para>
/// </remarks>
[DebuggerDisplay("{Prefix}{Text,nq}")]
public readonly struct IniComment : IEquatable<IniComment>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IniComment" /> struct.
    /// </summary>
    /// <param name="prefix">The introducing character; must be <c>'#'</c> or <c>';'</c>.</param>
    /// <param name="text">The text of the comment, excluding the prefix character.</param>
    /// <param name="lineNumber">
    /// The 1-based source line at which the comment appeared, or <c>0</c> when the comment was constructed
    /// programmatically.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="prefix" /> is neither <c>'#'</c> nor <c>';'</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    public IniComment(char prefix, string text, int lineNumber = 0)
    {
        ThrowHelper.ThrowIfNull(text);
        if (prefix != '#' && prefix != ';')
            throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_IniCommentPrefix, nameof(prefix));

        Prefix = prefix;
        Text = text;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the prefix character that introduced this comment.
    /// </summary>
    /// <returns>Either <c>'#'</c> or <c>';'</c>.</returns>
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
    public bool Equals(IniComment other) =>
        this.Prefix == other.Prefix
        && string.Equals(this.Text, other.Text, StringComparison.Ordinal)
        && this.LineNumber == other.LineNumber;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is IniComment other && this.Equals(other);

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
    public static bool operator ==(IniComment left, IniComment right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two comments are not equal.
    /// </summary>
    /// <param name="left">The first comment.</param>
    /// <param name="right">The second comment.</param>
    /// <returns><see langword="true" /> when the comments differ.</returns>
    public static bool operator !=(IniComment left, IniComment right)
    {
        return !left.Equals(right);
    }
}
