// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.Arithmetic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Complex<T>
{
    /// <summary>
    /// Adds two complex values.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static Complex<T> Add(Complex<T> left, Complex<T> right) =>
        left + right;

    /// <summary>
    /// Subtracts one complex value from another.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The difference <paramref name="left" /> minus <paramref name="right" />.</returns>
    public static Complex<T> Subtract(Complex<T> left, Complex<T> right) =>
        left - right;

    /// <summary>
    /// Multiplies two complex values.
    /// </summary>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <returns>The product <paramref name="left" /> times <paramref name="right" />.</returns>
    public static Complex<T> Multiply(Complex<T> left, Complex<T> right) =>
        left * right;

    /// <summary>
    /// Divides one complex value by another.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The quotient <paramref name="left" /> divided by <paramref name="right" />.</returns>
    public static Complex<T> Divide(Complex<T> left, Complex<T> right) =>
        left / right;

    /// <summary>
    /// Returns the additive inverse of a complex value.
    /// </summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The value with both components negated.</returns>
    public static Complex<T> Negate(Complex<T> value) =>
        -value;

    /// <summary>
    /// Returns the complex conjugate of a value.
    /// </summary>
    /// <param name="value">The value to conjugate.</param>
    /// <returns>The value with the sign of its imaginary component reversed.</returns>
    public static Complex<T> Conjugate(Complex<T> value) =>
        new(value.Real, -value.Imaginary);

    /// <summary>
    /// Returns the multiplicative inverse of a complex value.
    /// </summary>
    /// <param name="value">The value to invert.</param>
    /// <returns>
    /// The reciprocal <c>1 / value</c>, or <see cref="Zero" /> when <paramref name="value" /> is zero.
    /// </returns>
    /// <remarks>
    /// Returning <see cref="Zero" /> for a zero input matches <see cref="System.Numerics.Complex.Reciprocal" /> rather
    /// than producing an infinity.
    /// </remarks>
    public static Complex<T> Reciprocal(Complex<T> value) =>
        T.IsZero(value.Real) && T.IsZero(value.Imaginary)
            ? Zero
            : One / value;

    /// <summary>
    /// Returns the magnitude (absolute value) of a complex value.
    /// </summary>
    /// <param name="value">The value whose magnitude is computed.</param>
    /// <returns>The Euclidean distance of <paramref name="value" /> from the origin.</returns>
    /// <remarks>
    /// The magnitude is evaluated with overflow- and underflow-avoiding scaling: the larger component is factored out
    /// so that <c>Real² + Imaginary²</c> is never formed directly. This matches the scaled hypotenuse used by
    /// <see cref="System.Numerics.Complex.Abs" />.
    /// </remarks>
    public static T Abs(Complex<T> value) =>
        Hypot(value.Real, value.Imaginary);

    /// <summary>
    /// Computes <c>sqrt(a² + b²)</c> without intermediate overflow or underflow by factoring out the larger operand.
    /// </summary>
    /// <param name="a">The first operand.</param>
    /// <param name="b">The second operand.</param>
    /// <returns>The scaled hypotenuse of <paramref name="a" /> and <paramref name="b" />.</returns>
    /// <remarks>
    /// Uses the identity <c>sqrt(a² + b²) = large · sqrt(1 + (small / large)²)</c>, mirroring the private helper behind
    /// <see cref="System.Numerics.Complex.Abs" /> so that magnitudes agree component for component.
    /// </remarks>
    private static T Hypot(T a, T b)
    {
        a = T.Abs(a);
        b = T.Abs(b);

        T small;
        T large;
        if (a < b)
        {
            small = a;
            large = b;
        }
        else
        {
            small = b;
            large = a;
        }

        if (T.IsZero(small))
            return large;

        // An infinite larger operand yields an infinite magnitude, but only when the smaller operand is not NaN: a NaN
        // component must poison the result to NaN. This mirrors the guarded fast path in System.Numerics.Complex, so
        // Abs(±∞, NaN) returns NaN rather than infinity.
        if (T.IsPositiveInfinity(large) && !T.IsNaN(small))
            return T.PositiveInfinity;

        T ratio = small / large;
        return large * T.Sqrt(T.One + (ratio * ratio));
    }
}
