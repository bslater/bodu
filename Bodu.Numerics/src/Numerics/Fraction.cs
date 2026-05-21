// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Bodu.Numerics;

/// <summary>
/// Represents an immutable exact rational number — a ratio of two integers — backed by an arbitrary
/// <see cref="IBinaryInteger{TSelf}" /> component type.
/// </summary>
/// <typeparam name="T">
/// The integer type used to store the numerator and denominator. Use a fixed-width type such as <see cref="int" /> or
/// <see cref="long" /> for compact storage, or <see cref="BigInteger" /> for arithmetic that never overflows.
/// </typeparam>
/// <remarks>
/// <para>
/// A <see cref="Fraction{T}" /> is always held in canonical form: the denominator is strictly positive, the numerator
/// carries the sign, and the two components share no common factor other than one. Equal rational values therefore have
/// identical components and compare and hash equally.
/// </para>
/// <para>
/// Arithmetic is exact. Intermediate results are evaluated with <see cref="BigInteger" /> precision and the canonical
/// result is narrowed back to <typeparamref name="T" />; when the narrowed component does not fit a fixed-width
/// <typeparamref name="T" /> an <see cref="OverflowException" /> is thrown. Selecting <see cref="BigInteger" /> as
/// <typeparamref name="T" /> removes that limit entirely.
/// </para>
/// <para>
/// When <typeparamref name="T" /> is an unsigned integer type, negative rationals cannot be represented; operations
/// that would produce a negative component — such as negating a non-zero value — throw an
/// <see cref="OverflowException" /> at run time.
/// </para>
/// </remarks>
[Serializable]
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(FractionJsonConverterFactory))]
public readonly partial struct Fraction<T>
    where T : IBinaryInteger<T>
{
    /// <summary>
    /// The canonical numerator. Carries the sign of the rational value.
    /// </summary>
    private readonly T _numerator;

    /// <summary>
    /// The canonical denominator. A stored value of zero occurs only for a default-initialized instance and is
    /// interpreted as one by <see cref="Denominator" />.
    /// </summary>
    private readonly T _denominator;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fraction{T}" /> struct representing the whole number
    /// <paramref name="value" />.
    /// </summary>
    /// <param name="value">The integer value the fraction represents.</param>
    public Fraction(T value)
    {
        _numerator = value;
        _denominator = T.One;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Fraction{T}" /> struct from the specified numerator and
    /// denominator, reducing the result to canonical form.
    /// </summary>
    /// <param name="numerator">The numerator of the rational value.</param>
    /// <param name="denominator">The denominator of the rational value.</param>
    /// <exception cref="DivideByZeroException">Thrown if <paramref name="denominator" /> is zero.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public Fraction(T numerator, T denominator)
    {
        if (T.IsZero(denominator))
            throw new DivideByZeroException("The denominator of a fraction must not be zero.");

        BigInteger n = BigInteger.CreateChecked(numerator);
        BigInteger d = BigInteger.CreateChecked(denominator);

        if (d.Sign < 0)
        {
            n = -n;
            d = -d;
        }

        BigInteger g = BigInteger.GreatestCommonDivisor(n, d);
        if (g > BigInteger.One)
        {
            n /= g;
            d /= g;
        }

        _numerator = T.CreateChecked(n);
        _denominator = T.CreateChecked(d);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Fraction{T}" /> struct from components that are already in
    /// canonical form, bypassing reduction.
    /// </summary>
    /// <param name="numerator">The canonical numerator.</param>
    /// <param name="denominator">The canonical, strictly positive denominator.</param>
    /// <param name="canonical">
    /// A discriminator that selects the no-reduction initialization path. The value itself is not inspected.
    /// </param>
    private Fraction(T numerator, T denominator, bool canonical)
    {
        _ = canonical;
        _numerator = numerator;
        _denominator = denominator;
    }

    /// <summary>
    /// Gets a <see cref="Fraction{T}" /> representing the value zero.
    /// </summary>
    /// <returns>The rational value <c>0/1</c>.</returns>
    public static Fraction<T> Zero =>
        new Fraction<T>(T.Zero, T.One, canonical: true);

    /// <summary>
    /// Gets a <see cref="Fraction{T}" /> representing the value one.
    /// </summary>
    /// <returns>The rational value <c>1/1</c>.</returns>
    public static Fraction<T> One =>
        new Fraction<T>(T.One, T.One, canonical: true);

    /// <summary>
    /// Gets a <see cref="Fraction{T}" /> representing the value negative one.
    /// </summary>
    /// <returns>The rational value <c>-1/1</c>.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if <typeparamref name="T" /> is an unsigned type and cannot represent <c>-1</c>.
    /// </exception>
    public static Fraction<T> MinusOne =>
        FromBigInteger(BigInteger.MinusOne, BigInteger.One);

    /// <summary>
    /// Gets the numerator of the rational value in canonical form.
    /// </summary>
    /// <returns>The signed numerator.</returns>
    public T Numerator => _numerator;

    /// <summary>
    /// Gets the denominator of the rational value in canonical form.
    /// </summary>
    /// <returns>The strictly positive denominator.</returns>
    /// <value>
    /// A value of one is reported for a default-initialized instance so that <c>default(Fraction&lt;T&gt;)</c> behaves
    /// as the rational value zero.
    /// </value>
    public T Denominator =>
        T.IsZero(_denominator) ? T.One : _denominator;

    /// <summary>
    /// Gets the sign of the rational value.
    /// </summary>
    /// <returns>
    /// <c>-1</c> if the value is negative, <c>0</c> if the value is zero, and <c>1</c> if the value is positive.
    /// </returns>
    public int Sign =>
        T.IsZero(_numerator) ? 0 : (T.IsNegative(_numerator) ? -1 : 1);

    /// <summary>
    /// Deconstructs the rational value into its canonical numerator and denominator.
    /// </summary>
    /// <param name="numerator">When this method returns, contains the canonical numerator.</param>
    /// <param name="denominator">When this method returns, contains the canonical denominator.</param>
    public void Deconstruct(out T numerator, out T denominator)
    {
        numerator = Numerator;
        denominator = Denominator;
    }

    /// <summary>
    /// Computes the greatest common divisor of two integers.
    /// </summary>
    /// <param name="left">The first integer.</param>
    /// <param name="right">The second integer.</param>
    /// <returns>
    /// The largest non-negative integer that divides both <paramref name="left" /> and <paramref name="right" />, or
    /// zero when both arguments are zero.
    /// </returns>
    public static T GreatestCommonDivisor(T left, T right)
    {
        T a = Abs(left);
        T b = Abs(right);

        while (!T.IsZero(b))
        {
            (a, b) = (b, a % b);
        }

        return a;
    }

    /// <summary>
    /// Computes the least common multiple of two integers.
    /// </summary>
    /// <param name="left">The first integer.</param>
    /// <param name="right">The second integer.</param>
    /// <returns>
    /// The smallest non-negative integer that is a multiple of both arguments, or zero when either argument is zero.
    /// </returns>
    /// <exception cref="OverflowException">
    /// Thrown if the least common multiple cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static T LeastCommonMultiple(T left, T right)
    {
        if (T.IsZero(left) || T.IsZero(right))
            return T.Zero;

        BigInteger a = BigInteger.Abs(BigInteger.CreateChecked(left));
        BigInteger b = BigInteger.Abs(BigInteger.CreateChecked(right));
        BigInteger result = a / BigInteger.GreatestCommonDivisor(a, b) * b;

        return T.CreateChecked(result);
    }

    /// <summary>
    /// Returns the absolute value of an integer.
    /// </summary>
    /// <param name="value">The integer whose magnitude is required.</param>
    /// <returns>The magnitude of <paramref name="value" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Abs(T value) =>
        T.IsNegative(value) ? -value : value;

    /// <summary>
    /// Creates a canonical <see cref="Fraction{T}" /> from a numerator and denominator evaluated with
    /// <see cref="BigInteger" /> precision.
    /// </summary>
    /// <param name="numerator">The unreduced numerator.</param>
    /// <param name="denominator">The unreduced, non-zero denominator.</param>
    /// <returns>The reduced rational value narrowed to <typeparamref name="T" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    private static Fraction<T> FromBigInteger(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.Sign < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        BigInteger g = BigInteger.GreatestCommonDivisor(numerator, denominator);
        if (g > BigInteger.One)
        {
            numerator /= g;
            denominator /= g;
        }

        return new Fraction<T>(T.CreateChecked(numerator), T.CreateChecked(denominator), canonical: true);
    }
}
