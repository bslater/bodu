// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.Operators.cs" company="Bodu Pty. Ltd.">
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
    /// <returns>The component-wise sum of <paramref name="left" /> and <paramref name="right" />.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var a = new Complex<double>(1.0, 2.0);
    /// var b = new Complex<double>(3.0, 4.0);
    ///
    /// var sum = a + b;               // 4 + 6i
    /// var difference = a - b;        // -2 - 2i
    /// var product = a * b;           // -5 + 10i
    /// var quotient = a / b;          // 0.44 + 0.08i
    ///]]>
    /// </code>
    /// </example>
    public static Complex<T> operator +(Complex<T> left, Complex<T> right) =>
        new(left.Real + right.Real, left.Imaginary + right.Imaginary);

    /// <summary>
    /// Subtracts one complex value from another.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The component-wise difference <paramref name="left" /> minus <paramref name="right" />.</returns>
    public static Complex<T> operator -(Complex<T> left, Complex<T> right) =>
        new(left.Real - right.Real, left.Imaginary - right.Imaginary);

    /// <summary>
    /// Multiplies two complex values.
    /// </summary>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <returns>The product <paramref name="left" /> times <paramref name="right" />.</returns>
    /// <remarks>
    /// The product is evaluated with the direct four-multiply form <c>(ac − bd) + (bc + ad)i</c>, matching
    /// <see cref="System.Numerics.Complex" />. No special infinity recovery is applied, so a product of non-finite
    /// operands can yield <see cref="IFloatingPointIeee754{TSelf}.NaN" /> components in the same cases as the framework
    /// type.
    /// </remarks>
    public static Complex<T> operator *(Complex<T> left, Complex<T> right) =>
        new(
            (left.Real * right.Real) - (left.Imaginary * right.Imaginary),
            (left.Imaginary * right.Real) + (left.Real * right.Imaginary));

    /// <summary>
    /// Divides one complex value by another.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The quotient <paramref name="left" /> divided by <paramref name="right" />.</returns>
    /// <remarks>
    /// Division uses Smith's algorithm, which factors out the divisor component of larger magnitude to avoid
    /// intermediate overflow, matching <see cref="System.Numerics.Complex" /> bit for bit for finite inputs.
    /// </remarks>
    public static Complex<T> operator /(Complex<T> left, Complex<T> right)
    {
        T a = left.Real;
        T b = left.Imaginary;
        T c = right.Real;
        T d = right.Imaginary;

        if (T.Abs(d) < T.Abs(c))
        {
            T doc = d / c;
            return new Complex<T>(
                (a + (b * doc)) / (c + (d * doc)),
                (b - (a * doc)) / (c + (d * doc)));
        }
        else
        {
            T cod = c / d;
            return new Complex<T>(
                (b + (a * cod)) / (d + (c * cod)),
                (-a + (b * cod)) / (d + (c * cod)));
        }
    }

    /// <summary>
    /// Returns the additive inverse of a complex value.
    /// </summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The value with both components negated.</returns>
    public static Complex<T> operator -(Complex<T> value) =>
        new(-value.Real, -value.Imaginary);

    /// <summary>
    /// Returns the complex value unchanged.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>The unmodified <paramref name="value" />.</returns>
    public static Complex<T> operator +(Complex<T> value) =>
        value;

    /// <summary>
    /// Returns the complex value increased by the real unit one.
    /// </summary>
    /// <param name="value">The value to increment.</param>
    /// <returns>The value with <c>1</c> added to its real component.</returns>
    public static Complex<T> operator ++(Complex<T> value) =>
        value + One;

    /// <summary>
    /// Returns the complex value decreased by the real unit one.
    /// </summary>
    /// <param name="value">The value to decrement.</param>
    /// <returns>The value with <c>1</c> subtracted from its real component.</returns>
    public static Complex<T> operator --(Complex<T> value) =>
        value - One;

    /// <summary>
    /// Determines whether two complex values are equal, comparing components with floating-point equality.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> if both components are equal under floating-point equality; otherwise,
    /// <see langword="false" />. A component that is <see cref="IFloatingPointIeee754{TSelf}.NaN" /> is not equal to
    /// itself, and positive and negative zero compare equal.
    /// </returns>
    public static bool operator ==(Complex<T> left, Complex<T> right) =>
        left.Real == right.Real && left.Imaginary == right.Imaginary;

    /// <summary>
    /// Determines whether two complex values are unequal, comparing components with floating-point equality.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> if the values differ under floating-point equality; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(Complex<T> left, Complex<T> right) =>
        !(left == right);
}
