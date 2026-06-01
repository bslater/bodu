// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

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
        BigInteger.Abs(BigNumerator) < BigDenominator;

    /// <summary>
    /// Gets a value indicating whether this rational value is a unit fraction.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the numerator has a magnitude of one; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsUnit =>
        BigInteger.Abs(BigNumerator).IsOne;

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

    /// <summary>
    /// Gets a value indicating whether this rational value is a whole number.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the canonical denominator is one; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This property is an alias for <see cref="IsInteger" />.
    /// </remarks>
    public bool IsWhole =>
        IsInteger;

    /// <summary>
    /// Gets a value indicating whether this rational value is an improper fraction.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the magnitude of this value is at least one; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsImproper =>
        !IsProper;

    /// <summary>
    /// Gets a value indicating whether this rational value can be reduced to lower terms.
    /// </summary>
    /// <returns><see langword="false" />; the value is always held in canonical form.</returns>
    public bool IsReducible =>
        false;

    /// <summary>
    /// Gets a value indicating whether this rational value is in canonical form.
    /// </summary>
    /// <returns><see langword="true" />; the value is always held in canonical form.</returns>
    public bool IsCanonical =>
        true;

    /// <summary>
    /// Gets a value indicating whether this rational value is an even integer.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if this value is an integer with an even numerator; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsEvenInteger =>
        IsInteger && BigNumerator.IsEven;

    /// <summary>
    /// Gets a value indicating whether this rational value is an odd integer.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if this value is an integer with an odd numerator; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsOddInteger =>
        IsInteger && !BigNumerator.IsEven;
}
