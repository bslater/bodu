// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Complex<T> :
    IEquatable<Complex<T>>
{
    /// <summary>
    /// Determines whether the specified object is a <see cref="Complex{T}" /> equal to this value.
    /// </summary>
    /// <param name="obj">The object to compare with this value.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="Complex{T}" /> with equal components;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Complex<T> other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="Complex{T}" /> is equal to this value.
    /// </summary>
    /// <param name="other">The value to compare with this value.</param>
    /// <returns>
    /// <see langword="true" /> if both components are equal under <see cref="IEquatable{T}" /> equality; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Unlike <see cref="op_Equality" />, this method uses the components' own <see cref="object.Equals(object)" />
    /// contract, so a <see cref="IFloatingPointIeee754{TSelf}.NaN" /> component is considered equal to another
    /// <c>NaN</c> component. This matches <see cref="System.Numerics.Complex.Equals(System.Numerics.Complex)" />.
    /// </remarks>
    public bool Equals(Complex<T> other) =>
        EqualityComparer<T>.Default.Equals(Real, other.Real)
        && EqualityComparer<T>.Default.Equals(Imaginary, other.Imaginary);

    /// <summary>
    /// Returns a hash code for this complex value.
    /// </summary>
    /// <returns>A hash code derived from the real and imaginary components.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(Real, Imaginary);
}
