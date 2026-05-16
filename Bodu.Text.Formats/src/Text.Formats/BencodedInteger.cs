// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedInteger.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a bencoded integer value.
/// </summary>
public sealed class BencodedInteger
    : BencodedValue
    , IEquatable<BencodedInteger>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodedInteger" /> class.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public BencodedInteger(long value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public override BencodedValueKind Kind => BencodedValueKind.Integer;

    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Determines whether this instance and another <see cref="BencodedInteger" /> have the same value.
    /// </summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns><see langword="true" /> when both values are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(BencodedInteger? other) =>
        other is not null && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is BencodedInteger other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        Value.ToString(CultureInfo.InvariantCulture);
}
