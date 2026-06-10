// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextLocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Identifies a single position within a source document by its absolute offset and, for text sources, its one-based
/// line and column. Binary sources report only the offset, leaving <see cref="Line" /> and <see cref="Column" /> as
/// zero.
/// </summary>
/// <remarks>
/// The location is reported by parse diagnostics and carried on serialization exceptions so a failure can be traced
/// back to the exact point in the source where it was detected.
/// </remarks>
public readonly struct TextLocation
    : IEquatable<TextLocation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextLocation" /> struct.
    /// </summary>
    /// <param name="offset">The zero-based absolute offset of the position.</param>
    /// <param name="line">The one-based line number, or zero when the source is not line-oriented.</param>
    /// <param name="column">The one-based column number, or zero when the source is not line-oriented.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" />, <paramref name="line" />, or <paramref name="column" /> is negative.
    /// </exception>
    public TextLocation(int offset, int line = 0, int column = 0)
    {
        ThrowHelper.ThrowIfNegative(offset);
        ThrowHelper.ThrowIfNegative(line);
        ThrowHelper.ThrowIfNegative(column);

        Offset = offset;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the zero-based absolute offset of the position.
    /// </summary>
    /// <returns>The absolute offset, in source units.</returns>
    public int Offset { get; }

    /// <summary>
    /// Gets the one-based line number of the position.
    /// </summary>
    /// <returns>The line number, or zero when the source is not line-oriented.</returns>
    public int Line { get; }

    /// <summary>
    /// Gets the one-based column number of the position.
    /// </summary>
    /// <returns>The column number, or zero when the source is not line-oriented.</returns>
    public int Column { get; }

    /// <summary>
    /// Indicates whether two locations are equal.
    /// </summary>
    /// <param name="left">The first location.</param>
    /// <param name="right">The second location.</param>
    /// <returns><see langword="true" /> when the locations match; otherwise <see langword="false" />.</returns>
    public static bool operator ==(TextLocation left, TextLocation right) =>
        left.Equals(right);

    /// <summary>
    /// Indicates whether two locations are not equal.
    /// </summary>
    /// <param name="left">The first location.</param>
    /// <param name="right">The second location.</param>
    /// <returns><see langword="true" /> when the locations differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(TextLocation left, TextLocation right) =>
        !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(TextLocation other) =>
        Offset == other.Offset && Line == other.Line && Column == other.Column;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is TextLocation other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Offset, Line, Column);

    /// <summary>
    /// Returns a diagnostic string of the form <c>(line,column)</c> for line-oriented sources, or <c>offset N</c> for
    /// binary sources.
    /// </summary>
    /// <returns>A human-readable representation of the location.</returns>
    public override string ToString() =>
        Line > 0 ? $"({Line},{Column})" : $"offset {Offset}";
}
