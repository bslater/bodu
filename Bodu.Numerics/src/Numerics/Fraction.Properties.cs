// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Properties.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct Fraction<T>
{
    /// <summary>
    /// Gets a value indicating whether this rational value is zero.
    /// </summary>
    /// <returns><see langword="true" /> if this value equals zero; otherwise, <see langword="false" />.</returns>
    public bool IsZero =>
        T.IsZero(_numerator);

    /// <summary>
    /// Gets a value indicating whether this rational value is an exact integer.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the canonical denominator is one; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsInteger =>
        Denominator == T.One;

    /// <summary>
    /// Gets a value indicating whether this rational value is a proper fraction.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the magnitude of this value is strictly less than one; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool IsProper =>
        Abs(_numerator) < Denominator;

    /// <summary>
    /// Gets a value indicating whether this rational value is a unit fraction.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the numerator has a magnitude of one; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsUnit =>
        Abs(_numerator) == T.One;

    /// <summary>
    /// Gets a value indicating whether this rational value is negative.
    /// </summary>
    /// <returns><see langword="true" /> if this value is less than zero; otherwise, <see langword="false" />.</returns>
    public bool IsNegative =>
        T.IsNegative(_numerator);

    /// <summary>
    /// Gets a value indicating whether this rational value is positive.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if this value is greater than zero; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsPositive =>
        !T.IsZero(_numerator) && !T.IsNegative(_numerator);
}
