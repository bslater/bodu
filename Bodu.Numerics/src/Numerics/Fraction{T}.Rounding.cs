// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction{T}.Rounding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T>
{
    /// <summary>
    /// Returns the largest integer rational value less than or equal to this value.
    /// </summary>
    /// <returns>This value rounded toward negative infinity.</returns>
    public Fraction<T> Floor() =>
        FromBigInteger(FloorDivide(BigNumerator, BigDenominator), BigInteger.One);

    /// <summary>
    /// Returns the smallest integer rational value greater than or equal to this value.
    /// </summary>
    /// <returns>This value rounded toward positive infinity.</returns>
    public Fraction<T> Ceiling() =>
        FromBigInteger(-FloorDivide(-BigNumerator, BigDenominator), BigInteger.One);

    /// <summary>
    /// Returns the integer part of this value, discarding any fractional component.
    /// </summary>
    /// <returns>This value rounded toward zero.</returns>
    public Fraction<T> Truncate() =>
        FromBigInteger(BigNumerator / BigDenominator, BigInteger.One);

    /// <summary>
    /// Returns this value rounded to the nearest integer, resolving a tie to the nearest even integer.
    /// </summary>
    /// <returns>This value rounded to the nearest integer.</returns>
    public Fraction<T> Round() =>
        Round(MidpointRounding.ToEven);

    /// <summary>
    /// Returns this value rounded to the nearest integer using the specified midpoint rule.
    /// </summary>
    /// <param name="mode">The strategy used to resolve a value exactly halfway between two integers.</param>
    /// <returns>This value rounded to the nearest integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="mode" /> is not a defined value.
    /// </exception>
    public Fraction<T> Round(MidpointRounding mode)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(mode);

        BigInteger denominator = BigDenominator;
        BigInteger floor = FloorDivide(BigNumerator, denominator);
        BigInteger remainder = BigNumerator - (floor * denominator);
        int comparison = (remainder * 2).CompareTo(denominator);

        BigInteger rounded = comparison switch
        {
            < 0 => floor,
            > 0 => floor + BigInteger.One,
            _ => ResolveMidpoint(floor, mode, BigNumerator.Sign),
        };

        return FromBigInteger(rounded, BigInteger.One);
    }

    /// <summary>
    /// Returns the integer part of this value as a value of the backing type.
    /// </summary>
    /// <returns>The integer part of this value, truncated toward zero.</returns>
    public T GetWholePart() =>
        T.CreateChecked(BigNumerator / BigDenominator);

    /// <summary>
    /// Returns the fractional part of this value, carrying the same sign as this value.
    /// </summary>
    /// <returns>The signed fractional component, whose magnitude is strictly less than one.</returns>
    public Fraction<T> GetFractionalPart() =>
        FromBigInteger(BigNumerator % BigDenominator, BigDenominator);

    /// <summary>
    /// Splits this value into its integer part and its signed fractional part.
    /// </summary>
    /// <returns>A tuple containing the integer part and the signed fractional part.</returns>
    public (T Whole, Fraction<T> Fraction) ToMixedParts() =>
        (GetWholePart(), GetFractionalPart());

    /// <summary>
    /// Deconstructs this value into its integer part and the numerator and denominator of its fractional part.
    /// </summary>
    /// <param name="whole">When this method returns, contains the integer part, truncated toward zero.</param>
    /// <param name="numerator">When this method returns, contains the signed numerator of the fractional part.</param>
    /// <param name="denominator">When this method returns, contains the denominator of the fractional part.</param>
    public void Deconstruct(out T whole, out T numerator, out T denominator)
    {
        Fraction<T> fraction = GetFractionalPart();

        whole = GetWholePart();
        numerator = fraction.Numerator;
        denominator = fraction.Denominator;
    }

    /// <summary>
    /// Resolves a value exactly halfway between two integers using the specified midpoint rule.
    /// </summary>
    /// <param name="floor">The integer immediately at or below the value.</param>
    /// <param name="mode">The midpoint rule to apply.</param>
    /// <param name="sign">The sign of the value being rounded.</param>
    /// <returns>The integer the value rounds to.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="mode" /> is not a defined value.
    /// </exception>
    private static BigInteger ResolveMidpoint(BigInteger floor, MidpointRounding mode, int sign)
    {
        BigInteger ceiling = floor + BigInteger.One;

        return mode switch
        {
            MidpointRounding.ToEven => floor.IsEven ? floor : ceiling,
            MidpointRounding.AwayFromZero => sign >= 0 ? ceiling : floor,
            MidpointRounding.ToZero => sign >= 0 ? floor : ceiling,
            MidpointRounding.ToNegativeInfinity => floor,
            MidpointRounding.ToPositiveInfinity => ceiling,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }
}
