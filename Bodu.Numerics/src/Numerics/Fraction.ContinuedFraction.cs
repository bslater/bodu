// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.ContinuedFraction.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T>
{
    /// <summary>
    /// Expands this rational value into the coefficients of its simple continued fraction.
    /// </summary>
    /// <returns>
    /// The continued-fraction coefficients <c>[a0; a1, a2, ...]</c>, where the leading coefficient carries the sign and
    /// every following coefficient is strictly positive.
    /// </returns>
    public T[] ToContinuedFraction()
    {
        List<T> coefficients = new List<T>();

        BigInteger n = BigNumerator;
        BigInteger d = BigDenominator;

        while (!d.IsZero)
        {
            BigInteger a = FloorDivide(n, d);
            coefficients.Add(T.CreateChecked(a));
            (n, d) = (d, n - (a * d));
        }

        return coefficients.ToArray();
    }

    /// <summary>
    /// Reconstructs a rational value from the coefficients of a simple continued fraction.
    /// </summary>
    /// <param name="coefficients">
    /// The continued-fraction coefficients. The leading coefficient may carry any sign; every following coefficient
    /// must be strictly positive.
    /// </param>
    /// <returns>The canonical rational value the coefficients describe.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="coefficients" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="coefficients" /> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if a coefficient after the first is not strictly positive.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> FromContinuedFraction(params T[] coefficients)
    {
        ThrowHelper.ThrowIfNull(coefficients);
        if (coefficients.Length == 0)
            throw new ArgumentException("At least one coefficient is required.", nameof(coefficients));

        for (int i = 1; i < coefficients.Length; i++)
        {
            if (T.IsZero(coefficients[i]) || T.IsNegative(coefficients[i]))
                throw new ArgumentOutOfRangeException(nameof(coefficients), "Coefficients after the first must be strictly positive.");
        }

        Fraction<T> result = new Fraction<T>(coefficients[^1]);
        for (int i = coefficients.Length - 2; i >= 0; i--)
        {
            result = new Fraction<T>(coefficients[i]) + result.Reciprocal();
        }

        return result;
    }

    /// <summary>
    /// Returns the closest rational value whose denominator does not exceed the specified limit.
    /// </summary>
    /// <param name="maxDenominator">The largest denominator the approximation may use.</param>
    /// <returns>
    /// The best rational approximation of this value with a denominator no greater than
    /// <paramref name="maxDenominator" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="maxDenominator" /> is less than one.
    /// </exception>
    /// <remarks>
    /// The approximation is derived from the convergents of the continued-fraction expansion and is the closest
    /// rational value subject to the denominator bound, matching the behaviour of the best-approximation algorithm used
    /// by other rational-number libraries.
    /// </remarks>
    public Fraction<T> LimitDenominator(T maxDenominator)
    {
        if (T.IsZero(maxDenominator) || T.IsNegative(maxDenominator))
            throw new ArgumentOutOfRangeException(nameof(maxDenominator), "The denominator limit must be at least one.");

        BigInteger limit = BigInteger.CreateChecked(maxDenominator);
        if (BigDenominator <= limit)
            return this;

        BigInteger p0 = BigInteger.Zero, q0 = BigInteger.One;
        BigInteger p1 = BigInteger.One, q1 = BigInteger.Zero;
        BigInteger n = BigNumerator, d = BigDenominator;

        while (true)
        {
            BigInteger a = FloorDivide(n, d);
            BigInteger q2 = q0 + (a * q1);
            if (q2 > limit)
                break;

            (p0, q0, p1, q1) = (p1, q1, p0 + (a * p1), q2);
            (n, d) = (d, n - (a * d));
        }

        BigInteger k = (limit - q0) / q1;
        Fraction<T> bound1 = FromBigInteger(p0 + (k * p1), q0 + (k * q1));
        Fraction<T> bound2 = FromBigInteger(p1, q1);

        return (bound2 - this).Abs() <= (bound1 - this).Abs() ? bound2 : bound1;
    }

    /// <summary>
    /// Divides one integer by another, rounding the quotient toward negative infinity.
    /// </summary>
    /// <param name="dividend">The value being divided.</param>
    /// <param name="divisor">The strictly positive value to divide by.</param>
    /// <returns>The floored quotient of <paramref name="dividend" /> divided by <paramref name="divisor" />.</returns>
    private static BigInteger FloorDivide(BigInteger dividend, BigInteger divisor)
    {
        BigInteger quotient = BigInteger.DivRem(dividend, divisor, out BigInteger remainder);
        if (!remainder.IsZero && remainder.Sign != divisor.Sign)
            quotient -= BigInteger.One;

        return quotient;
    }
}
