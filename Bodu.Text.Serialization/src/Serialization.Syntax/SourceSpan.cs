// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SourceSpan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Represents a contiguous, offset-based slice of a source document, identified by a start position and a length. The
/// span is unit-agnostic: the offsets count characters for a text format and bytes for a binary format.
/// </summary>
/// <remarks>
/// The span deliberately stores only integer offsets so that a single concrete syntax tree abstraction can describe
/// both character-oriented sources (such as TOML) and byte-oriented sources (such as Bencode) without committing the
/// common layer to one element type.
/// </remarks>
public readonly struct SourceSpan
    : IEquatable<SourceSpan>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceSpan" /> struct.
    /// </summary>
    /// <param name="start">The zero-based offset of the first unit in the span.</param>
    /// <param name="length">The number of units the span covers.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> or <paramref name="length" /> is negative.
    /// </exception>
    public SourceSpan(int start, int length)
    {
        ThrowHelper.ThrowIfLessThan(start, 0);
        ThrowHelper.ThrowIfLessThan(length, 0);

        Start = start;
        Length = length;
    }

    /// <summary>
    /// Gets the zero-based offset of the first unit in the span.
    /// </summary>
    /// <returns>The start offset.</returns>
    public int Start { get; }

    /// <summary>
    /// Gets the number of units the span covers.
    /// </summary>
    /// <returns>The span length.</returns>
    public int Length { get; }

    /// <summary>
    /// Gets the offset immediately past the last unit in the span.
    /// </summary>
    /// <returns>The sum of <see cref="Start" /> and <see cref="Length" />.</returns>
    public int End => Start + Length;

    /// <summary>
    /// Indicates whether two spans are equal.
    /// </summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns>
    /// <see langword="true" /> when the spans share the same start and length; otherwise <see langword="false" />.
    /// </returns>
    public static bool operator ==(SourceSpan left, SourceSpan right) =>
        left.Equals(right);

    /// <summary>
    /// Indicates whether two spans are not equal.
    /// </summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns><see langword="true" /> when the spans differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(SourceSpan left, SourceSpan right) =>
        !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(SourceSpan other) =>
        Start == other.Start && Length == other.Length;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is SourceSpan other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Start, Length);

    /// <summary>
    /// Returns a diagnostic string of the form <c>[start..end)</c>.
    /// </summary>
    /// <returns>A human-readable representation of the span.</returns>
    public override string ToString() =>
        $"[{Start}..{End})";
}
