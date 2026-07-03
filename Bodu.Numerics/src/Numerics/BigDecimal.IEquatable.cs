// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
    : IEquatable<BigDecimal>
{
    /// <summary>
    /// Determines whether this value equals another <see cref="BigDecimal" />.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns>
    /// <see langword="true" /> when the values are numerically equal; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Because every value is held in canonical form, numerically equal values (for example <c>1.0</c> and <c>1.00</c>)
    /// share the same unscaled value and scale and therefore compare equal.
    /// </remarks>
    public bool Equals(BigDecimal other) =>
        _scale == other._scale && _mantissa == other._mantissa;

    /// <summary>
    /// Determines whether this value equals another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is a <see cref="BigDecimal" /> equal to this value;
    /// otherwise <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is BigDecimal other && Equals(other);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(BigDecimal)" />.
    /// </summary>
    /// <returns>A hash code for the value.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(_mantissa, _scale);
}
